using System;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;
using Util.UniTween.Value;
using Util.UniTween.Value.Blending;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat.UI.TurnOrder
{
	public abstract class ViewTurn : MonoBehaviour
	{
		static private float[] _RNGVals;

		public static float[] RNGVals
		{
			get
			{
				if (_RNGVals == null)
				{
					_RNGVals = new float[32];
					for (int i = 0; i < 32; i++)
					{
						_RNGVals[i] = RNG.Float;
					}
				}

				return _RNGVals;
			}
		}

		public float GetRNGVal(int offset = 0) => RNGVals.WrapGet((int)Action.id + offset);

		[ShowInPlay] public uint  TurnID      => Action.id;
		[ShowInPlay] public float BasicRNGVal => GetRNGVal();


		[FormerlySerializedAs("Styles"), SerializeField]
		public ViewStateStyleCollection StateStyles;

		[FormerlySerializedAs("AlignmentStyleAlly"), SerializeField, LabelText("Ally Style"), Required, Space]
		protected FriendnessStyle AllyStyle;

		[FormerlySerializedAs("AlignmentStyleEnemy"), SerializeField, LabelText("Enemy Style"), Required]
		protected FriendnessStyle EnemyStyle;

		public SelectUIObject selection = SelectUIObject.Initial;

		[NonSerialized]
		public PooledView poolee;

		public RectTransform Rect;

		[ShowInPlay] protected ViewStyle  style;
		[ShowInPlay] protected ViewStates state;

		[ShowInPlay] private int            _turnIndex;
		[ShowInPlay] private ViewFriendness _friendness;

	#region Tweenable Properties

		public TweenableVector2   position = new TweenableVector2();
		public TweenableFloat     rotation = new TweenableFloat();
		public Vector2OffsetMixer offset   = new Vector2OffsetMixer();
		public BlendableVector2   scale    = new BlendableVector2(Vector2.one, new Vector2MultiplierMixer());
		public BlendableFloat     shading  = new BlendableFloat(0, new FloatMultiplierMixer());
		public BlendableFloat     opacity  = new BlendableFloat(1, new FloatMultiplierMixer());
		public TintMixer          tint     = new TintMixer();
		public FillMixer          fill     = new FillMixer(Color.clear);

	#endregion

		protected virtual void Awake()
		{
			Rect = GetComponent<RectTransform>();
		}

		/// <summary>
		/// The current TurnInfo.
		/// </summary>
		[ShowInPlay]
		public TurnInfo Info { get; private set; }


		/// <summary>
		/// The current ViewInfo.
		/// </summary>
		[ShowInPlay]
		public ViewInfo vInfo { get; set; }

		/// <summary>
		/// The event of the current TurnInfo.
		/// </summary>
		[CanBeNull]
		public ITurnActer Event => Info.action.acter;

		/// <summary>
		/// The trigger of the current TurnInfo.
		/// </summary>
		[CanBeNull]
		public Trigger Trigger => Info.trigger;

		/// <summary>
		/// The action of the current TurnInfo.
		/// </summary>
		public Action Action => Info.action;

		[ShowInPlay]
		public int SortingIndex
		{
			set => Rect.SetSiblingIndex(value);
		}

		public Color MainColor => GetStyleForFriendness(_friendness).effectColor;

		/// <summary>
		/// Set the action info currently being looked at.
		/// </summary>
		/// <param name="info"></param>
		public void SetTurn(TurnInfo info)
		{
			ITurnActer oevent = Info.acter;
			ITurnActer nevent = info.action.acter;

			Trigger otrigger = Trigger;
			Trigger ntrigger = info.trigger;

			Info = info;

			if (oevent != nevent) OnEventChanged(oevent, nevent);
			if (info.trigger != null) OnTriggerChanged(otrigger, ntrigger);

			if (GameOptions.current.combat_merge_groupturns && info.marker == ActionMarker.Action && info.groupCount > 1)
			{
				SetNumberShow(true);
				SetNumber(info.groupCount);
			}
			else
			{
				SetNumberShow(false);
			}
		}


		public FriendnessStyle GetStyleForFriendness(ViewFriendness friend)
		{
			switch (friend)
			{
				case ViewFriendness.Ally:
					return AllyStyle;

				case ViewFriendness.Enemy:
				case ViewFriendness.Neutral:
					return EnemyStyle;

				default:
					throw new ArgumentOutOfRangeException(nameof(friend), friend, null);
			}
		}

		/// <summary>
		/// The final desired size of the action, excluding animation.
		/// </summary>
		/// <param name="vi"></param>
		/// <returns></returns>
		public virtual Vector2 GetDesiredSize(ViewInfo state)
		{
			return Vector2.one * StateStyles.Get(state.state).scale;
		}

		protected abstract void OnEventChanged(ITurnActer old, ITurnActer @new);

		protected abstract void OnTriggerChanged(Trigger old, Trigger @new);

	#region Set Functions

		/// <summary>
		/// Set instantly to a ViewInfo.
		/// </summary>
		/// <param name="vi"></param>
		public void Set(ViewInfo vi)
		{
			if (vi.state == ViewStates.Inactive)
			{
				gameObject.SetActive(false);
				return;
			}

			SetPosition(vi.position);
			SetStates(vi.state);
			SetFriendness(vi.friendness);
		}

		public void SetPosition(Vector2 position)
		{
			this.position.value = position;
		}

		protected abstract void SetNumber(int infoStackCount);

		protected abstract void SetNumberShow(bool enable);

		/// <summary>
		/// Sets the visual properties for the state.
		/// </summary>
		public abstract void SetStates(ViewStates state);

		/// <summary>
		/// Sets the visual properties for the friendness.
		/// </summary>
		public virtual void SetFriendness(ViewFriendness friendness)
		{
			this._friendness = friendness;
		}

		/// <summary>
		/// Update the images for the state.
		/// </summary>
		protected abstract void UpdateStateImages();

	#endregion


	#region Animation Functions

		public Tween Position(float   x,          float y, Easer ease) => this.position.To(new Vector2(x, y), ease);
		public Tween Position(Vector2 pos,        Easer ease) => this.position.To(pos, ease);
		public Tween Rotation(float   rot,        Easer ease) => this.rotation.To(rot, ease);
		public Tween Offset(Vector2   offset,     Easer ease) => this.offset.value.To(offset, ease);
		public Tween OffsetB(Vector2  offset,     Easer ease) => this.offset.BlendTo(offset, ease);
		public Tween Scale(float      scale,      Easer ease) => this.scale.To(Vector2.one * scale, ease);
		public Tween Scale(Vector2    scale,      Easer ease) => this.scale.To(scale, ease);
		public Tween Shading(float    shading,    Easer ease) => this.shading.To(shading, ease);
		public Tween Opacity(float    opacity,    Easer ease) => this.opacity.To(opacity, ease);
		public Tween Tint(Color       lightColor, Easer ease) => this.tint.BlendTo(lightColor, ease);
		public Tween Fill(Color       lightColor, Easer ease) => this.fill.BlendTo(lightColor, ease);

		public Tween Friendness(ViewFriendness friendness)
		{
			// we could tween, but alignment typically doesn't ever change after creation of the view.
			SetFriendness(friendness);
			return null;
		}

		public Tween State(ViewStates state, Easer state_ease)
		{
			Sequence seq = DOTween.Sequence();

			float scale   = 1;
			float opacity = 1;
			float shading = 0;

			style      = StateStyles.Get(state);
			this.state = state;

			if (state == ViewStates.Stacked && vInfo.stackHeadState.HasValue)
			{
				ViewStyle headStyle = StateStyles.Get(vInfo.stackHeadState.Value);

				// Get an RNG val that is constant for our particular action
				float rng = GetRNGVal();

				float scale_mag = headStyle.scale * Mathf.Pow(style.scale, Info.groupIndex) + (style.scaling_rng.Lerp(rng));

				Offset(headStyle.offset + style.offset * scale_mag * Info.groupIndex + new Vector2(style.offset_x_rng.Lerp(rng), rng * style.offset_y_rng.Lerp(rng)) * scale_mag, state_ease).JoinTo(seq);
				Rotation(headStyle.rotation + style.rotation * scale_mag * Info.groupIndex + (style.rotation_rng.Lerp(rng)) * scale_mag, state_ease).JoinTo(seq);
				Scale(scale_mag, state_ease).JoinTo(seq).OnStart(UpdateStateImages);

				Shading(headStyle.shading + style.shading * Info.groupIndex, state_ease).JoinTo(seq);
				Opacity(headStyle.opacity + style.opacity * Info.groupIndex, state_ease).JoinTo(seq);
			}
			else
			{
				Scale(style.scale, state_ease).JoinTo(seq).OnStart(UpdateStateImages);
				Shading(style.shading, state_ease).JoinTo(seq);
				Opacity(style.opacity, state_ease).JoinTo(seq);
				Offset(style.offset, state_ease).JoinTo(seq);
				Rotation(style.rotation, state_ease).JoinTo(seq);
			}

			return seq;
		}

	#endregion
	}
}
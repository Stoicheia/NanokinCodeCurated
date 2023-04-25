using System;
using Anjin.Cameras;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using DG.Tweening;
using Sirenix.OdinInspector;
using UniTween.Core;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Anjin.UI
{
	[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
	public class HUDElement : SerializedMonoBehaviour
	{
		public enum ElemPositionMode { Free, AnchoredToWorldPoint }

		/// <summary>
		/// The pivot rect that will be used for scaling, rotating, etc.
		/// </summary>
		[Title("References")]
		[Optional]
		public RectTransform PivotRect;

		[Title("Position")]
		public ElemPositionMode PositionMode;
		public bool       IsFreePositionPercentage;
		public Vector3    WorldAnchorOffset;
		public Vector2    ScreenOffset;
		public WorldPoint WorldAnchor;

		[Title("Distance Fade")]
		public bool DistanceFade;
		public float      DistanceFadeStart;
		public float      DistanceFadeEnd;
		public WorldPoint DistanceFadeCenterPoint;

		public float InitialAlpha = 1;

		[Title("Distance Scale")]
		public bool DistanceScale;
		public AnimationCurve DistanceToScale;

		// Right now the distance is baked into the curve. We will need this if we want to base it off of external properties like TalkNPC radius.
		// However, we may wanna add another variable to have a static plateau range before the curve actually starts applying. Otherwise if we
		// bake the initial plateau into the curve, the plateau range will be proportional to the max distance as well.
		// That is of course IF we want an initial static plateau. I think it's a good idea personally, having all bubbles at 1x1x1 scale within
		// a certain range sounds like it would make it easier to read without having the bubble constantly resizing as you move ever so slightly.
		// -oxy
		// public float          MaxDistance;

		[Title("Runtime")]
		[HideInEditorMode] public Vector2 FreePosition;
		[HideInEditorMode] public TweenableFloat Alpha              = 1.0f;

		public bool Invisible = false;

		public bool Interactable = true;

		public bool ManualPosition 	= false;
		public bool ManualScale 	= false;
		public bool ManualRotation 	= false;

		[HideInEditorMode] public TweenableVector3 Scale 	= Vector3.one;
		[HideInEditorMode] public TweenableVector3 Rotation = Vector3.zero;
		[HideInEditorMode] public TweenableVector3 SequenceOffset = Vector3.zero;

		private RectTransform _rectTrans;
		private CanvasGroup   _canvasGroup;

		private bool _blocksRaycasts;

		protected virtual void Awake()
		{
			_rectTrans   = GetComponent<RectTransform>();
			_canvasGroup = GetComponent<CanvasGroup>();

			FreePosition = _rectTrans.localPosition;
			WorldAnchor  = WorldPoint.Default;

			Alpha          = new TweenableFloat(InitialAlpha);
			Scale          = new TweenableVector3(transform.localScale);
			Rotation 	   = new TweenableVector3(transform.localRotation.eulerAngles);
			SequenceOffset = new TweenableVector3(Vector3.zero);

			_blocksRaycasts = _canvasGroup.blocksRaycasts;

			if (PivotRect == null)
				PivotRect = _rectTrans;
		}

		public void LateUpdate()
		{
			var alphaMultipler  = 1.0f;
			var scaleMultiplier = 1.0f;

			Vector3 worldPos = Vector3.zero;

			if(!ManualPosition) {
				switch (PositionMode) {
					case ElemPositionMode.Free:
						Vector2 finalPos = FreePosition;

						if (IsFreePositionPercentage)
							finalPos = GameHUD.PercentToPixel(finalPos);

						_rectTrans.localPosition = finalPos + SequenceOffset.value.xy();
						break;

					case ElemPositionMode.AnchoredToWorldPoint:
						Camera cam = GameCams.Live.UnityCam;

						if (WorldAnchor.TryGet(out Vector3 position)) {
							worldPos = position + WorldAnchorOffset + SequenceOffset;

							Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
							_rectTrans.localPosition = GameHUD.CorrectScreenpointPos(screenPoint.xy() + ScreenOffset);

							bool inView = Vector3.Dot(cam.transform.forward.normalized, (cam.transform.position - worldPos).normalized) < 0;

							if (!inView)
								alphaMultipler = 0;
						}

						break;
				}
			}

			if (DistanceScale && PositionMode == ElemPositionMode.AnchoredToWorldPoint)
			{
				float dist  = Vector3.Distance(worldPos, GameCams.Live.UnityCam.transform.position);
				float scale = DistanceToScale.Evaluate(dist);

				scaleMultiplier = scale;
			}

			if (DistanceFade)
			{
				if (DistanceFadeCenterPoint.TryGet(out Vector3 centerPoint))
				{
					float distance = worldPos.Distance(centerPoint);

					alphaMultipler *= 1.0f - Mathf.Clamp01(
						(distance - DistanceFadeStart) / (DistanceFadeEnd - DistanceFadeStart));
				}
			}

			if (!Invisible)
				_canvasGroup.alpha = Alpha * alphaMultipler;
			else
				_canvasGroup.alpha = 0;

			if (_canvasGroup.alpha <= Mathf.Epsilon) {
				_canvasGroup.blocksRaycasts = false;
			} else {
				_canvasGroup.blocksRaycasts = _blocksRaycasts;
			}

			_canvasGroup.interactable = Interactable;

			if(!ManualScale) {
				PivotRect.localScale = Scale.value * scaleMultiplier;
			}

			if(!ManualRotation) {
				PivotRect.localRotation = Quaternion.Euler(Rotation);
			}
		}

		public void SetChildrenActive(bool active)
		{
			Transform[] children = GetComponentsInChildren<Transform>(true);

			foreach (Transform child in children)
			{
				if (child != transform)
				{
					child.gameObject.SetActive(active);
				}
			}
		}

		//TODO: make free positioning mode work for HUDElement
		public void SetPositionModeFree(Vector2 position, bool percentage = false)
		{
			PositionMode             = ElemPositionMode.Free;
			FreePosition             = position;
			IsFreePositionPercentage = percentage;
		}

		public void SetPositionModeFree(float? x, float? y, bool percentage = false)
		{
			PositionMode = ElemPositionMode.Free;
			if (x.HasValue) FreePosition.x = x.Value;
			if (y.HasValue) FreePosition.y = y.Value;
			IsFreePositionPercentage = percentage;
		}

		public void SetPositionModeWorldPoint(WorldPoint anchor, Vector3 anchorOffset)
		{
			PositionMode      = ElemPositionMode.AnchoredToWorldPoint;
			WorldAnchor       = anchor;
			WorldAnchorOffset = anchorOffset;
		}

		public void DisableDistanceFade()
		{
			DistanceFade = false;
		}

		public void EnableDistanceFade(float start, float end)
		{
			DistanceFade      = true;
			DistanceFadeStart = start;
			DistanceFadeEnd   = end;
		}

		// Alpha
		public ManagedTween DoAlphaFade(float target, float duration, Ease      ease = Ease.Unset)                     => new ManagedTween(DoAlphaFadeBase(target, duration, new EaserTo(duration).Ease(ease)));
		public ManagedTween DoAlphaFade(float target, float duration, TweenerTo tweener)                               => new ManagedTween(DoAlphaFadeBase(target, duration, tweener));
		public ManagedTween DoAlphaFade(float start,  float end,      float     duration, Ease      ease = Ease.Unset) => new ManagedTween(DoAlphaFadeBase(start,  end,      duration, ease));
		public ManagedTween DoAlphaFade(float start,  float end,      float     duration, TweenerTo tweener)           => new ManagedTween(DoAlphaFadeBase(start,  end,      duration));

		public Tween DoAlphaFadeBase(float target, float duration, Ease      ease = Ease.Unset)                     => DoAlphaFadeBase(target, duration, new EaserTo(duration).Ease(ease));
		public Tween DoAlphaFadeBase(float target, float duration, TweenerTo tweener)                               => Alpha.EnsureComplete().To(target, tweener);
		public Tween DoAlphaFadeBase(float start,  float end,      float     duration, Ease      ease = Ease.Unset) => DoAlphaFadeBase(start, end, duration, new EaserTo(duration).Ease(ease));
		public Tween DoAlphaFadeBase(float start,  float end,      float     duration, TweenerTo tweener)           => Alpha.EnsureComplete().FromTo(start, end, tweener);

		// Offset
		public ManagedTween DoOffset(Vector3 target, float duration, Ease ease = Ease.Unset)
			=> DoOffset(target, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoOffset(Vector3 start, Vector3 end, float duration, Ease ease = Ease.Unset)
			=> DoOffset(start, end, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoOffset(Vector3 target, float   duration, TweenerTo tweener)
			=> new ManagedTween(SequenceOffset.EnsureComplete().To(target, tweener));

		public ManagedTween DoOffset(Vector3 start, Vector3 end, float duration, TweenerTo tweener)
			=> new ManagedTween(SequenceOffset.EnsureComplete().FromTo(start, end, tweener));


		// Scale

		public ManagedTween DoScale(Vector3 target, float duration, Ease ease = Ease.Unset)
			=> DoScale(target, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoScale(Vector3 start, Vector3 end, float duration, Ease ease = Ease.Unset)
			=> DoScale(start, end, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoScale(Vector3 target, float   duration, TweenerTo tweener)
			=> new ManagedTween(Scale.EnsureComplete().To(target, tweener));

		public ManagedTween DoScale(Vector3 start, Vector3 end, float duration, TweenerTo tweener)
			=> new ManagedTween(Scale.EnsureComplete().FromTo(start, end, tweener));


		// Rotation

		public ManagedTween DoRotation(Vector3 target, float duration, Ease ease = Ease.Unset)
			=> DoRotation(target, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoRotation(Vector3 start, Vector3 end, float duration, Ease ease = Ease.Unset)
			=> DoRotation(start, end, duration, new EaserTo(duration).Ease(ease));

		public ManagedTween DoRotation(Vector3 target, float duration, TweenerTo tweener)
			=> new ManagedTween(Rotation.EnsureComplete().To(target, tweener));

		public ManagedTween DoRotation(Vector3 start, Vector3 end, float duration, TweenerTo tweener)
			=> new ManagedTween(Rotation.EnsureComplete().FromTo(start, end, tweener));

		public Tween DoAlpha(float      to, Easer ease) => Alpha.EnsureComplete().To(to, ease);
		public Tween DoScale(Vector3    to, Easer ease) => Scale.EnsureComplete().To(to, ease);
		public Tween DoOffset(Vector2   to, Easer ease) => SequenceOffset.EnsureComplete().To(to, ease);
		public Tween DoRotation(Vector3 to, Easer ease) => Rotation.EnsureComplete().To(to, ease);


		public Tween DoAlpha(float      from, float   to, Easer ease) => Alpha.EnsureComplete().FromTo(from, to, ease);
		public Tween DoScale(Vector3    from, Vector3 to, Easer ease) => Scale.EnsureComplete().FromTo(from, to, ease);
		public Tween DoOffset(Vector2   from, Vector2 to, Easer ease) => SequenceOffset.EnsureComplete().FromTo(from, to, ease);
		public Tween DoRotation(Vector3 from, Vector3 to, Easer ease) => Rotation.EnsureComplete().FromTo(from, to, ease);

		public class HUDElementProxy : MonoLuaProxy<HUDElement>
		{
			public float   alpha         { get => proxy.Alpha;        set => proxy.Alpha = value; }
			public Vector2 free_position { get => proxy.FreePosition; set => proxy.FreePosition = value; }

			public ManagedTween do_alpha_fade(float target, float duration)            => proxy.DoAlphaFade(target, duration);
			public ManagedTween do_alpha_fade(float start,  float end, float duration) => proxy.DoAlphaFade(start, end, duration);

			public ManagedTween do_offset(Vector3 target, float   duration)            => proxy.DoOffset(target, duration);
			public ManagedTween do_offset(Vector3 start,  Vector3 end, float duration) => proxy.DoOffset(start, end, duration);

			public ManagedTween do_scale(Vector3 target, float   duration)            => proxy.DoScale(target, duration);
			public ManagedTween do_scale(Vector3 start,  Vector3 end, float duration) => proxy.DoScale(start, end, duration);
		}
	}
}
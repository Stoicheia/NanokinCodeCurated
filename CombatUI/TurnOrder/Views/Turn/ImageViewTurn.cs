using Anjin.EditorUtility;
using Anjin.Util;
using Combat.Data;
using Combat.Features.TurnOrder.Events;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using UnityUtilities;
using Util.Odin.Attributes;

namespace Combat.UI.TurnOrder
{
	public class ImageViewTurn : ViewTurn {

		[SerializeField, Required] private Image            ImgContent;
		[SerializeField, Required] private Image            ImgContentFill;
		[SerializeField, Required] private Image[]          ImgContentMasks;
		[SerializeField, Required] private Image            ImgFrame;
		[SerializeField, Required] private Image            ImgFrameShadow;
		[SerializeField, Required] private Image            ImgLight;
		[SerializeField, Required] private Graphic          GfcNumber;
		[SerializeField, Required] private TextMeshProMulti TmpNumber;
		[SerializeField, Required] public  Graphic[]        OpacityGraphics;

		public bool SetNativeSize = true;

		[SerializeField, Space] private AnimationCurve ContentShadingCurve;

		private Color fillColor  = Color.clear;
		private Color frameColor = Color.clear;

		private RectTransform   _rectFrame;
		private FriendnessStyle _friendStyle;

		private bool IsCondensed(ViewStyle style) => StateStyles.enableCondensed && style.condense;

		protected override void Awake()
		{
			base.Awake();

			_rectFrame = ImgFrame.transform.parent.GetComponent<RectTransform>();
		}

		private void OnEnable()
		{
			Rect = GetComponent<RectTransform>();
		}

		protected override void OnEventChanged([CanBeNull] ITurnActer old, [CanBeNull] ITurnActer @new)
		{
			if (@new != null)
				SetSprite(@new.GetEventSprite()).Forget(); // this could backfire if loading takes too longelse
			else
				SetSprite(null);
		}

		protected override void OnTriggerChanged([CanBeNull] Trigger old, [CanBeNull] Trigger @new)
		{
			if ((@new.GetEnv()).skill != null)
				SetSprite(@new.GetEnv().skill.user.GetEventSprite()).Forget(); // this could backfire if loading takes too longelse
			else
				SetSprite(null);
		}

		private async UniTask SetSprite(UniTask<Sprite> task)
		{
			SetSprite(await task);
		}

		private void SetSprite(Sprite sprite)
		{
			ImgContent.sprite  = sprite;
			ImgContent.enabled = sprite != null;
		}


		protected override void SetNumber(int value)
		{
			TmpNumber.Text = value.ToString();
			//StackedCard1.gameObject.SetActive(value > 1);
			//StackedCard2.gameObject.SetActive(value > 2);
		}

		protected override void SetNumberShow(bool b)
		{
			TmpNumber.gameObject.SetActive(b);

			//StackedCard1.gameObject.SetActive(b);
			//StackedCard2.gameObject.SetActive(b);
		}

		public override void SetStates(ViewStates state)
		{

			ViewStyle oldstyle = style;
			style      = StateStyles.Get(state);
			this.state = state;

			if (state == ViewStates.Inactive)
			{
				gameObject.SetActive(false);
				return;
			}

			gameObject.SetActive(true);

			if (state == ViewStates.Stacked && vInfo.stackHeadState.HasValue) {
				ViewStyle headStyle = StateStyles.Get(vInfo.stackHeadState.Value);

				// Get an RNG val that is constant for our particular action
				float rng = GetRNGVal();

				float scale_mag = headStyle.scale * Mathf.Pow(style.scale, Info.groupIndex) + (style.scaling_rng.Lerp(rng));

				offset.value.value = headStyle.offset   + style.offset   * scale_mag * Info.groupIndex + new Vector2(style.offset_x_rng.Lerp(rng), style.offset_y_rng.Lerp(rng)) * scale_mag;
				rotation.value     = headStyle.rotation + style.rotation * scale_mag * Info.groupIndex + (style.rotation_rng.Lerp(rng)) * scale_mag;
				scale.Value        = Vector2.one * scale_mag;

				shading.Value = headStyle.shading  + style.shading                                   * Info.groupIndex;
				opacity.Value = headStyle.opacity  + style.opacity                                   * Info.groupIndex;
			} else {
				scale.Value        = Vector2.one * style.scale;
				rotation.value     = style.rotation;
				shading.Value      = style.shading;
				opacity.Value      = style.opacity;
				offset.value.value = style.offset;
			}


			UpdateStateImages();
		}

		protected override void UpdateStateImages()
		{
			if (!Rect)
				Rect = GetComponent<RectTransform>();

			if (IsCondensed(style))
			{
				ImgFrame.sprite       = StateStyles.frameSpriteCondensed;
				ImgFrameShadow.sprite = StateStyles.frameSpriteCondensed;
				foreach (Image mask in ImgContentMasks)
				{
					mask.sprite = StateStyles.contentMaskCondensed;
				}

				Rect.sizeDelta = new Vector2(GetDesiredWidth(this.state), Rect.sizeDelta.y);
			}
			else
			{
				ImgFrame.sprite       = StateStyles.frameSprite;
				ImgFrameShadow.sprite = StateStyles.frameSprite;
				foreach (Image mask in ImgContentMasks)
				{
					mask.sprite = StateStyles.contentMask;
					if(SetNativeSize)
						mask.SetNativeSize();
				}

				//StackedCard1.sprite = StateStyles.frameSprite;
				//StackedCard2.sprite = StateStyles.frameSprite;

				Rect.sizeDelta = new Vector2(StateStyles.width, Rect.sizeDelta.y);
			}

			foreach (Image img in ImgContentMasks)
			{
				if(SetNativeSize)
					img.SetNativeSize();
			}

			if(SetNativeSize)
				ImgFrame.SetNativeSize();

			if(SetNativeSize)
				ImgFrameShadow.SetNativeSize();

			//StackedCard1.SetNativeSize();
			//StackedCard2.SetNativeSize();
		}


		private int GetDesiredWidth(ViewStates state)
		{
			ViewStyle style = StateStyles.Get(state);
			return IsCondensed(style) ? StateStyles.widthCondensed : StateStyles.width;
		}

		public override void SetFriendness(ViewFriendness friendness)
		{
			base.SetFriendness(friendness);
			_friendStyle = GetStyleForFriendness(friendness);
		}


		/// <summary>
		/// The final desired size of the action, excluding animation.
		/// </summary>
		/// <param name="vi"></param>
		/// <returns></returns>
		public override Vector2 GetDesiredSize(ViewInfo state)
		{
			ViewStyle style = StateStyles.Get(state.state);
			return new Vector2(GetDesiredWidth(state.state), Rect.sizeDelta.y) * style.scale;
		}

		protected void LateUpdate()
		{
			Profiler.BeginSample("Update transform");
			Rect.anchoredPosition = position.value + offset.Value;
			Rect.localScale       = new Vector3(scale.Value.x, scale.Value.y, 1);
			Rect.localRotation    = Quaternion.Euler(0, 0, rotation.value);
			Profiler.EndSample();

			Profiler.BeginSample("Update images");
			float shading = Mathf.Max(this.shading.Value, 1 - selection.brightness); // Selection cancels out shading

			Color frameColor   = _friendStyle.frameColor.Lerp(Color.black, shading);
			Color contentColor = _friendStyle.contentColor.Lerp(Color.black, shading);
			Color lightColor   = _friendStyle.lightColor.Lerp(Color.black, shading);
			Color textColor    = _friendStyle.textColor.Lerp(Color.black, shading);

			ImgContent.color = contentColor;
			ImgFrame.color   = frameColor;
			ImgLight.color   = lightColor;
			GfcNumber.color  = textColor;

			float shadingPower = (ContentShadingCurve.Evaluate(shading));

			fillColor = fill.Value.Lerp(Color.black.Alpha(shadingPower), shadingPower).ScaleAlpha(opacity.Value);

			ImgContentFill.color = fillColor;
			ImgFrame.color       = frameColor;

			Profiler.EndSample();

			Profiler.BeginSample("Update opacity graphics");
			for (var i = 0; i < OpacityGraphics.Length; i++)
			{
				Graphic graphic = OpacityGraphics[i];
				if (graphic == null) continue;
				graphic.color = graphic.color.Alpha(opacity.Value);
			}

			Profiler.EndSample();
		}

		public override string ToString() => $"{gameObject.name} : action={Action.id}, ev={Action.acter}";
	}
}
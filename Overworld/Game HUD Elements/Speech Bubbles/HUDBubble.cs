using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Anjin.UI
{
	[RequireComponent(typeof(HUDElement))]
	public abstract class HUDBubble : SerializedMonoBehaviour
	{
		public enum State { Off, On, Transition }

		[FoldoutGroup("References")] public HUDElement         hudElement;
		[FoldoutGroup("References")] public Transform          BubbleDisplay;
		[FoldoutGroup("References")] public RectTransform      BubbleBox;
		[FoldoutGroup("References")] public UISlideAlongParent Slider;

		public float? 	Height;
		public float? 	Width;
		public float? 	MaxWidth;
		public float 	NormalMaxWidth;

		[FormerlySerializedAs("MaxSizeLayout")]
		public LayoutElement TextLayout;

		public LayoutMaxSize MaxSizeLayout;

		public List<MonoBehaviour> LayoutElements;
		public List<RectTransform> StretchRects;

		[NonSerialized]
		public State state = State.Off;

		public virtual void Awake()
		{
			Slider = GetComponentInChildren<UISlideAlongParent>();
			hudElement = GetComponent<HUDElement>();
			hudElement.SetChildrenActive(false);
			hudElement.Alpha = 0;
		}

		public virtual void Update()
		{
			for (int i = 0; i < LayoutElements.Count; i++)
				LayoutElements[i].enabled = ( !Width.HasValue && !Height.HasValue );

			if (Width.HasValue || Height.HasValue) {
				var prev_delta = BubbleBox.sizeDelta;

				if (Width.HasValue) 	prev_delta.x = Width.Value;
				if (Height.HasValue)	prev_delta.y = Height.Value;

				BubbleBox.sizeDelta = prev_delta;

				for (int i = 0; i < StretchRects.Count; i++) {
					var r = StretchRects[i];
					r.anchorMin = Vector2.zero;
					r.anchorMax = Vector2.one;

					r.offsetMin = Vector2.zero;
					r.offsetMax = Vector2.zero;
				}
			}

			if (TextLayout != null) {
				TextLayout.preferredWidth = MaxWidth.GetValueOrDefault(-1);

				if (MaxWidth != null) {
					MaxSizeLayout.maxWidth = -1;
				} else {
					MaxSizeLayout.maxWidth = NormalMaxWidth;
				}
			}

		}

		public virtual void OnEnter() { }
		public virtual void OnDone()  { }

		[Button]
		public void StartActivation()
		{
			if (state != State.Off) return;

			//Don't do this
			//await UniTask.WaitForEndOfFrame();

			state = State.On;
			hudElement.SetChildrenActive(true);
			OnEnter();
			hudElement.DoAlphaFade(0, 1, 0.2f);
			hudElement.DoOffset(Vector3.up * -0.3f, Vector3.zero, 0.2f);
		}

		public void StartDeactivation(bool doAnim)
		{
			if (state == State.On)
			{
				if(doAnim) {
					state = State.Transition;
					hudElement.DoAlphaFade(1, 0, 0.3f);
					hudElement.DoOffset(Vector3.zero, Vector3.up * -0.3f, 0.3f).Tween.OnComplete(OnDone);
				} else {
					OnDone();
				}
			}
		}

		public void DeactivateFinish()
		{
			state = State.Off;
			ResetSettings();
			hudElement.SetChildrenActive(false);
		}

		public virtual void ResetSettings()
		{
			Width = null;
			Height = null;

			MaxWidth = null;

			hudElement.PositionMode      = HUDElement.ElemPositionMode.Free;
			hudElement.FreePosition      = Vector2.zero;
			hudElement.WorldAnchorOffset = Vector3.zero;
			hudElement.ScreenOffset 	 = Vector2.zero;

			Slider.alignment     = UISlideAlongParent.Alignment.Top;
			Slider.slidePosition = 0.5f;
		}

		public virtual void ApplySettings(Table tbl)
		{
			if (tbl == null) return;


			if (tbl.TryGet("size", out Vector2 size)) {
				Width = size.x;
				Height = size.x;
			} else {
				var has_width 	= tbl.TryGet("width", out float width);
				var has_height 	= tbl.TryGet("height", out float height);
				if(has_width) 					Width 	= width;
				if(has_height) 					Height 	= height;
			}

			if (tbl.TryGet("offset", out Vector2 offset)) {
				hudElement.ScreenOffset = offset;
			}

			if (tbl.TryGet("world_offset", out Vector3 world_offset)) {
				hudElement.PositionMode      = HUDElement.ElemPositionMode.AnchoredToWorldPoint;
				hudElement.WorldAnchorOffset = world_offset;
			}

			if (tbl.TryGet("pos", out Vector2 pos)) {
				hudElement.SetPositionModeFree(pos);
			} else if (tbl.TryGet("pos_float", out Vector2 pos_float)) {
				hudElement.SetPositionModeFree(pos_float, true);
			} else {
				var is_x = tbl.TryGet("x", out float x);
				var is_y = tbl.TryGet("y", out float y);
				if (is_x || is_y) {
					hudElement.PositionMode = HUDElement.ElemPositionMode.Free;
					hudElement.SetPositionModeFree(x, y);
				}
			}

			//var align = tbl.Get("align"); //"top", "bottom", "left", "right"
			if(tbl.TryGet("align", out string align)) {
				switch (align) {
					case "top":    Slider.alignment = UISlideAlongParent.Alignment.Top; break;
					case "bottom": Slider.alignment = UISlideAlongParent.Alignment.Bottom; break;
					case "left":   Slider.alignment = UISlideAlongParent.Alignment.Left; break;
					case "right":  Slider.alignment = UISlideAlongParent.Alignment.Right; break;
				}
			}

			//var slide = tbl.Get("slide");
			if (tbl.TryGet("slide", out float slide) /*slide.IsNotNil() && slide.Type == DataType.Number && Slider != null*/)
				Slider.slidePosition = slide;

			if (tbl.TryGet("max_width", out float max_width)) {
				MaxWidth = max_width;
			}

			var max_height = tbl.Get("max_height");
		}
	}
}
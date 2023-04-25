using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI.Extensions;
using Util.Odin.Attributes;

[ExecuteAlways]
public class UISlideAlongParent : SerializedMonoBehaviour
{
	public enum Alignment { Top, Bottom, Left, Right }

	public bool AutoAlign;

	[EnumToggleButtons, DisableIf("AutoAlign")]
	public Alignment alignment;

	public RectTransform selfRT;
	public RectTransform parentRT;

	[Range01]
	public float slidePosition;
	public float margin = 32;
	public float CenterOffset;

	public RectTransform Tag;
	public Vector2       TagOffset;
	public float         TagRotationOffset;

	[NonSerialized]
	public Canvas parentCanvas;

	//public Vector3 position;

	void Awake()
	{
		if (!Application.IsPlaying(gameObject)) return;

		parentCanvas = selfRT.GetParentCanvas();
	}

	void LateUpdate()
	{
		if (!Application.IsPlaying(gameObject)) return;

		if (AutoAlign)
		{
			var pos = parentRT.anchoredPosition.xy();

			var normalizedPos = pos / parentCanvas.pixelRect.size;

			if (normalizedPos.x < 0.5)
			{
				alignment = Alignment.Right;
				/*if (normalizedPos.y < 0.5f)
				else
					alignment = Alignment.Left;*/
			}
			else
			{
				alignment = Alignment.Left;
			}
		}

		slidePosition = Mathf.Clamp01(slidePosition);

		Vector2 pivot   = Vector2.zero;
		float   xOffset = 0;
		float   yOffset = 0;

		float   tagRot   = 0;
		Vector2 tagScale = Vector2.one;

		switch (alignment)
		{
			case Alignment.Top:
				pivot   = new Vector2(1, 0);
				yOffset = CenterOffset;
				break;

			case Alignment.Bottom:
				pivot   = new Vector2(1, 1);
				yOffset = -CenterOffset;

				tagRot     = 180;
				tagScale.x = -1;
				break;

			case Alignment.Left:
				pivot   = new Vector2(1, 1);
				xOffset = -CenterOffset;

				tagRot     = 90;
				tagScale.y = -1;
				break;

			case Alignment.Right:
				pivot   = new Vector2(0, 1);
				xOffset = CenterOffset;

				tagRot = 270;
				break;
		}

		selfRT.pivot = pivot;
		Vector2 slidePos = Vector2.zero;

		{
			var size  = (alignment == Alignment.Bottom || alignment == Alignment.Top) ? selfRT.sizeDelta.x : selfRT.sizeDelta.y;
			var slide = ((size - margin * 2) * slidePosition) + margin;

			var tagPos = Vector2.zero;

			if (alignment == Alignment.Bottom || alignment == Alignment.Top)
				slidePos = new Vector2(slide, 0) + new Vector2(xOffset, yOffset);
			else if (alignment == Alignment.Left || alignment == Alignment.Right)
				slidePos = new Vector2(0, slide) + new Vector2(xOffset, yOffset);

			if (Tag != null)
			{
				if (alignment == Alignment.Bottom)
					tagPos = new Vector2(size - selfRT.anchoredPosition.x, selfRT.sizeDelta.y);
				else if (alignment == Alignment.Top)
					tagPos = new Vector2(size - selfRT.anchoredPosition.x, 0);
				else if (alignment == Alignment.Left)
					tagPos = new Vector2(selfRT.sizeDelta.x, size - selfRT.anchoredPosition.y);
				else if (alignment == Alignment.Right)
					tagPos = new Vector2(0, size - selfRT.anchoredPosition.y);

				Tag.anchoredPosition = new Vector2(
					(alignment == Alignment.Bottom || alignment == Alignment.Top) ? Mathf.Clamp(tagPos.x, margin, selfRT.sizeDelta.x - margin) : tagPos.x,
					(alignment == Alignment.Left || alignment == Alignment.Right) ? Mathf.Clamp(tagPos.y, margin, selfRT.sizeDelta.y - margin) : tagPos.y);

				Tag.localScale = new Vector3(tagScale.x, 1, tagScale.y);
				var rot = Tag.localRotation.eulerAngles;
				rot.z             = tagRot + TagRotationOffset;
				Tag.localRotation = Quaternion.Euler(rot);
			}
		}

		{
			// Vector2 canvasSize = parentCanvas.pixelRect.size;
			// Vector2 scale = canvasSize / new Vector2(1920, 1080);

			// var size = selfRT.rect.size;
			// var pivotSize = size * selfRT.pivot;

			// Vector2 topLeftPos = parentRT.anchoredPosition.xy() - new Vector2(pivotSize.x, pivotSize.y) + slidePos;
			// selfRT.position = new Vector2(
			// Mathf.Clamp(topLeftPos.x, -1, 1920 - size.x) + pivotSize.x,
			// Mathf.Clamp(topLeftPos.y, 0, 1080 - size.y) + pivotSize.y) * scale;

			selfRT.localPosition = slidePos;
		}
	}
}
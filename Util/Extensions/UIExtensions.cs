using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Util.Extensions
{
	public static class UIExtensions
	{
		// Shared array used to receive result of RectTransform.GetWorldCorners

		public static EventTrigger.Entry AddEntry(this EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> handler = null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry {eventID = type};

			if (handler != null)
			{
				entry.callback.AddListener(handler);
			}

			trigger.triggers.Add(entry);
			return entry;
		}

		public static void SetButtonText(this Button button, string text)
		{
			switch (button.targetGraphic) {
				case TMP_Text tmp_text:
					tmp_text.text = text;
					break;

				case Text utext:
					utext.text = text;
					break;
			}
		}

	#region Canvas

		/// <summary>
		/// Calulates Position for RectTransform.position from a transform.position. Does not Work with WorldSpace Canvas!
		/// </summary>
		/// <param name="canvas"> The Canvas parent of the RectTransform.</param>
		/// <param name="position">Position of in world space of the "Transform" you want the "RectTransform" to be.</param>
		/// <param name="cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
		/// <returns></returns>
		public static Vector3 CalculatePositionFromTransformToRectTransform(this Canvas canvas, Vector3 position, Camera cam)
		{
			switch (canvas.renderMode)
			{
				case RenderMode.ScreenSpaceOverlay:
					return cam.WorldToScreenPoint(position);

				case RenderMode.WorldSpace:
				case RenderMode.ScreenSpaceCamera:
					RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, cam.WorldToScreenPoint(position), cam, out Vector2 tempVector);
					return canvas.transform.TransformPoint(tempVector);

				default: throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Calulates Position for RectTransform.position Mouse Position. Does not Work with WorldSpace Canvas!
		/// </summary>
		/// <param name="canvas">The Canvas parent of the RectTransform.</param>
		/// <param name="cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
		/// <returns></returns>
		public static Vector3 CalculatePositionFromMouseToRectTransform(this Canvas canvas, Camera cam = null)
		{
			var canvasRect = canvas.transform as RectTransform;

			Vector2 mousePos = Mouse.current.position.ReadValue();
			Vector3 scaledMousePos = new Vector2(mousePos.x / Screen.width * canvasRect.sizeDelta.x,
				mousePos.y / Screen.height * canvasRect.sizeDelta.y);

			return scaledMousePos;

			// None of this shit below actually works
			// This is what everyone online suggest.......
			//
			// On the other hand, the simple maths I wrote above are
			// sound and working well for all resolutions.
			// Either these people are on drugs, or Unity devs
			// fucked with the utility function and messed it up

			// switch (canvas.renderMode)
			// {
			// 	case RenderMode.ScreenSpaceOverlay:
			// 		return scaledMousePos;
			//
			// 	case RenderMode.WorldSpace:
			// 	case RenderMode.ScreenSpaceCamera:
			// 	{
			// 		// Vector3 scaledMousePos = new Vector2(mousePos.x * scaler.referenceResolution.x / Screen.width,
			// 		// mousePos.y * scaler.referenceResolution.y / Screen.height);
			//
			// 		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
			// 																mousePos,
			// 																cam,
			// 																out Vector2 rectPoint);
			//
			// 		return canvas.transform.TransformPoint(rectPoint);
			// 	}
			//
			// 	default: throw new NotImplementedException();
			// }
		}

		/// <summary>
		/// Calculates Position for "Transform".position from a "RectTransform".position. Does not Work with WorldSpace Canvas!
		/// </summary>
		/// <param name="canvas">The Canvas parent of the RectTransform.</param>
		/// <param name="position">Position of the "RectTransform" UI element you want the "Transform" object to be placed to.</param>
		/// <param name="cam">The Camera which is used. Note this is useful for split screen and both RenderModes of the Canvas.</param>
		/// <returns></returns>
		public static Vector3 CalculatePositionFromRectTransformToTransform(this Canvas canvas, Vector3 position, Camera cam)
		{
			switch (canvas.renderMode)
			{
				case RenderMode.ScreenSpaceOverlay:
					return cam.ScreenToWorldPoint(position);

				case RenderMode.WorldSpace:
				case RenderMode.ScreenSpaceCamera:
					RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, cam.WorldToScreenPoint(position), cam, out Vector3 ret);
					return ret;

				default: throw new NotImplementedException();
			}
		}

	#endregion
	}
}
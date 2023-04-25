using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Overworld.Shopping
{
	public class ScrollWheelSurface : MonoBehaviour, IScrollHandler
	{
		[SerializeField] public ScrollEvent onScrolledDown;
		[SerializeField] public ScrollEvent onScrolledUp;

		public void OnScroll(PointerEventData eventData)
		{
			if (eventData.scrollDelta.y > 0)
			{
				onScrolledUp.Invoke((int) eventData.scrollDelta.y);
			}
			else if (eventData.scrollDelta.y < 0)
			{
				onScrolledDown.Invoke((int) eventData.scrollDelta.y);
			}

			eventData.Use();
		}


		[Serializable]
		public class ScrollEvent : UnityEvent<int>
		{ }
	}
}
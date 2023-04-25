using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Overworld.UI
{
	public class MenuAnimEventProcessor : SerializedMonoBehaviour
	{
		[SerializeField] private Dictionary<string, UnityEvent> animationEvents;

		public void RunAnimationEvent(string eventName)
		{
			if (animationEvents.ContainsKey(eventName))
			{
				animationEvents[eventName]?.Invoke();
			}
		}
	}
}

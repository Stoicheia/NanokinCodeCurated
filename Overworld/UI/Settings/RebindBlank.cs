using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overworld.UI.Settings
{
	public class RebindBlank : MonoBehaviour
	{
		[SerializeField] private ButtonControl Button;

		void Awake()
        {
			Button.OnSelected = OnSelected;
			Button.OnDeselected = OnDeselected;
		}

		public void OnSelected(UnityEngine.EventSystems.BaseEventData data)
		{
			SettingsMenu.Live.OnRebindInputSelected(data, Button); 
		}

		public void OnDeselected(UnityEngine.EventSystems.BaseEventData data)
		{
			SettingsMenu.Live.OnRebindInputDeselected(data, Button);
		}
	}
}

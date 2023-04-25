using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overworld.UI.Settings
{
	public class ControlInteraction : MonoBehaviour
	{
		[SerializeField] private SettingsMenuControlBase control;

		public void Interact()
        {
			control.Interact();
        }
	}
}

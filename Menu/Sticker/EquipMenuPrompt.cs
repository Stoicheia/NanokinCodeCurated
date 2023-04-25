using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Menu.Sticker
{
	public class EquipMenuPrompt : MonoBehaviour
	{
		[SerializeField] private List<EquipMenuState> activeStates;

		public void ToggleActivity(params object[] args)
		{
			EquipMenuState state = (EquipMenuState)args[0];
			gameObject.SetActive(activeStates.Contains(state));
		}
	}
}

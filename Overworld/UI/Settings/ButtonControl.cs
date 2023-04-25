using System;
using Anjin.EditorUtility.UIShape;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overworld.UI.Settings {
	public class ButtonControl : SettingsMenuControlBase {
		public Button Button;
		public Action OnClicked;
		public Action<Selectable> OnUpdateUI;

		public override void Awake()
		{
			base.Awake();
			Button.onClick.AddListener(() => OnClicked?.Invoke()); 
		}

		public override Selectable Selectable => Button;

		public override void Interact()
		{
			if (Selectable.interactable)
			{
				Button.onClick?.Invoke();
			}
		}

		public override void UpdateUI()
		{
			OnUpdateUI?.Invoke(Selectable);
		}
	}
}
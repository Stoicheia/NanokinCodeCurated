using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

namespace Overworld.UI.Settings {
	public class DropdownControl : SettingsMenuControl<int> {
		public TMP_Dropdown Dropdown;

		public override void Awake()
		{
			base.Awake();
			Dropdown.onValueChanged.AddListener(val => OnChanged?.Invoke(val));
		}

		[Button]
		public void Setup(List<string> options, int selected)
		{
			Dropdown.ClearOptions();
			Dropdown.AddOptions(options);
			Set(selected);
		}

		public override Selectable Selectable => Dropdown;

		[Button]
		public override void Set(int val)
		{
			Dropdown.SetValueWithoutNotify(val);
		}

		public override void Interact()
		{
			if (!Dropdown.IsExpanded)
			{
				Dropdown.Show();
			}
		}
	}
}
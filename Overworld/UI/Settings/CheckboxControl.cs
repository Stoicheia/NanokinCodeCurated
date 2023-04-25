using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Overworld.UI.Settings {

	public class CheckboxControl : SettingsMenuControl<bool> {
		public Toggle   Toggle;

		public GameObject Checkmark;

		public override void Awake()
		{
			base.Awake();
			Toggle.onValueChanged.AddListener(val => { OnChanged?.Invoke(val); });
		}

		public override Selectable Selectable => Toggle;

		[Button]
		public override void Set(bool val) => Toggle.SetIsOnWithoutNotify(val);

		public override void Interact()
		{
			Toggle.isOn = !Toggle.isOn;

			//Toggle.onValueChanged?.Invoke(!Toggle.isOn);
			//Checkmark.SetActive(Toggle.isOn);
		}
	}
}
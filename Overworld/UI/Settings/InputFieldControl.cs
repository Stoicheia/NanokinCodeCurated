using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

namespace Overworld.UI.Settings {
	public class InputFieldControl : SettingsMenuControl<string> {
		public TMP_InputField InputField;

		public override void Awake()
		{
			base.Awake();
			InputField.onSubmit.AddListener(val => OnChanged?.Invoke(val));
		}

		public override Selectable Selectable => InputField;

		[Button]
		public override void Set(string val) => InputField.SetTextWithoutNotify(val);
	}
}
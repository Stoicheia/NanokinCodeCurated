using Anjin.EditorUtility;
using Anjin.Nanokin;
using JetBrains.Annotations;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Util.Odin.Attributes;

namespace Core.Input {
	public class InputJoystickLabel : MonoBehaviour
	{
		[SerializeField] private TMP_Text         Text;
		[SerializeField] private TextMeshProMulti MultiText;

		[SerializeField] private Image Icon1;
		[SerializeField] private Image Icon2;
		[SerializeField] private Image Icon3;
		[SerializeField] private Image Icon4;

		[ShowInPlay]
		private GameInputs.Joystick _joystick;

		public GameInputs.Joystick Joystick
		{
			set
			{
				_joystick = value;
				Refresh();
			}
		}

		[CanBeNull]
		public string ButtonName;

		private void Awake()
		{
			//if (Text == null && MultiText == null)
			//{
			//	MultiText = GetComponentInChildren<TextMeshProMulti>();
			//	if (MultiText == null)
			//	{
			//		Text = GetComponentInChildren<TMP_Text>();
			//		if (Text == null)
			//		{
			//			this.LogError("A TextMeshPro component (or TextMeshProMulti) is required for InputButtonLabel.");
			//			return;
			//		}
			//	}
			//}

			Refresh();
		}

		private void Start()
		{
			GameInputs.DeviceChanged += OnDeviceChanged;

			if (!ButtonName.IsNullOrWhitespace()) {
				if (GameInputs.FieldNamesToControls.TryGetValue(ButtonName, out GameInputs.IHasInputAction _action) && _action is GameInputs.Joystick joystick) {
					_joystick = joystick;
				}
			}

			Refresh();
		}

		private void OnDestroy() => GameInputs.DeviceChanged -= OnDeviceChanged;
		private void OnEnable()  => Refresh();

		private void OnDeviceChanged(InputDevices device)
		{
			Refresh();
		}

		private void Refresh()
		{
			//if (Text == null && MultiText == null) return;
			if (_joystick == null /*|| Icon1 == null || Icon2 == null || Icon3 == null || Icon4 == null*/) return;

			/*if(Text != null)
				Text.gameObject.SetActive(false);*/

			//Sprite icon = GameInputs.GetInputIcon(_joystick.InputAction);

			//Text.text = _joystick.GetBindingDisplayString();

			Sprite icon = null;
			if(GameInputs.ActiveDevice == InputDevices.Gamepad)
				icon = GameInputs.GetInputIcon(_joystick.InputAction);

			if (icon != null) {
				Icon1.gameObject.SetActive(true);
				Text.gameObject.SetActive(false);
				Icon1.sprite = icon;
			}  else {
				Icon1.gameObject.SetActive(false);
				Text.gameObject.SetActive(true);
				Text.text = _joystick.GetBindingDisplayString();
			}


		}
	}
}
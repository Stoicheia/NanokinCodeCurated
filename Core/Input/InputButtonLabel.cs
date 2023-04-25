using System;
using Anjin.EditorUtility;
using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;

namespace Anjin.Nanokin
{
	public class InputButtonLabel : MonoBehaviour
	{
		[SerializeField] private TMP_Text         Text;
		[SerializeField] private TextMeshProMulti MultiText;

		[SerializeField] private UnityEngine.UI.Image InputIcon;

		private GameInputs.ActionButton _button;

		public GameInputs.ActionButton Button
		{
			set
			{
				_button = value;
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
				if (GameInputs.FieldNamesToControls.TryGetValue(ButtonName, out GameInputs.IHasInputAction _action) && _action is GameInputs.ActionButton button) {
					_button = button;
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
			if (_button == null || InputIcon == null) return;

			//string text = _button.GetBindingDisplayString();

			//if (MultiText != null)
			//MultiText.Text = text;
			//else
			//Text.SetText(text);

			Sprite icon = GameInputs.GetInputIcon(_button.InputAction);

			if (icon != null) {
				InputIcon.gameObject.SetActive(true);

				if(Text != null)
					Text.gameObject.SetActive(false);

				InputIcon.sprite = icon;

			}  else if(Text != null) {
				InputIcon.gameObject.SetActive(false);
				Text.gameObject.SetActive(true);
				Text.text = _button.GetBindingDisplayString();
			} else {

				InputIcon.gameObject.SetActive(false);

				if(Text != null)
					Text.gameObject.SetActive(false);
			}
		}
	}
}
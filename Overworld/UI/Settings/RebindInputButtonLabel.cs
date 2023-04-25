using System;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Overworld.UI.Settings
{
	public class RebindInputButtonLabel : MonoBehaviour
	{
		[SerializeField] private InputDevices DeviceGroup;
		[SerializeField] private GamepadType DefaultPlatform;
		[SerializeField] private SettingsMenu.RebindMode Device;

		[SerializeField] private ButtonControl Button;

		[SerializeField] private UnityEngine.UI.Image InputIcon;

		private SettingsMenu.BindingInfo _bindingInfo;

		private GamepadType _currentPlatform;

		public ButtonControl Selectable => Button;

		public SettingsMenu.BindingInfo BindingInfo
		{
			get { return _bindingInfo; }
			set
			{
				_bindingInfo = value;
				Refresh();
			}
		}

		public void ShowRebindPrompt()
		{
			SettingsMenu.Live.ShowRebindPopup(_bindingInfo);
		}

		public void OnSelected(UnityEngine.EventSystems.BaseEventData data)
		{
			SettingsMenu.Live.OnRebindInputSelected(data, Button);
		}

		public void OnDeselected(UnityEngine.EventSystems.BaseEventData data)
		{
			SettingsMenu.Live.OnRebindInputDeselected(data, Button);
		}

		private void Awake()
		{
			_currentPlatform = ((DeviceGroup != InputDevices.Gamepad) ? DefaultPlatform : GameInputs.Live.LastGamepadUsed);

			Selectable.OnSelected = OnSelected;
			Selectable.OnDeselected = OnDeselected;

			//Refresh();
		}

		private void Start()
		{
			GameInputs.DeviceChanged += OnDeviceChanged;

			Refresh();
		}

		private void OnDestroy() => GameInputs.DeviceChanged -= OnDeviceChanged;
		private void OnEnable() => Refresh();

		private void OnDeviceChanged(InputDevices device)
		{
			if (DeviceGroup == InputDevices.Gamepad)
			{
				_currentPlatform = ((device == InputDevices.Gamepad) ? GameInputs.Live.CurrentController : GameInputs.Live.LastGamepadUsed);

				Refresh();
			}
		}

		private void Refresh()
		{
			if (_bindingInfo == null || _bindingInfo.Mapping == "") return;

			Sprite icon = GameInputs.GetInputIconForPlatform(_bindingInfo, _currentPlatform);

			if (icon != null)
			{
				InputIcon.sprite = icon;
				InputIcon.gameObject.SetActive(true);
			}
			else
			{
				InputIcon.gameObject.SetActive(false);
			}
		}
	}
}

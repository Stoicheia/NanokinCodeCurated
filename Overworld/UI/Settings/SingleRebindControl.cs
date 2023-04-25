using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

namespace Overworld.UI.Settings
{
	public class SingleRebindControl : RebindControl
	{
		public RebindInputButtonLabel KeyboardIcon;
		public RebindInputButtonLabel MouseIcon;
		public RebindInputButtonLabel GamepadIcon;

		public TMP_Text KeyboardBlank;
		public TMP_Text MouseBlank;
		public TMP_Text GamepadBlank;

		public ButtonControl KeyboardBlankButton;
		public ButtonControl MouseBlankButton;
		public ButtonControl GamepadBlankButton;

		public override void Set(InputAction action, string category, string name, bool allowDuplicates, bool keyboardRebindAllowed, bool mouseRebindAllowed, bool gamepadRebindAllowed, List<SettingsMenu.BindingInfo> keyboardBindings, List<SettingsMenu.BindingInfo> mouseBindings, List<SettingsMenu.BindingInfo> gamepadBindings)
		{
			AllowDuplicateBindings = allowDuplicates;

			Category = category;
			Name = name;

			this.keyboardRebindAllowed = keyboardRebindAllowed;
			this.mouseRebindAllowed = mouseRebindAllowed;
			this.gamepadRebindAllowed = gamepadRebindAllowed;

			if ((keyboardBindings != null) && (keyboardBindings.Count > 0))
			{
				KeyboardBlank.gameObject.SetActive(false);

				var keyboardBinding = keyboardBindings[0];

				KeyboardIcon.BindingInfo = keyboardBinding;

				//KeyboardIcon.Handler = keyboardBinding.Handler;

				KeyboardIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noKeyboardBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Keyboard", "KeyboardMouse", "");

				KeyboardIcon.BindingInfo = noKeyboardBinding;
				KeyboardIcon.gameObject.SetActive(false);

				KeyboardBlank.text = (keyboardRebindAllowed ? "---" : "N/A");
				KeyboardBlank.gameObject.SetActive(true);
				KeyboardBlankButton.Selectable.interactable = keyboardRebindAllowed;
			}

			if ((mouseBindings != null) && (mouseBindings.Count > 0))
			{
				MouseBlank.gameObject.SetActive(false);

				var mouseBinding = mouseBindings[0];

				MouseIcon.BindingInfo = mouseBinding;

				//MouseIcon.Handler = mouseBinding.Handler;

				MouseIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noMouseBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Mouse", "KeyboardMouse", "");

				MouseIcon.BindingInfo = noMouseBinding;
				MouseIcon.gameObject.SetActive(false);

				MouseBlank.text = (mouseRebindAllowed ? "---" : "N/A");
				MouseBlank.gameObject.SetActive(true);
				MouseBlankButton.Selectable.interactable = mouseRebindAllowed;
			}

			if ((gamepadBindings != null) && (gamepadBindings.Count > 0))
			{
				GamepadBlank.gameObject.SetActive(false);

				var gamepadBinding = gamepadBindings[0];

				GamepadIcon.BindingInfo = gamepadBinding;

				//GamepadIcon.Handler = gamepadBinding.Handler;

				GamepadIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noGamepadBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Gamepad", "Gamepad", "");

				GamepadIcon.BindingInfo = noGamepadBinding;
				GamepadIcon.gameObject.SetActive(false);

				GamepadBlank.text = (gamepadRebindAllowed ? "---" : "N/A");
				GamepadBlank.gameObject.SetActive(true);
				GamepadBlankButton.Selectable.interactable = gamepadRebindAllowed;
			}
		}

		public override void Refresh(List<SettingsMenu.BindingInfo> keyboardBindings, List<SettingsMenu.BindingInfo> mouseBindings, List<SettingsMenu.BindingInfo> gamepadBindings)
		{
			if ((keyboardBindings != null) && (keyboardBindings.Count > 0))
			{
				KeyboardBlank.gameObject.SetActive(false);

				var keyboardBinding = keyboardBindings[0];

				KeyboardIcon.BindingInfo = keyboardBinding;

				//KeyboardIcon.Handler = keyboardBinding.Handler;

				KeyboardIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noKeyboardBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Keyboard", "KeyboardMouse", "");

				KeyboardIcon.BindingInfo = noKeyboardBinding;
				KeyboardIcon.gameObject.SetActive(false);

				KeyboardBlank.text = (keyboardRebindAllowed ? "---" : "N/A");
				KeyboardBlank.gameObject.SetActive(true);
				KeyboardBlankButton.Selectable.interactable = keyboardRebindAllowed;
			}

			if ((mouseBindings != null) && (mouseBindings.Count > 0))
			{
				MouseBlank.gameObject.SetActive(false);

				var mouseBinding = mouseBindings[0];

				MouseIcon.BindingInfo = mouseBinding;

				//MouseIcon.Handler = mouseBinding.Handler;

				MouseIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noMouseBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Mouse", "KeyboardMouse", "");

				MouseIcon.BindingInfo = noMouseBinding;
				MouseIcon.gameObject.SetActive(false);

				MouseBlank.text = (mouseRebindAllowed ? "---" : "N/A");
				MouseBlank.gameObject.SetActive(true);
				MouseBlankButton.Selectable.interactable = mouseRebindAllowed;
			}

			if ((gamepadBindings != null) && (gamepadBindings.Count > 0))
			{
				GamepadBlank.gameObject.SetActive(false);

				var gamepadBinding = gamepadBindings[0];

				GamepadIcon.BindingInfo = gamepadBinding;

				//GamepadIcon.Handler = gamepadBinding.Handler;

				GamepadIcon.gameObject.SetActive(true);
			}
			else
			{
				SettingsMenu.BindingInfo noGamepadBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Gamepad", "Gamepad", "");

				GamepadIcon.BindingInfo = noGamepadBinding;
				GamepadIcon.gameObject.SetActive(false);

				GamepadBlank.text = (gamepadRebindAllowed ? "---" : "N/A");
				GamepadBlank.gameObject.SetActive(true);
				GamepadBlankButton.Selectable.interactable = gamepadRebindAllowed;
			}
		}

		public override List<ButtonControl> GetSelectables()
		{
			selectables.Clear();

			//if (keyboardRebindAllowed)
			//{
				selectables.Add(KeyboardIcon.gameObject.activeSelf ? KeyboardIcon.Selectable : KeyboardBlankButton);
			//}

			//if (mouseRebindAllowed)
			//{
				selectables.Add(MouseIcon.gameObject.activeSelf ? MouseIcon.Selectable : MouseBlankButton);
			//}

			//if (gamepadRebindAllowed)
			//{
				selectables.Add(GamepadIcon.gameObject.activeSelf ? GamepadIcon.Selectable : GamepadBlankButton);
			//}

			return selectables;
		}
	}
}

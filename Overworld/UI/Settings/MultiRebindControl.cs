using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

namespace Overworld.UI.Settings
{
	public class MultiRebindControl : RebindControl
	{
		public List<RebindInputButtonLabel> KeyboardIcons;
		public List<RebindInputButtonLabel> MouseIcons;
		public List<RebindInputButtonLabel> GamepadIcons;

		public List<TMP_Text> KeyboardBlanks;
		public List<TMP_Text> MouseBlanks;
		public List<TMP_Text> GamepadBlanks;

		public List<ButtonControl> KeyboardBlankButtons;
		public List<ButtonControl> MouseBlankButtons;
		public List<ButtonControl> GamepadBlankButtons;

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
				for (int i = 0; i < keyboardBindings.Count; i++)
				{
					TMP_Text keyboardBlank = KeyboardBlanks[i];
					keyboardBlank.gameObject.SetActive(false);

					var keyboardBinding = keyboardBindings[i];

					RebindInputButtonLabel inputLabel = KeyboardIcons[i];

					inputLabel.BindingInfo = keyboardBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < KeyboardIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = KeyboardIcons[i];

					SettingsMenu.BindingInfo noKeyboardBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Keyboard", "KeyboardMouse", "");

					inputLabel.BindingInfo = noKeyboardBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text keyboardBlank = KeyboardBlanks[i];
					keyboardBlank.text = (keyboardRebindAllowed ? "---" : "N/A");
					keyboardBlank.gameObject.SetActive(true);

					ButtonControl keyboardBlankButton = KeyboardBlankButtons[i];
					keyboardBlankButton.Selectable.interactable = keyboardRebindAllowed;
				}
			}

			if ((mouseBindings != null) && (mouseBindings.Count > 0))
			{
				for (int i = 0; i < mouseBindings.Count; i++)
				{
					TMP_Text mouseBlank = MouseBlanks[i];
					mouseBlank.gameObject.SetActive(false);

					var mouseBinding = mouseBindings[i];

					RebindInputButtonLabel inputLabel = MouseIcons[i];

					inputLabel.BindingInfo = mouseBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < MouseIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = MouseIcons[i];

					SettingsMenu.BindingInfo noMouseBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Mouse", "KeyboardMouse", "");

					inputLabel.BindingInfo = noMouseBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text mouseBlank = MouseBlanks[i];
					mouseBlank.text = (mouseRebindAllowed ? "---" : "N/A");
					mouseBlank.gameObject.SetActive(true);

					ButtonControl mouseBlankButton = MouseBlankButtons[i];
					mouseBlankButton.Selectable.interactable = mouseRebindAllowed;
				}
			}

			if ((gamepadBindings != null) && (gamepadBindings.Count > 0))
			{
				for (int i = 0; i < gamepadBindings.Count; i++)
				{
					TMP_Text gamepadBlank = GamepadBlanks[i];
					gamepadBlank.gameObject.SetActive(false);

					var gamepadBinding = gamepadBindings[i];

					RebindInputButtonLabel inputLabel = GamepadIcons[i];

					inputLabel.BindingInfo = gamepadBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < GamepadIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = GamepadIcons[i];

					SettingsMenu.BindingInfo noGamepadBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Gamepad", "Gamepad", "");

					inputLabel.BindingInfo = noGamepadBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text gamepadBlank = GamepadBlanks[i];
					gamepadBlank.text = (gamepadRebindAllowed ? "---" : "N/A");
					gamepadBlank.gameObject.SetActive(true);

					ButtonControl gamepadBlankButton = GamepadBlankButtons[i];
					gamepadBlankButton.Selectable.interactable = gamepadRebindAllowed;
				}
			}
		}

		public override void Refresh(List<SettingsMenu.BindingInfo> keyboardBindings, List<SettingsMenu.BindingInfo> mouseBindings, List<SettingsMenu.BindingInfo> gamepadBindings)
		{
			if ((keyboardBindings != null) && (keyboardBindings.Count > 0))
			{
				for (int i = 0; i < keyboardBindings.Count; i++)
				{
					TMP_Text keyboardBlank = KeyboardBlanks[i];
					keyboardBlank.gameObject.SetActive(false);

					var keyboardBinding = keyboardBindings[i];

					RebindInputButtonLabel inputLabel = KeyboardIcons[i];

					inputLabel.BindingInfo = keyboardBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < KeyboardIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = KeyboardIcons[i];

					SettingsMenu.BindingInfo noKeyboardBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Keyboard", "KeyboardMouse", "");

					inputLabel.BindingInfo = noKeyboardBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text keyboardBlank = KeyboardBlanks[i];
					keyboardBlank.text = (keyboardRebindAllowed ? "---" : "N/A");
					keyboardBlank.gameObject.SetActive(true);

					ButtonControl keyboardBlankButton = KeyboardBlankButtons[i];
					keyboardBlankButton.Selectable.interactable = keyboardRebindAllowed;
				}
			}

			if ((mouseBindings != null) && (mouseBindings.Count > 0))
			{
				for (int i = 0; i < mouseBindings.Count; i++)
				{
					TMP_Text mouseBlank = MouseBlanks[i];
					mouseBlank.gameObject.SetActive(false);

					var mouseBinding = mouseBindings[i];

					RebindInputButtonLabel inputLabel = MouseIcons[i];

					inputLabel.BindingInfo = mouseBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < MouseIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = MouseIcons[i];

					SettingsMenu.BindingInfo noMouseBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Mouse", "KeyboardMouse", "");

					inputLabel.BindingInfo = noMouseBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text mouseBlank = MouseBlanks[i];
					mouseBlank.text = (mouseRebindAllowed ? "---" : "N/A");
					mouseBlank.gameObject.SetActive(true);

					ButtonControl mouseBlankButton = MouseBlankButtons[i];
					mouseBlankButton.Selectable.interactable = mouseRebindAllowed;
				}
			}

			if ((gamepadBindings != null) && (gamepadBindings.Count > 0))
			{
				for (int i = 0; i < gamepadBindings.Count; i++)
				{
					TMP_Text gamepadBlank = GamepadBlanks[i];
					gamepadBlank.gameObject.SetActive(false);

					var gamepadBinding = gamepadBindings[i];

					RebindInputButtonLabel inputLabel = GamepadIcons[i];

					inputLabel.BindingInfo = gamepadBinding;

					//KeyboardIcon.Handler = keyboardBinding.Handler;

					inputLabel.gameObject.SetActive(true);
				}
			}
			else
			{
				for (int i = 0; i < GamepadIcons.Count; i++)
				{
					RebindInputButtonLabel inputLabel = GamepadIcons[i];

					SettingsMenu.BindingInfo noGamepadBinding = new SettingsMenu.BindingInfo(Category, action, -1, Name, "Gamepad", "Gamepad", "");

					inputLabel.BindingInfo = noGamepadBinding;
					inputLabel.gameObject.SetActive(false);

					TMP_Text gamepadBlank = GamepadBlanks[i];
					gamepadBlank.text = (gamepadRebindAllowed ? "---" : "N/A");
					gamepadBlank.gameObject.SetActive(true);

					ButtonControl gamepadBlankButton = GamepadBlankButtons[i];
					gamepadBlankButton.Selectable.interactable = gamepadRebindAllowed;
				}
			}
		}

		public override List<ButtonControl> GetSelectables()
		{
			selectables.Clear();

			int i;

			//if (keyboardRebindAllowed)
			//{
				for (i = 0; i < KeyboardIcons.Count; i++)
				{
					selectables.Add(KeyboardIcons[i].gameObject.activeSelf ? KeyboardIcons[i].Selectable : KeyboardBlankButtons[i]);
				}
			//}

			//if (mouseRebindAllowed)
			//{
				for (i = 0; i < MouseIcons.Count; i++)
				{
					selectables.Add(MouseIcons[i].gameObject.activeSelf ? MouseIcons[i].Selectable : MouseBlankButtons[i]);
				}
			//}

			//if (gamepadRebindAllowed)
			//{
				for (i = 0; i < GamepadIcons.Count; i++)
				{
					selectables.Add(GamepadIcons[i].gameObject.activeSelf ? GamepadIcons[i].Selectable : GamepadBlankButtons[i]);
				}
			//}

			return selectables;
		}

		//public void Set(int bindingIndex, string group, string label, InputAction action)
		//{
		//	Handler = action;
		//	Group = group;
		//	Label.text = label;
		//	BindingIndex = bindingIndex;
		//	//ButtonText.text = action.GetBindingDisplayString(bindingIndex);
		//}

		//public void StartRebind()
		//{

		//}
	}
}

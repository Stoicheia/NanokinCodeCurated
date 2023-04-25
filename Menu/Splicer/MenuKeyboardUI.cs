using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Util;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;


namespace Anjin.UI
{
	[RequireComponent(typeof(MenuKeyboardController))]
	public class MenuKeyboardUI : SerializedMonoBehaviour
	{
		private readonly Array _keyCodes = Enum.GetValues(typeof(KeyCode));
		private MenuKeyboardController _controller;
		private TMP_InputField _inputField;

		[SerializeField] private List<KeyboardCharacterUI> _keyImages;
		[SerializeField] private RectTransform _cursor;
		[SerializeField] private Vector3 _cursorOffset;
		[SerializeField] private KeyboardCharacterUI _defaultSelectedKey;
		[SerializeField] private RectTransform _keyboardGraphics;
		[SerializeField] private TextMeshProUGUI _reflection;

		[SerializeField] private List<RectTransform> _dependants;

		[Space]
		[SerializeField] private int _characterLimit = 16;
		private KeyboardCharacterUI _prevSelectedKey;
		private KeyboardCharacterUI _currentSelectedKey;
		private bool _charChanged;
		private bool _arrowPressedThisSession;
		private bool _inputGuarantee;

		private bool _shiftOn;

		public bool ShiftOn
		{
			get => _shiftOn;
			set
			{
				_shiftOn = value;
				foreach (var c in _keyImages)
				{
					c.ToggleShift(value);
				}
			}
		}

		private bool OnscreenMode => _arrowPressedThisSession || GameInputs.ActiveDevice == InputDevices.Gamepad;
		private void Awake()
		{
			_controller = GetComponent<MenuKeyboardController>();
			_currentSelectedKey = _defaultSelectedKey;
			_prevSelectedKey = _defaultSelectedKey;
			_inputField.characterLimit = _characterLimit;
		}

		private void Update()
		{
			ReadInputs();
			_keyboardGraphics.SetActive(OnscreenMode);
			if (_charChanged)
			{
				UpdateCursor();
			}
		}

		private void ReadInputs()
		{
			if (!_arrowPressedThisSession)
			{
				if (GameInputs.IsPressed(Key.W) || GameInputs.IsPressed(Key.A) || GameInputs.IsPressed(Key.S) || GameInputs.IsPressed(Key.D))
				{
					return;
				}
				if (GameInputs.IsDown(Key.W) || GameInputs.IsDown(Key.A) || GameInputs.IsDown(Key.S) || GameInputs.IsDown(Key.D))
				{
					return;
				}
			}

			if (GameInputs.menuNavigate.right.IsPressedOrHeld())
			{
				if(_inputField.caretPosition == _inputField.text.Length)
					_arrowPressedThisSession = true;
				_prevSelectedKey = _currentSelectedKey;
				_currentSelectedKey = _currentSelectedKey.Right ? _currentSelectedKey.Right : _currentSelectedKey;
				_charChanged = true;
			}
			if (GameInputs.menuNavigate.left.IsPressedOrHeld())
			{
				if(_inputField.caretPosition == 0)
					_arrowPressedThisSession = true;
				_prevSelectedKey = _currentSelectedKey;
				_currentSelectedKey = _currentSelectedKey.Left ? _currentSelectedKey.Left : _currentSelectedKey;;
				_charChanged = true;
			}
			if (GameInputs.menuNavigate.down.IsPressedOrHeld())
			{
				_arrowPressedThisSession = true;
				_prevSelectedKey = _currentSelectedKey;
				_currentSelectedKey = _currentSelectedKey.Lower ? _currentSelectedKey.Lower : _currentSelectedKey;
				_charChanged = true;
			}
			if (GameInputs.menuNavigate.up.IsPressedOrHeld())
			{
				_arrowPressedThisSession = true;
				_prevSelectedKey = _currentSelectedKey;
				_currentSelectedKey = _currentSelectedKey.Upper ? _currentSelectedKey.Upper : _currentSelectedKey;
				_charChanged = true;
			}
			if (GameInputs.confirm.IsPressed && OnscreenMode)
			{
				ProcessKeycode(_currentSelectedKey.Keycode);
			}
			if (GameInputs.cancel.IsPressed && OnscreenMode)
			{
				if(_inputField.text == "") ProcessKeycode(KeyCode.Escape);
				else ProcessKeycode(KeyCode.Backspace);
			}
		}

		private void ProcessKeycode(KeyCode key)
		{
			switch (key)
			{
				case KeyCode.Escape:
					Close(false);
					break;
				case KeyCode.Return:
					Close(true);
					break;
				case KeyCode.Backspace:
					_inputGuarantee = true;
					_inputField.text = _inputField.text.Remove(_inputField.text.Length - 1);
					break;
				case KeyCode.LeftShift:
					ShiftOn = !ShiftOn;
					break;
				case KeyCode.Tilde:
					_inputGuarantee = true;
					_inputField.text = _controller.GetDefault();
					break;
				default:
					_inputGuarantee = true;
					string text = _shiftOn ? ((char) key).ToString().ToUpper() : ((char) key).ToString().ToLower();
					_inputField.text += text[0];
					ShiftOn = false;
					break;
			}
		}

		private void UpdateCursor()
		{
			if(_cursor != null)
				_cursor.transform.position = _currentSelectedKey.RectTransform.position + _cursorOffset;
			_prevSelectedKey.ToggleCursor(false);
			_currentSelectedKey.ToggleCursor(true);
		}

		public void Open(IMenuKeyboardInputReceiver receiver, TMP_InputField giver)
		{
			_inputField = giver;
			_inputField.onValueChanged.AddListener(ValueChange);
			_inputField.onEndEdit.AddListener(x => Close(true));
			_inputField.onDeselect.AddListener(x => Close(false));
			_inputField.onValidateInput += delegate(string input, int charIndex, char addedChar) { return ValidateChar(addedChar); };
			gameObject.SetActive(true);
			_keyboardGraphics.SetActive(false);
			_controller.Open(receiver);
			_currentSelectedKey = _defaultSelectedKey;
			_prevSelectedKey = _defaultSelectedKey;
			_arrowPressedThisSession = false;
			foreach (var k in _keyImages)
			{
				k.ToggleCursor(false);
			}

			foreach (var d in _dependants)
			{
				d.SetActive(true);
			}
		}



		public void Close(bool submit = true)
		{
			_inputField.onValueChanged.RemoveAllListeners();
			_inputField.onEndEdit.RemoveAllListeners();
			_inputField.onDeselect.RemoveAllListeners();

			_inputField.DeactivateInputField();
			if (submit)
			{
				_controller.Submit();
			}
			else
			{
				_controller.Cancel();
				_inputField.text = "";
			}

			gameObject.SetActive(false);

			foreach (var d in _dependants)
			{
				d.SetActive(false);
			}
		}


		private void ValueChange(string newValue)
		{
			_controller.SetString(newValue);
			_inputGuarantee = false;
			_reflection.text = newValue;
		}

		private char ValidateChar(char c)
		{
			if (!_inputGuarantee && OnscreenMode) return '\0';
			else return c;
		}
	}

}

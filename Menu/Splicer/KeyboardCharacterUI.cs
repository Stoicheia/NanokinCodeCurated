using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Anjin.UI
{
	[RequireComponent(typeof(RectTransform))]
	public class KeyboardCharacterUI : SerializedMonoBehaviour
	{
		[SerializeField] private KeyCode _keycode;
		[SerializeField] private bool _autoFillShiftKeycode;
		[ShowIf("@!_autoFillShiftKeycode")][SerializeField] private KeyCode _shiftKeycode;
		[SerializeField] private bool _autoFillDisplay;
		[ShowIf("@!_autoFillDisplay")][SerializeField] private string _defaultDisplay;
		[ShowIf("@!_autoFillDisplay")][SerializeField] private string _shiftDisplay;
		[SerializeField] private KeyboardCharacterUI _upperNeighbour;
		[SerializeField] private KeyboardCharacterUI _leftNeighbour;
		[SerializeField] private KeyboardCharacterUI _rightNeighbour;
		[SerializeField] private KeyboardCharacterUI _lowerNeighbour;

		[Space]
		[SerializeField] private Image _cursorImage;

		[SerializeField] private TextMeshProUGUI _myText;

		private RectTransform _rectTransform;
		private KeyCode _activeKeycode;
		private bool _shiftOn;

		public KeyCode Keycode => _activeKeycode;
		public KeyboardCharacterUI Upper => _upperNeighbour;
		public KeyboardCharacterUI Lower => _lowerNeighbour;
		public KeyboardCharacterUI Right => _rightNeighbour;
		public KeyboardCharacterUI Left => _leftNeighbour;
		public RectTransform RectTransform => _rectTransform;


		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();
			if (_cursorImage == null)
			{
				_cursorImage = GetComponentInChildren<Image>();
			}

			if (_cursorImage != null)
			{
				_cursorImage.enabled = false;
			}

			if (_myText == null)
			{
				_myText = GetComponent<TextMeshProUGUI>();
			}

			if (_autoFillDisplay)
			{
				_defaultDisplay = _myText.text.ToLower();
				_shiftDisplay = _myText.text.ToUpper();
			}

			if (_autoFillShiftKeycode)
			{
				_shiftKeycode = _keycode;
			}


			ToggleShift(false);
		}

		public void ToggleCursor(bool b)
		{
			if (_cursorImage != null)
			{
				_cursorImage.enabled = b;
			}
		}

		public void ToggleShift()
		{
			_shiftOn = !_shiftOn;
			_activeKeycode = _shiftOn ? _shiftKeycode : _keycode;
			_myText.text = _shiftOn ? _shiftDisplay : _defaultDisplay;

			if (_activeKeycode == KeyCode.None)
			{
				_activeKeycode = _keycode;
			}
		}

		public void ToggleShift(bool b)
		{
			_shiftOn = b;
			_activeKeycode = _shiftOn ? _shiftKeycode : _keycode;
			_myText.text = _shiftOn ? _shiftDisplay : _defaultDisplay;

			if (_activeKeycode == KeyCode.None)
			{
				_activeKeycode = _keycode;
			}
		}
	}
}

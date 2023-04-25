using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Anjin.UI
{
	public interface IMenuKeyboardInputReceiver
	{
		void ReceiveInput(string s);
		void CloseWithoutInput();
		string GetDefault();
	}

	public class MenuKeyboardController : MonoBehaviour
	{
		private StringBuilder _currentInput;
		private IMenuKeyboardInputReceiver _receiver;
		public string CurrentString => _currentInput.ToString();

		private void Awake()
		{
			_currentInput = new StringBuilder();
		}

		public void Open(IMenuKeyboardInputReceiver receiver)
		{
			_currentInput.Clear();
			_receiver = receiver;
		}

		public void InputCharacter(char c)
		{
			_currentInput.Append(c);
		}

		public void SetString(string s)
		{
			_currentInput = new StringBuilder(s);
		}

		public void Backspace()
		{
			_currentInput.Remove(_currentInput.Length - 1, 1);
		}

		public void Submit()
		{
			string ret = _currentInput.ToString();
			_currentInput.Clear();
			_receiver.ReceiveInput(ret);
		}

		public void Cancel()
		{
			_receiver.CloseWithoutInput();
		}

		public string GetDefault()
		{
			return _receiver.GetDefault();
		}
	}
}

using System;
using TMPro;
using UnityEngine;
using Object = System.Object;

namespace Util {
	public class TMPRichTextParser : MonoBehaviour {

		public TMP_Text Text;
		bool            _changed;

		void Start()
		{
			_changed = false;
			Text     = GetComponent<TMP_Text>();
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextChanged);
		}

		public void TextChanged(Object obj)
		{
			if (obj == Text)
				_changed = true;
		}

		void Update()
		{
			if (_changed) {
				_changed = false;

				var text = Text.text;

				int i = 0, j = 0;
				while (i < text.Length) {
					char c = text[i];

					if (c == '[') {

						j = i;
						while (j < text.Length && text[j] != ']')  j++;

						string tag = text.Substring(i, j - i);

						Debug.Log("Tag " + tag);

					}


					i++;
				}

			}
		}


	}
}
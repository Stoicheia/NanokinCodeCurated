using Anjin.Util;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Systems.Combat.Networking.Components
{
	public class GenericDialog : SerializedMonoBehaviour
	{
		[SerializeField] private GameObject _pfbLabel, _pfbButton, _pfbSpace;
		[SerializeField] private Transform  _fieldStack;

		public void AddLabel(string text, Color? color = null)
		{
			color = color ?? Color.black;

			GameObject goLabel = Instantiate(_pfbLabel, _fieldStack);

			TextMeshProUGUI label = goLabel.GetComponentInChildren<TextMeshProUGUI>();
			label.text  = text;
			label.color = color.Value.To32();
		}

		public void AddSpace()
		{
			Instantiate(_pfbSpace, _fieldStack);
		}

		public void AddButton(string text, UnityAction onClick)
		{
			GameObject goButton = Instantiate(_pfbButton, _fieldStack);

			TextMeshProUGUI label = goButton.GetComponentInChildren<TextMeshProUGUI>();
			label.text = text;

			Button button = goButton.GetComponentInChildren<Button>();
			button.onClick.AddListener(onClick);
		}

		public void AddOKButton()
		{
			AddButton("OK", Close);
		}

		public void Close()
		{
			Destroy(gameObject);
		}
	}
}
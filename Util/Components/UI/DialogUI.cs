using Systems.Combat.Networking.Components;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Systems.Combat.Networking
{
	public class DialogUI : SerializedMonoBehaviour
	{
		[SerializeField] private GameObject _pfbDialog;
		[SerializeField] private Canvas     _canvas;

		public GenericDialog Empty()
		{
			return Instantiate(_pfbDialog, _canvas.transform).GetComponent<GenericDialog>();
		}

		public GenericDialog Text(string text)
		{
			GenericDialog dialog = Empty();
			dialog.AddLabel(text);
			dialog.AddSpace();
			dialog.AddOKButton();
			return dialog;
		}

		public GenericDialog ErrorText(string text)
		{
			GenericDialog dialog = Empty();
			dialog.AddLabel("Error!", Color.red);
			dialog.AddLabel(text);
			dialog.AddSpace();
			dialog.AddOKButton();
			return dialog;
		}
	}
}
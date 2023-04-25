using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;
using UnityEngine.UI;

public enum ListSetupMode { Auto, Horizontal, Vertical };

public class UGUISetupListInputs : MonoBehaviour
{

	static List<Selectable> _selectables;

	public ListSetupMode mode = ListSetupMode.Auto;

	void Start() {
		Recalculate();
    }

	public void Recalculate()
	{
		if (_selectables == null)
			_selectables = new List<Selectable>();

		if(mode == ListSetupMode.Auto) {
			if (GetComponent<VerticalLayoutGroup>())
				mode = ListSetupMode.Vertical;
			else if (GetComponent<HorizontalLayoutGroup>())
				mode = ListSetupMode.Horizontal;
		}

		_selectables.Clear();
		GetComponentsInChildren(true, _selectables);
		for (int i = 0; i < _selectables.Count; i++) {
			if (!_selectables[i].interactable) {
				_selectables[i].navigation = new Navigation{mode = Navigation.Mode.None};
				_selectables.RemoveAt(i);
			}
		}

		for (int i = 0; i < _selectables.Count; i++) {


			_selectables[i].navigation = new Navigation
			{
				mode          = Navigation.Mode.Explicit,
				selectOnUp    = ( mode == ListSetupMode.Vertical ) 	? _selectables.WrapGet(i - 1) : null,
				selectOnDown  = ( mode == ListSetupMode.Vertical ) 	? _selectables.WrapGet(i + 1) : null,
				selectOnLeft  = ( mode == ListSetupMode.Horizontal ) ? _selectables.WrapGet(i - 1) : null,
				selectOnRight = ( mode == ListSetupMode.Horizontal ) ? _selectables.WrapGet(i + 1) : null,
			};

		}
	}
}


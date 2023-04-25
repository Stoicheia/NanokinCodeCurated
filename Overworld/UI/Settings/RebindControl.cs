using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworld.UI.Settings
{
	public abstract class RebindControl : MonoBehaviour
	{
		public bool AllowDuplicateBindings { get; protected set; }

		public string Category { get; protected set; }
		public string Name { get; protected set; }

		public RectTransform RT;

		public TMP_Text Label;

		protected bool keyboardRebindAllowed;
		protected bool mouseRebindAllowed;
		protected bool gamepadRebindAllowed;

		protected InputAction action;

		public abstract void Set(InputAction action, string category, string name, bool allowDuplicates, bool keyboardRebindAllowed, bool mouseRebindAllowed, bool gamepadRebindAllowed, List<SettingsMenu.BindingInfo> keyboardBindings, List<SettingsMenu.BindingInfo> mouseBindings, List<SettingsMenu.BindingInfo> gamepadBindings);

		public abstract void Refresh(List<SettingsMenu.BindingInfo> keyboardBindings, List<SettingsMenu.BindingInfo> mouseBindings, List<SettingsMenu.BindingInfo> gamepadBindings);

		public abstract List<ButtonControl> GetSelectables();

		protected List<ButtonControl> selectables = new List<ButtonControl>();

		//public virtual void Set(int bindingIndex, string group, string label, InputAction action)
		//{
		//	Handler          = action;
		//	Group           = group;
		//	Label.text      = label;
		//	BindingIndex    = bindingIndex;
		//	//ButtonText.text = action.GetBindingDisplayString(bindingIndex);
		//}

		//public void StartRebind()
		//{

		//}

		//public override Selectable Selectable => Button;
	}
}
using System.Reflection;

using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Util.Odin.Selectors
{
	public static class OdinSelectorExtensions
	{
#if UNITY_EDITOR
		public static void ShowContextMenuSafe<T>(this OdinSelector<T> selector)
		{
			if (Event.current != null)
			{
				selector.ShowInPopup();
				return;
			}

			FieldInfo field = typeof(Event).GetField("s_Current", BindingFlags.Static | BindingFlags.NonPublic);
			if (field != null)
			{
				if (field.GetValue(null) is Event current)
				{
					Vector2 mousePosition = current.mousePosition;
					Rect    btnRect       = new Rect(mousePosition.x, mousePosition.y, 1f, 1f);
					OdinEditorWindow.InspectObjectInDropDown(selector, btnRect, 200);
				}
			}
		}
#endif
	}
}
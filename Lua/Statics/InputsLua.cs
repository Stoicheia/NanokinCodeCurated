using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace Anjin.Scripting
{
	[LuaUserdata(StaticName = "Input")]
	public class InputsLua
	{
		public static bool get_key(KeyCode      key) => Input.GetKey(key);
		public static bool get_key_down(KeyCode key) => Input.GetKeyDown(key);
		public static bool get_key_up(KeyCode   key) => Input.GetKeyUp(key);

		public static bool get_mouse_button(int      button) => Input.GetMouseButton(button);
		public static bool get_mouse_button_down(int button) => Input.GetMouseButtonDown(button);
		public static bool get_mouse_button_up(int   button) => Input.GetMouseButtonUp(button);

		//public static bool any_key_down => Input.anyKeyDown;
		public static bool any_key_down() => Input.anyKeyDown;
		public static bool any_key        => Input.anyKey;

		public static string input_string => Input.inputString;

		public static Vector2 get_mouse_position() => Input.mousePosition.xy();
		public static Vector2 scroll_delta         => Input.mouseScrollDelta;

		/*KeyCode DynValueTokey(DynValue)
		{
			KeyCode.
		}*/

		//TODO: This is obviously temporary. We will need a way to get the binding names of all inputs easily
		public static string get_interact_binding_name()
		{
			return "Z";

			//return NanokinInputs.Interact.Handler.GetBindingDisplayString(group:"Keyboard");
		}

		public static void RegisterLuaUserdata()
		{
		}
	}
}
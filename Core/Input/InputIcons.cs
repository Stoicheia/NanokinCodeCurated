using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Anjin.Nanokin
{
	public class InputIcons : SerializedScriptableObject
	{
		// Gamepad icons
		public Dictionary<GamepadType, Dictionary<GamepadInput, Sprite>> gamepadIcons;

		// Keyboard/mouse icons
		public Dictionary<DesktopInput, Sprite> desktopIcons;
	}
}

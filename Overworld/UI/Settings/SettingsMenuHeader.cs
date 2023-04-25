using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Overworld.UI.Settings {
	public class SettingsMenuHeader : SettingsMenuControl<string> {

		public override Selectable Selectable => null;
		public override void       Set(string val) { }
	}
}
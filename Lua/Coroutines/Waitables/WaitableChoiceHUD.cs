using Anjin.UI;

namespace Anjin.Scripting.Waitables {
	[LuaUserdata]
	public class WaitableChoiceHUD : ICoroutineWaitable {

		public ChoiceTextbox textbox;

		public WaitableChoiceHUD(ChoiceTextbox textbox) {
			this.textbox = textbox;
		}

		public bool CanContinue(bool justYielded, bool isCatchup) => textbox == null || !textbox.IsActive;
	}
}
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableSpritePopup : CoroutineManaged
	{
		private readonly SpritePopup _popup;

		public WaitableSpritePopup(SpritePopup popup)
		{
			_popup = popup;
		}

		public override bool Active => _popup.state != SpritePopup.State.Off;

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			_popup.Hide(true);
		}
	}
}
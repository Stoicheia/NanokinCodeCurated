using Anjin.UI;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableChoiceBubble : ICoroutineWaitable
	{
		private readonly ChoiceBubble _bubble;

		public WaitableChoiceBubble(ChoiceBubble bubble)
		{
			_bubble = bubble;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return _bubble.state == HUDBubble.State.Off;
		}
	}
}
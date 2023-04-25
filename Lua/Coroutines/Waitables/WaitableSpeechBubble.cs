using Anjin.UI;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableSpeechBubble : ICoroutineWaitable
	{
		private readonly SpeechBubble _bubble;

		private bool _isDone;

		public WaitableSpeechBubble(SpeechBubble bubble)
		{
			_bubble = bubble;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return _bubble.state != HUDBubble.State.On;
		}
	}
}
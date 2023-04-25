using Anjin.Nanokin.Map;

namespace Anjin.Scripting.Waitables {

	[LuaUserdata]
	public class WaitableTrigger : ICoroutineWaitable {

		private Trigger _trigger;
		private bool    _waitForInside;

		public WaitableTrigger(Trigger trigger, bool waitForInside = true)
		{
			_trigger       = trigger;
			_waitForInside = waitForInside;
		}

		public bool CanContinue(bool justYielded, bool isCatchup = false)
		{
			if (_trigger != null) {

				if (_waitForInside)
					return _trigger.IsPlayerInside;

				return !_trigger.IsPlayerInside;
			}


			return false;
		}
	}
}
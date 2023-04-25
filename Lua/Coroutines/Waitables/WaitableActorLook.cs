using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableActorLook : ICoroutineWaitable
	{
		private DirectedActor _actor;

		public WaitableActorLook(DirectedActor actor)
		{
			_actor                 = actor;
			_actor.isAdjustingLook = true;
		}

		public bool CanContinue(bool justYielded, bool isCatchup)
		{
			return !_actor.isAdjustingLook;
		}
	}
}
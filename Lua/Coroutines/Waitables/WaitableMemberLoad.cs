using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableMemberLoad : ICoroutineWaitable
	{
		public readonly DirectedBase actor;

		public WaitableMemberLoad(DirectedBase actor)
		{
			this.actor = actor;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return actor.loaded;
		}
	}
}
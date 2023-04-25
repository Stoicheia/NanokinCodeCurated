using Anjin.Actors;
using Combat.Data;

namespace Combat
{
	public class BattleActor : ActorBase
	{
		public virtual void OnStateApplied(State state) { }
		public virtual void OnStateRemoved(State state) { }
	}
}
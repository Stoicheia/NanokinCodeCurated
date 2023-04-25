using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedActorMove : CoroutineManaged
	{
		private readonly DirectedActor _actor;

		public ManagedActorMove(DirectedActor actor)
		{
			_actor = actor;
		}

		public override bool Active    => _actor.directions.moveMode != DirectedActor.MoveMode.None;
		public override bool Skippable => _actor.directions.moveMode != DirectedActor.MoveMode.Direction;

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			_actor.CompleteMove();
		}
	}
}
namespace Anjin.Actors
{
	/// <summary>
	/// A brain which assuredly takes control of the whole actor and
	/// does nothing. Can be useful for testing since Actor Brains cannot
	/// be enabled/disabled without removing them from the actor.
	/// </summary>
	public class DisableBrain : ActorBrain
	{
		public override int Priority => 100;

		public override void OnBeginControl()
		{
			disableControls = true;
		}

		public override void OnTick(float dt) { }

		public override void OnEndControl() { }
	}
}
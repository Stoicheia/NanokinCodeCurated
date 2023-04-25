using Anjin.Actors;

namespace Overworld.Navigation {
	public class NPCNavTester : ActorBrain {

		public override int  Priority         { get; }
		public override void OnBeginControl() { }
		public override void OnTick(float dt) { }
		public override void OnEndControl()   { }
	}
}
namespace Anjin.Actors.Test_Brains
{
	public class CharJumpTestBrain : ActorBrain, ICharacterActorBrain

	{
		public override int Priority => 1;


		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs.jumpPressed = true;
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }

		public override void OnBeginControl()
		{
			//Debug.Log("CharJumpTestBrain OnBeginControl");
		}

		public override void OnTick(float dt) {}

		public override void OnEndControl()
		{
			//Debug.Log("CharJumpTestBrain OnEndControl");
		}
	}
}
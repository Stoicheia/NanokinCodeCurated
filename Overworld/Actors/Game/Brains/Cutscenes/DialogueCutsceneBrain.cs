namespace Anjin.Actors
{
	/// <summary>
	/// DialogueCutsceneBrain:
	/// - Between multiple actors who display speech bubbles in sequence.
	/// - Driven by the event system.
	/// - Character Actors can be told to look towards WorldPoints.
	/// - No actor movement.
	/// </summary>
	public class DialogueCutsceneBrain : CutsceneBrain, ICharacterActorBrain
	{
		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			/*if (CutsceneActors.Contains(character) && Directions.ContainsKey(character.Reference))
			{
				ActorDirections dir = Directions[character.Reference];

				if (dir.LookTowardsWorldPoint)
				{
					Vector3? point = dir.LookingPoint.Get();
					if(point != null)
					{
						inputs.look = point.Value.xz() - character.transform.position.xz();
					}
				}
			}*/
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{

		}


		public override int  Priority         { get; }
		public override void OnTick(float dt) { }
	}
}
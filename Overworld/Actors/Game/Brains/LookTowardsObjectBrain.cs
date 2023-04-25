using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Actors
{
	public class LookTowardsObjectBrain : ActorBrain, ICharacterActorBrain
	{
		public override int Priority => 0;

		public override void OnBeginControl() { }
		public override void OnEndControl()   { }
		public override void OnTick(float dt) { }

		[NonSerialized]
		public Dictionary<Actor, Transform> lookPairs;

		private void Start()
		{
			lookPairs = new Dictionary<Actor, Transform>();
		}

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if(lookPairs.ContainsKey(character))
			{
				inputs.look = lookPairs[character].position.xz() - character.transform.position.xz();
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }
	}
}
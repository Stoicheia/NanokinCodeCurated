using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	public class MoveTowardsBrain : ActorBrain, ICharacterActorBrain
	{
		public override int Priority => 0;

		/// <summary>
		/// Maximum distance before the destination is considered reached.
		/// </summary>
		[ShowInInspector] public float ReachThreshold { get; set; } = .6f;

		/// <summary>
		/// Position that the actor should walk towards.
		/// </summary>
		[ShowInInspector] public Vector3 Destination { get; set; }

		/// <summary>
		/// The brain to use afterwards once the destiniation has been reached.
		/// </summary>
		public ActorBrain ExitBrain { get; set; }

		public override void OnBeginControl()
		{ }

		public override void OnTick(float dt)
		{ }

		public override void OnEndControl()
		{ }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			Vector3 now = character.transform.position;
			Vector3 end = Destination;

			float distance = Vector3.Distance(now, end);

			bool hasReached = distance <= ReachThreshold;
			if (!hasReached)
			{
				inputs.move = now.Towards(end).xz();
			}
			else
			{
				character.AddLocalBrain(ExitBrain);
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{ }
	}
}
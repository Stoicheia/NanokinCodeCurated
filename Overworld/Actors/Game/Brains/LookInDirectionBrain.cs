using Anjin.Util;
using UnityEngine;
using Util;

namespace Anjin.Actors
{
	public class LookInDirectionBrain : ActorBrain, ICharacterActorBrain
	{
		[SerializeField, Range(0, 360)] private float _angle;

		/// <summary>
		/// Direction to look towards.
		/// </summary>
		public Vector3 Direction { get; set; }

		public override int Priority => 0;

		private void Awake()
		{
			// Set the initial look direction from a serialized angle field. (360 degrees)
			Direction = Vector3.forward.Rotate(y: _angle);
		}

		public override void OnBeginControl()
		{ }

		public override void OnTick(float dt)
		{ }

		public override void OnEndControl()
		{ }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs.look     = Direction.xz();
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{ }

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
			{
				Draw2.DrawLineTowards(transform.position, Vector3.forward.Rotate(y: _angle), ColorsXNA.Black);
			}
		}
	}
}
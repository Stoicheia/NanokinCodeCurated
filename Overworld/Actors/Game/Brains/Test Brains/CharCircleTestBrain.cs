using UnityEngine;
using Util;

namespace Anjin.Actors.Test_Brains
{
	public class CharCircleTestBrain : ActorBrain, ICharacterActorBrain
	{
		public override int Priority => 1;

		public float turn = 0;
		public float turnSpeed = 10;

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs.move = MathUtil.AnglePosition(turn, 1f).x_y();
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }

		private void Update()
		{
			turn += turnSpeed * Time.deltaTime;
		}

		public override void OnBeginControl()
		{
			turn = Random.Range(0, 360);
			Debug.Log("CharCircleTestBrain OnBeginControl");
		}

		public override void OnTick(float dt) {}

		public override void OnEndControl()
		{
			Debug.Log("CharCircleTestBrain OnEndControl");
		}
	}
}
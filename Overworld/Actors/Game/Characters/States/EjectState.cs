using Anjin.Nanokin.Map;
using UnityEngine;
using Util;

namespace Anjin.Actors.States
{
	public class EjectState : StateKCC
	{
		private bool _start;
		private Vector3 _initialVelocity;

		public float   Height;
		public Vector3 Start;
		public Vector3 End;

		public float Speed;
		public float Time;

		public bool IsDone;

		protected override Vector3 TurnDirection => (End - Start).normalized;

		public Vector3 ExitVelocity;

		public override void OnActivate()
		{
			_start = false;

			IsDone = false;

			Time = 0;

			Height = 3;
			Start  = Motor.transform.position;

			Speed = 10;

			actor.ClearVelocity();
			actor.Motor.SetGroundSolvingActivation(false);
		}

		public override void OnDeactivate()
		{
			actor.Motor.SetGroundSolvingActivation(true);
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			float speed = Speed;

			Time += (speed * deltaTime) / MathUtil.ParabolaLength(Height, (Start - End).magnitude);

			if (Time >= 0.95f) IsDone = true;
			if (Time >= 1)	   return;

			//End = LaunchPad.Target.position;

			Vector3 target = MathUtil.EvaluateParabola(Start, End, Height, Time);

			ExitVelocity = MathUtil.ParabolaDirection(Start, End, Height, 1) * 10;

			var vec = target - Motor.transform.position;

			/*if (vec.magnitude > Mathf.Epsilon)
				ExitDirection = vec.normalized;*/

			Motor.SetPosition(target);
		}

		public EjectState ToPosition(Vector3 position)
		{
			End = position;
			return this;
		}
	}
}
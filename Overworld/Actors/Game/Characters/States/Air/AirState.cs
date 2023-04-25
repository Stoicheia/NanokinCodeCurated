using UnityEngine;

namespace Anjin.Actors
{
	public abstract class
		AirState : StateKCC
	{
		public override bool IsAir    => true;
		public override bool IsGround => false;

		public virtual float SpeedDamping => actor.DefaultSpeedSharpness;

		/*public Vector3 StartingPosition;
		public Vector3 HighestPosition;
		public float   FallHeight = 0;*/

		public override void OnActivate()
		{
			base.OnActivate();

			/*FallHeight       = 0;
			StartingPosition = actor.transform.position;
			HighestPosition  = StartingPosition;*/
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			/*if (actor.transform.position.y > HighestPosition.y) {
				HighestPosition = actor.transform.position;
			}

			FallHeight = Mathf.Abs(actor.transform.position.y - HighestPosition.y);*/

			UpdateAir(ref currentVelocity, deltaTime, SpeedDamping);
		}
	}
}
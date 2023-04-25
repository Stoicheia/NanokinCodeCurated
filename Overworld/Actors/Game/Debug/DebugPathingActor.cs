using Anjin.MP;
using UnityEngine;

namespace Anjin.Actors
{
	[RequireComponent(typeof(MotionPlanner))]
	public class DebugPathingActor : Actor, IPathFollower
	{
		public Vector3 	Direction = Vector3.zero;
		public float 	Speed = 0;

		//public MotionPlanner planner;

		public void MP_DoMovement()
		{
			if (Direction.magnitude > Mathf.Epsilon)
			{
				transform.rotation =  Quaternion.LookRotation(Direction, Vector3.up);
				transform.position += Direction.normalized * Speed * Time.deltaTime;
			}
		}

		public void MP_SetDirection(Vector3 dir) => Direction = dir;
		public void MP_SetSpeed(float speed)	 => Speed = speed;
		public void MP_SetPosition(Vector3 pos)	 => transform.position = pos;

		public bool AbleToPath() => true;

		public void MP_OnPathDone() {}
		public void MP_OnPathStart() {}
	}
}
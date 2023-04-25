using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.MP
{
	public class MotionPlanner : SerializedMonoBehaviour
	{
		public IPathFollower Follower;

		public PathingSettings settings;
		[HideInEditorMode]
		public PathingState state;

		public bool AutoDiscover = true;

		void Awake()
		{
			state = PathingState.Default;
			//WillPass = false;

			if (AutoDiscover)
				Follower = GetComponent<IPathFollower>();
		}

		//public bool WillPass = false;

		void Update()
		{
			MotionPlanning.Pathing_UpdateState(ref state, transform.position);

			if(state.state == MPState.Running) {
				Follower.MP_SetSpeed(state.follower_speed);
				Follower.MP_SetDirection(state.follower_dir);
				Follower.MP_DoMovement();
			}
		}

		public bool WillPassTarget(Vector3 target, Vector3 prevPoint, float spd)
		{
			var nextPoint = transform.position + ((target - transform.position).normalized * spd * Time.deltaTime);
			return Vector3.Distance(prevPoint, nextPoint) + 0.1f >= Vector3.Distance(prevPoint, target);
		}

		[Button]
		public void StartPathing()
		{
			state.Start(settings);
		}

		[Button]
		public void StopPathing()
		{
			if (state.state == MPState.Running)
				state.state = MPState.Idle;
		}

		void OnDrawGizmos()
		{
			if(state.Path != null)
				MotionPlanning.DrawPathInEditor(state.Path);
		}
	}
}
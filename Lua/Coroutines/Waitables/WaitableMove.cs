using UnityEngine;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableMove : ICoroutineWaitable
	{
		private readonly Transform _transform;
		private readonly Vector3   _goal;

		public WaitableMove(Transform transform, Vector3 goal)
		{
			_transform = transform;
			_goal      = goal;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return Vector3.Distance(_transform.gameObject.transform.position, _goal) < 0.1f;
		}
	}
}
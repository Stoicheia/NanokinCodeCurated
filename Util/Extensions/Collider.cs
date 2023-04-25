using UnityEngine;

namespace Util.Extensions
{
	public partial class Extensions
	{
		private static RaycastHit[] _colliders = new RaycastHit[16];

		public static bool ContainsPoint(this Collider test, Vector3 point)
		{
			return Vector3.SqrMagnitude(test.ClosestPoint(point) - point) > Mathf.Epsilon + Mathf.Epsilon;

			// NOTE this is another solution to this problem

			// // Use collider bounds to get the center of the collider. May be inaccurate
			// // for some colliders (i.e. MeshCollider with a 'plane' mesh)
			// Vector3 center = test.bounds.center;
			//
			// // Cast a ray from point to center
			// Vector3 direction = center - point;
			// return Physics.RaycastNonAlloc(point, direction, _colliders, Vector3.Distance(center, point)) % 2 == 1;
		}
	}
}
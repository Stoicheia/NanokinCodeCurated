using Anjin.Scripting;
using UnityEngine;

namespace Anjin.Actors
{
	[LuaUserdata]
	public readonly struct SwordHit : IHitInfo
	{
		public readonly Vector3 direction;
		public readonly float   force;

		public SwordHit(Vector3 direction, float force)
		{
			this.direction = direction;
			this.force     = force;
		}
	}
}
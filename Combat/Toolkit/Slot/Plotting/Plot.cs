using UnityEngine;

namespace Combat.Data
{
	public readonly struct Plot
	{
		public readonly Vector3 position;
		public readonly Vector3 facing;

		public Plot(Vector3 position, Vector3 facing)
		{
			this.position = position;
			this.facing   = facing;
		}
	}
}
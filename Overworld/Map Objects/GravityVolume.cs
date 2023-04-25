using Anjin.Actors;
using Overworld.Terrains;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class GravityVolume : Volume
	{
		[Min(0)]
		public float GravityScale = 1f;

		protected override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);

			if (other.TryGetComponent(out ActorKCC kcc))
			{
				// NOTE:
				// We could use a list to handle multiple volumes intersecting.
				// Also this could lead to bugs in some specific circumstances
				kcc.gravityVolume = this;
			}
		}

		protected override void OnTriggerExit(Collider other)
		{
			base.OnTriggerExit(other);

			if (other.TryGetComponent(out ActorKCC kcc))
			{
				kcc.gravityVolume = null;
			}
		}
	}
}
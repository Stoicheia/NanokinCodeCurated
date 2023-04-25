using Anjin.Actors;
using Anjin.Util;
using UnityEngine;
using Util;

namespace Overworld.Terrains
{
	public readonly struct BounceInfo
	{
		public readonly float   control;
		public readonly Vector3 direction;
		public readonly float   height;
		public readonly float   energyConservation;
		public readonly float   energyConservationMax;

		public BounceInfo(float control,
			Vector3             direction,
			float               height,
			float               energyConservation,
			float               energyConservationMax
		)
		{
			this.control               = control;
			this.direction             = direction;
			this.height                = height;
			this.energyConservation    = energyConservation;
			this.energyConservationMax = energyConservationMax;
		}

		public Vector3 AffectVelocity(Vector3 velocity, ActorKCC kcc)
		{
			float energy = (velocity.magnitude * energyConservation).Maximum(energyConservationMax);
			float force  = kcc.CalculateJumpForce(height);

			return direction * (force + energy);
		}


		public interface IHandler
		{
			bool CanBounce { get; }
			void OnBounce(BounceInfo info);
		}
	}
}
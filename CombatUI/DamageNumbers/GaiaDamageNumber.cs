using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.UI
{
	public class GaiaDamageNumber : DamageNumber
	{
		[FormerlySerializedAs("burstParticles")]
		public ParticleSystem BurstParticles;

		public void OnBurst()
		{
			ParticleSystem.ShapeModule shape = BurstParticles.shape;
			shape.scale = ParticleScale();
			BurstParticles.Play();
		}

		private Vector3 ParticleScale()
		{
			float scaleX;
			if (damageValue < 10)
				scaleX = 1;
			else if (damageValue < 100)
				scaleX = 1.33f;
			else if (damageValue < 1000)
				scaleX = 1.66f;
			else
				scaleX = 2f;

			return new Vector3(scaleX, 1f, 1f);
		}
	}
}
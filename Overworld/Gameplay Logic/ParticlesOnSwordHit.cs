using Anjin.Actors;
using Assets.Scripts.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	public class ParticlesOnSwordHit : SerializedMonoBehaviour, IHitHandler<SwordHit>
	{
		[SerializeField] private ParticlePrefab FX_SwordHitPrefab;

		public void OnHit(SwordHit hit)
		{
			FX_SwordHitPrefab.Instantiate(transform, transform.position);
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
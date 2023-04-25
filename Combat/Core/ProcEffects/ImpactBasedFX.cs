using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Skills.Generic
{
	public class ImpactBasedFX : MonoBehaviour
	{
		[SerializeField] private Transform ImpactPoint;

		[SerializeField] private List<ParticleSystem> particleSystems;

		public void UpdateImpactLocation([NotNull] Transform target)
		{
			//Transform parent = ImpactPoint.parent;

			ImpactPoint.SetParent(null);
			ImpactPoint.position = target.position;
			//ImpactPoint.SetParent(parent);

			for (int i = 0; i < particleSystems.Count; i++)
			{
				particleSystems[i].collision.SetPlane(0, ImpactPoint);
			}
		}
	}
}
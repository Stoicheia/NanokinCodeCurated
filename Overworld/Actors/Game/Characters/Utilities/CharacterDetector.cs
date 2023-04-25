using UnityEngine;
using Util;

namespace Anjin.Actors
{
	public class CharacterDetector : MonoBehaviour
	{
		public Bounds    bounds;
		public LayerMask mask;

		public bool Detected;

		private Collider[] overlappingColliders;

		private void Start()
		{
			overlappingColliders = new Collider[8];
		}

		private void Update()
		{
			Detected = false;

			int num = Physics.OverlapBoxNonAlloc(transform.position, bounds.extents, overlappingColliders, transform.rotation, mask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				if (overlappingColliders[i].transform != transform)
				{
					Detected = true;
					break;
				}
			}
		}

		private void OnDrawGizmos()
		{
			Draw2.DrawBounds(transform.position, bounds, Color.red);
		}
	}
}
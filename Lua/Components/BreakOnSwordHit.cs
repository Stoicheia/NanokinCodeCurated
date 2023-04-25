using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Scripting
{
	[AddComponentMenu("Anjin: Events/Break on Sword Hit")]
	[RequireComponent(typeof(Breakable))]
	public class BreakOnSwordHit : MonoBehaviour, IHitHandler<SwordHit>
	{
		public void OnHit(SwordHit hit)
		{
			Break();
		}

		public bool IsHittable(SwordHit hit) => true;

		public void Break()
		{
			GetComponent<Breakable>().Break();
		}
	}
}
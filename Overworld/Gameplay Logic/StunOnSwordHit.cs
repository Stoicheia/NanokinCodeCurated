using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Events/Stun on Sword Hit")]
	public class StunOnSwordHit : SerializedMonoBehaviour, IHitHandler<SwordHit>
	{
		public void OnHit(SwordHit info)
		{
			if (!isActiveAndEnabled)
				return;

			Stunnable stunnable = gameObject.GetComponent<Stunnable>();
			stunnable.Stunned = true;
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
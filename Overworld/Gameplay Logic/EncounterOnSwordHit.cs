using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Events/Encounter on Sword Hit")]
	public class EncounterOnSwordHit : SerializedMonoBehaviour, IHitHandler<SwordHit>
	{
		public EncounterAdvantages Advantage = EncounterAdvantages.Player;

		public void OnHit(SwordHit hit)
		{
			if (!isActiveAndEnabled)
				return;

			GetComponent<EncounterMonster>().Trigger(Advantage);
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
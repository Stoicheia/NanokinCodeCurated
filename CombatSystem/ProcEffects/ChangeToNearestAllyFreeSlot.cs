using System.Linq;
using Combat.Toolkit;
using UnityEngine;

namespace Combat.Data
{
	public class ChangeToNearestAllyFreeSlot : ProcEffect
	{
		protected override ProcEffectFlags ApplyFighter()
		{
			Team team = battle.GetTeam(fighter);
			if (team == null)
			{
				Debug.Log("WARNING: The victim has no team. (cannot move to ally free slot)");
				return ProcEffectFlags.NoEffect;
			}

			Slot slot = team.slots.all.FirstOrDefault(s => !s.taken);
			if (slot == null)
			{
				Debug.Log("WARNING: No free slots in the team. (cannot move to ally free slot)");
				return ProcEffectFlags.NoEffect;
			}

			return new SwapHome(slot).TryApplyFighter();
		}
	}
}
using System;
using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Data
{
	[LuaUserdata]
	public class InFrontTargeter : TargetSystem
	{
		public int  n;
		public bool continuous;

		public InFrontTargeter(int n, bool continuous)
		{
			this.n          = n;
			this.continuous = continuous;
		}

		[NotNull]
		public override List<Target> EvaluateTargets(Battle battle, [NotNull] Fighter source)
		{
			var ret = new List<Target>();

			Slot home = source.home;
			if (home == null)
				return ret;

			Vector2Int start = home.coord;
			Vector2Int fwd   = home.forward;


			var fighters = new List<Fighter>();

			if (fwd == Vector2Int.zero)
			{
				Debug.LogError($"Bad forward value {fwd} for source slot at {start} cannot work with InFrontTargeter. Correct the forward value of the slots for this arena.");
				return ret;
			}

			// Find the first fighter in front for the target
			// ----------------------------------------
			while (true)
			{
				start += fwd;
				Slot slot = battle.GetSlot(start);

				if (slot == null)
					// Ran into a wall or something
					return ret;

				Fighter fighter = slot.owner;
				if (fighter == null)
					continue;

				if (!friendRestriction.Matches(battle, source, fighter))
					continue;

				break;
			}

			if (continuous)
			{
				for (var i = 0; i < n || n == -1; i++)
				{
					Vector2Int pos  = start + fwd * i;
					Slot       slot = battle.GetSlot(pos);

					if (slot == null || slot.owner == null)
						break;

					fighters.Add(slot.owner);
				}
			}
			else
			{
				throw new NotImplementedException("Only continuous targets are currently implemented. (InFrontTargeter)");
			}

			ret.Add(new Target(fighters));
			return ret;
		}
	}
}
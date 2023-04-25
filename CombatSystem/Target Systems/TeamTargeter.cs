using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;

namespace Combat.Data
{
	/// <summary>
	/// Gets targets containing all members of teams matching certain restrictions.
	/// </summary>
	[LuaUserdata]
	public class TeamTargeter : TargetSystem
	{
		public TargetTypes typeRestriction = TargetTypes.Fighter;

		[NotNull]
		public override List<Target> EvaluateTargets([NotNull] Battle battle, Fighter source)
		{
			var ret = new List<Target>();

			foreach (Team team in battle.teams)
			{
				if (friendRestriction.Matches(battle, source, team))
				{
					List<Fighter> fighters = team.fighters;
					SlotGrid    slots    = team.slots;

					Target target = null;

					switch (typeRestriction)
					{
						case TargetTypes.Fighter when fighters.Count > 0:
							target = new Target(fighters);
							break;

						case TargetTypes.Slot when slots.all.Count > 0:
							target = new Target(slots.all);
							break;
					}

					if (target != null)
						ret.Add(target);
				}
			}

			return ret;
		}
	}
}
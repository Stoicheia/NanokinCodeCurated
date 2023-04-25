using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;

namespace Combat.Data
{
	/// <summary>
	/// Get targets comprised of single targetables according to some restrictions.
	/// </summary>
	[LuaUserdata]
	public class SingularTargeter : TargetSystem
	{
		public TargetTypes typeRestriction;

		public SingularTargeter(Friendliness friendRestriction, TargetTypes typeRestriction) : base(friendRestriction)
		{
			this.typeRestriction = typeRestriction;
		}

		[NotNull]
		public override List<Target> EvaluateTargets(Battle battle, Fighter source)
		{
			var targets = new List<Target>();

			// Debug.Log(typeRestriction + ", " + battle.fighters.Count);

			if (typeRestriction == TargetTypes.Fighter)
			{
				foreach (Fighter entity in battle.fighters)
				{
					if (friendRestriction.Matches(battle, source, entity))
					{
						targets.Add(new Target(entity));
					}
				}
			}
			else if (typeRestriction == TargetTypes.Slot)
			{
				foreach (Slot slot in battle.slots)
				{
					if (friendRestriction.Matches(battle, source, slot))
					{
						targets.Add(new Target(slot));
					}
				}
			}

			return targets;
		}
	}
}
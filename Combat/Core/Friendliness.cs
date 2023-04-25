using System;
using JetBrains.Annotations;

namespace Combat.Data
{
	[Flags]
	public enum Friendliness
	{
		Self  = 0b001,
		Ally  = 0b010,
		Enemy = 0b100,
		Any   = 0b111
	}

	public static class FriendlinessExtensions
	{
		public static bool Matches(this Friendliness value, [NotNull] Battle battle, object self, object other)
		{
			Team team1 = battle.GetTeam(self);
			Team team2 = battle.GetTeam(other);

			if (value.HasFlag(Friendliness.Self))
			{
				if (other is Slot && self is Fighter selfEntity)
				{
					self = selfEntity.home;
				}

				return self == other;
			}

			if (value.HasFlag(Friendliness.Ally)) return team1 == team2;
			if (value.HasFlag(Friendliness.Enemy)) return team1 != team2; // Currently it is not possible to have an ally team! We would need a new 'alignment' service in order to fine-tune.
			if (value.HasFlag(Friendliness.Any)) return true;

			return false;
		}
	}
}
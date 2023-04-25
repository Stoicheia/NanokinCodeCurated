using System;
using JetBrains.Annotations;

namespace Combat.StandardResources
{
	public static class PlayerAlignmentsExtensions
	{
		public static bool MatchesTeam(this PlayerAlignments alignment, [CanBeNull] Team team)
		{
			// Not the greatest logic but that works for our current purposes
			bool isPlayer = team?.isPlayer == true;
			switch (alignment)
			{
				case PlayerAlignments.Ally:
					return isPlayer;

				case PlayerAlignments.Enemy:
					return !isPlayer;

				case PlayerAlignments.Neutral:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
			}
		}
	}
}
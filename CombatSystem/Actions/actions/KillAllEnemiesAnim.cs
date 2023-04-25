using Data.Combat;

namespace Combat.Toolkit
{
	/// <summary>
	/// Kill all enemies of a particular team in the battle.
	/// </summary>
	public class KillAllEnemiesAnim : BattleAnim
	{
		private readonly Team _team;

		public KillAllEnemiesAnim(Team team)
		{
			_team = team;
		}

		public override void RunInstant()
		{
			foreach (Team team in battle.teams)
			{
				if (team == _team)
					continue;

				foreach (Fighter ft in team.fighters)
				{
					battle.SetPoints(ft, Pointf.Zero);
				}
			}
		}
	}
}
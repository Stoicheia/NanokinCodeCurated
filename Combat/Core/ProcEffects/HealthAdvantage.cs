namespace Combat.StandardResources
{
	public class HealthAdvantage : AdvantagePlugin
	{
		public const float PERCENT = 0.15f;

		public HealthAdvantage(PlayerAlignments alignment) : base(alignment) { }

		protected override void OnApply()
		{
			foreach (Team team in battle.teams)
			{
				if (!alignment.MatchesTeam(team))
				{
					foreach (Fighter fter in team.fighters)
					{
						battle.AddHP(fter, -fter.max_points.hp * PERCENT);
					}
				}
			}
		}
	}
}
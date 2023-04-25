using Combat.Features.TurnOrder.Sampling.Operations.Scoping;

namespace Combat.StandardResources
{
	public class TurnAdvantage : AdvantagePlugin
	{
		public TurnAdvantage(PlayerAlignments alignment) : base(alignment) { }

		protected override void OnApply()
		{
			foreach (Team team in battle.teams)
			{
				if (!alignment.MatchesTeam(team))
					continue;

				foreach (Fighter teamMember in team.fighters)
				{
					battle.turns.Execute(op =>
					{
						op.lim_round();
						op.sel(op.next_group(teamMember));
						op.lim(op.get(ListSegments.round_head));
						op.move(op.get_max());
						// op.add(teamMember);
					}, @static: true);
				}
			}
		}
	}
}
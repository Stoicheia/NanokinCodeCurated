using Data.Combat;

namespace Combat.Data
{
	public class DrainOPByPercent : ProcEffect
	{
		public float percent;

		public DrainOPByPercent(float percent)
		{
			this.percent = percent;
		}

		public override float OPChange => -(battle.GetMaxPoints(fighter).op * percent * battle.GetComboMultiplier(dealer));

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(op: OPChange).ToChange(proc.noNumbers));
			battle.AddPoints(dealer, new Pointf(op: -OPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
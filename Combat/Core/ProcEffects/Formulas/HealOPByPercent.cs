using Data.Combat;

namespace Combat.Data
{
	public class HealOPByPercent : ProcEffect
	{
		public float percent;

		public HealOPByPercent(float percent)
		{
			this.percent = percent;
		}

		public override float OPChange
		{
			get
			{
				float v = battle.GetMaxPoints(fighter).op
				          * ctx.status.OPPercent(percent)
				          * battle.GetComboMultiplier(dealer);
				v = (int)v;
				v = ctx.status.OPChange(v);
				v = (int)v;

				return (int)v;
			}
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(op: OPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
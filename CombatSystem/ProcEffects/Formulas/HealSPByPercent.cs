using Data.Combat;

namespace Combat.Data
{
	public class HealSPByPercent : ProcEffect
	{
		public float percent;

		public HealSPByPercent(float percent)
		{
			this.percent = percent;
		}

		public override float OPChange
		{
			get
			{
				float v = battle.GetMaxPoints(fighter).op
				          * ctx.status.SPPercent(percent)
				          * battle.GetComboMultiplier(dealer);
				v = (int)v;
				v = ctx.status.SPChange(v);
				v = (int)v;
				if (v < 1 && percent > 0)
					v = 1;

				return (int)v;
			}
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(0, SPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
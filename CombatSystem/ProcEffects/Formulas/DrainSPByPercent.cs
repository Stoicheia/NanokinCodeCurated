using Data.Combat;

namespace Combat.Data
{
	public class DrainSPByPercent : ProcEffect
	{
		public float percent;

		public DrainSPByPercent(float percent)
		{
			this.percent = percent;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			float v = SPChange;

			battle.AddPoints(fighter, new PointChange(new Pointf(0, v), proc.noNumbers));
			battle.AddPoints(dealer, new PointChange(new Pointf(0, -v), proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}

		public override float SPChange
		{
			get
			{
				var v = (int)(battle.GetMaxPoints(fighter).sp * percent * battle.GetComboMultiplier(dealer));
				if (v < 1 && percent > 0)
					v = 1;
				return -v;
			}
		}
	}
}
using Data.Combat;
using UnityEngine;

namespace Combat.Data
{
	public class HealHPByPercent : ProcEffect
	{
		public float percent;

		public HealHPByPercent(float percent)
		{
			this.percent = percent;
		}

		public override float HPChange
		{
			get
			{
				float v = battle.GetMaxPoints(fighter).hp
				          * ctx.status.HPPercent(percent)
				          * battle.GetComboMultiplier(dealer);
				v = (int)v;
				v = ctx.status.HPChange(v);
				v = (int)v;
				if (v < 1 && percent > Mathf.Epsilon)
					v = 1;

				return v;
			}
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new PointChange
			{
				value     = new Pointf(HPChange),
				noNumbers = proc.noNumbers
			});

			return ProcEffectFlags.VictimEffect;
		}
	}
}
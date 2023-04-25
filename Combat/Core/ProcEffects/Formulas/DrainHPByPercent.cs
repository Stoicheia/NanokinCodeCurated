using Anjin.Util;
using Data.Combat;

namespace Combat.Data
{
	public class DrainHPByPercent : ProcEffect
	{
		public float percent;

		public override float HPChange
		{
			get
			{
				Pointf victimMaxPoints = battle.GetMaxPoints(fighter);
				return -(int)(victimMaxPoints.hp * percent * battle.GetComboMultiplier(dealer));
			}
		}

		public DrainHPByPercent(float percent)
		{
			this.percent = percent;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			float v = HPChange;

			battle.AddPoints(dealer, new Pointf(-v).ToChange(proc.noNumbers));
			battle.AddPoints(fighter, new Pointf(v).ToChange(proc.noNumbers));

			return ProcEffectFlags.VictimEffect;
		}
	}
}
using Data.Combat;

namespace Combat.Data
{
	/// <summary>
	/// A proc effect which hurts the user's HP with an amount equal to a percentage of its max HP.
	/// </summary>
	public class HurtSPByPercent : ProcEffect
	{
		public float    percent;
		public Elements element;

		public HurtSPByPercent(float percent, Elements element = Elements.none)
		{
			this.percent = percent;
			this.element = element;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(0, Status.SPChange(SPChange)).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}

		public override float SPChange
		{
			get
			{
				Pointf max = battle.GetMaxPoints(fighter);
				float  v   = max.sp * Status.SPPercent(-(percent)) * battle.GetComboMultiplier(dealer);
				if (v < 1 && percent > 0)
					v = 1;

				return Status.SPChange(-v);
			}
		}
	}
}
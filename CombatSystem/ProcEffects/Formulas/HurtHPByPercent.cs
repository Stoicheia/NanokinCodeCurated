using Data.Combat;

namespace Combat.Data
{
	/// <summary>
	/// A proc effect which hurts the user's HP with an amount equal to a percentage of its max HP.
	/// </summary>
	public class HurtHPByPercent : ProcEffect
	{
		public float    percent;
		public Elements element;

		public HurtHPByPercent(float percent, Elements element = Elements.none)
		{
			this.percent = percent;
			this.element = element;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new PointChange
			{
				value   = new Pointf(HPChange),
				element = element
			});

			return ProcEffectFlags.VictimEffect;
		}

		public override float HPChange
		{
			get
			{
				Pointf victimMaxPoints = battle.GetMaxPoints(fighter);
				float v = victimMaxPoints.hp
				          * Status.HPPercent(-(percent))
				          * battle.GetComboMultiplier(dealer);

				if (v < 1 && percent > 0)
					v = 1;

				return Status.HPChange(-v);
			}
		}
	}
}
using Data.Combat;

namespace Combat.Data
{
	/// <summary>
	/// A proc effect which hurts the user's HP with an amount equal to a percentage of its max HP.
	/// </summary>
	public class HurtOPByPercent : ProcEffect
	{
		public float    percent;
		public Elements element;

		public HurtOPByPercent(float percent, Elements element = Elements.none)
		{
			this.percent = percent;
			this.element = element;
		}

		public override float OPChange => Status.HPChange(-(battle.GetMaxPoints(fighter).op * Status.OPPercent(-percent) * battle.GetComboMultiplier(dealer)));

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(op: Status.OPChange(OPChange)).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
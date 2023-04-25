using Data.Combat;

namespace Combat.Data
{
	public class HealOP : ProcEffect
	{
		public float value;

		public HealOP(float value)
		{
			this.value = value;
		}

		public override float OPChange => Status.OPChange(value);

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(op: value).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
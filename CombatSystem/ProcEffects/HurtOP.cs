using Data.Combat;

namespace Combat.Data
{
	public class HurtOP : ProcEffect
	{
		public float value;

		public HurtOP(float value)
		{
			this.value = value;
		}

		public override float OPChange => Status.OPChange(-value);

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(op: OPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
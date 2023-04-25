using Data.Combat;
using JetBrains.Annotations;

namespace Combat.Data
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class DrainOP : ProcEffect
	{
		public float value;

		public DrainOP(float value)
		{
			this.value = value;
		}

		public override float OPChange => -value;

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new PointChange(new Pointf(op: OPChange), proc.noNumbers));
			battle.AddPoints(dealer, new PointChange(new Pointf(op: -OPChange), proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
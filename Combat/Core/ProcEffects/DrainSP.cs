using Data.Combat;
using JetBrains.Annotations;

namespace Combat.Data
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class DrainSP : ProcEffect
	{
		public int value;

		public DrainSP(int value)
		{
			this.value = value;
		}

		public override float SPChange => Status.SPChange(-value);

		protected override ProcEffectFlags ApplyFighter()
		{
			float v = SPChange;

			battle.AddPoints(fighter, new Pointf(sp: v).ToChange(proc.noNumbers));
			battle.AddPoints(dealer, new Pointf(sp: -v).ToChange(proc.noNumbers));

			return ProcEffectFlags.VictimEffect;
		}
	}
}
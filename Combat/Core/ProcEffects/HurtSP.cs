using Data.Combat;

namespace Combat.Data
{
	public class HurtSP : ProcEffect
	{
		public int value;

		public HurtSP(int value)
		{
			this.value = value;
		}

		//public override float SPChange => stats.SPChange(-((value + bonus) * multiplier));

		public override float SPChange => Status.SPChange(-value);

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(sp: SPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
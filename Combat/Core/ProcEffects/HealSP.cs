using Data.Combat;

namespace Combat.Data
{
	public class HealSP : ProcEffect
	{
		/// <summary>
		/// Exact SP to be gained from this
		/// </summary>
		public int value;

		public HealSP(int value)
		{
			this.value = value;
		}

		public override float SPChange => Status.SPChange(value);

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(sp: SPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
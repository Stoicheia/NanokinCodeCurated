using Data.Combat;

namespace Combat.Data
{
	public class HealHP : ProcEffect
	{
		/// <summary>
		/// Exact health to be gained from this
		/// </summary>
		public int value;

		public HealHP(int value)
		{
			this.value = value;
		}

		//public override float HPChange => stats.HPChange(((value + bonus) * multiplier));

		public override float HPChange => Status.HPChange(value);

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new Pointf(HPChange).ToChange(proc.noNumbers));
			return ProcEffectFlags.VictimEffect;
		}
	}
}
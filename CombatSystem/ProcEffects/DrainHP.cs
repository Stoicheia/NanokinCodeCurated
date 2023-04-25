using Anjin.Util;
using Data.Combat;

namespace Combat.Data
{
	public class DrainHP : ProcEffect
	{
		public int value;

		public override float HPChange => -value;

		public DrainHP(int value)
		{
			this.value = value;
		}

		public DrainHP(float value) : this(value.Floor()) { }

		protected override ProcEffectFlags ApplyFighter()
		{
			float v = Status.HPDrain(value);

			battle.AddPoints(dealer, new Pointf(v).ToChange(proc.noNumbers));
			battle.AddPoints(fighter, new Pointf(-v).ToChange(proc.noNumbers));

			return ProcEffectFlags.VictimEffect;
		}
	}
}
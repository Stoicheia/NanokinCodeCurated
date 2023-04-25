namespace Combat.Data
{
	/// <summary>
	/// Add a fighter to the combat.
	/// </summary>
	public class AddFighter : ProcEffect
	{
		public Fighter add;

		public AddFighter(Fighter add)
		{
			this.add = add;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddFighter(add);
			return ProcEffectFlags.NoEffect;
		}

		protected override ProcEffectFlags ApplySlot()
		{
			return ApplyFighter();
		}
	}
}
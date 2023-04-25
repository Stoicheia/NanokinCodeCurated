namespace Combat.Data.Decorative
{
	public class MissEffect : ProcEffect
	{
		protected override ProcEffectFlags ApplyFighter() => ProcEffectFlags.MetaEffect;
	}
}
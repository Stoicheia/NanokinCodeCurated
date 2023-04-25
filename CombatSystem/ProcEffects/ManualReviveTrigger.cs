using Combat.Features.TurnOrder.Sampling.Operations;
using JetBrains.Annotations;

namespace Combat.Data
{
	public class ManualReviveTrigger : ProcEffect
	{
		public ManualReviveTrigger() { }

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.runner.SubmitReviveFlush();
			return ProcEffectFlags.MetaEffect;
		}

		protected override ProcEffectFlags ApplySlot()
		{
			battle.runner.SubmitReviveFlush();
			return ProcEffectFlags.MetaEffect;
		}
	}
}
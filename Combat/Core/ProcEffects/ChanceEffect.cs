using Combat.Features.TurnOrder.Sampling.Operations;
using JetBrains.Annotations;

namespace Combat.Data
{
	public class ChanceEffect : ProcEffect
	{
		public ProcEffect potential;

		public ChanceEffect(float chance, ProcEffect potential)
		{
			this.chance      = chance;
			this.potential   = potential;
			potential.Parent = this;
		}

		public ChanceEffect(float chance, [NotNull] State state) : this(chance, new AddState(state))
		{
			state.Parent = this;
		}

		public ChanceEffect(float chance, TurnFunc op) : this(chance, new TurnFuncEffect(op)) { }

		protected override ProcEffectFlags ApplyFighter()
		{
			float chc = chance
				.Mod(Status.luck)
				.Mod(Status.effect_luck);

			if (RNG.Chance(chc))
			{
				potential.battle  = battle;
				potential.proc    = proc;
				potential.dealer  = dealer;
				potential.fighter = fighter;
				potential.slot    = slot;
				potential.ctx     = ctx;

				return potential.TryApplyFighter();
			}

			return ProcEffectFlags.DecorativeEffect;
		}
	}
}
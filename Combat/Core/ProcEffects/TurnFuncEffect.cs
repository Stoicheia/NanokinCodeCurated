using System;
using Combat.Features.TurnOrder.Events;
using Combat.Features.TurnOrder.Sampling.Operations;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class TurnFuncEffect : ProcEffect
	{
		public TurnFunc func;

		public TurnFuncEffect(TurnFunc func)
		{
			this.func   = func;
			this.chance = this.func.chance;
		}

		public TurnFuncEffect(Action<TurnSM> action)
		{
			func.action = action;
		}

		public TurnFuncEffect(Closure closure)
		{
			func.closure = closure;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			ITurnActer acter = fighter;
			var op = new TurnSM(battle.turns)
			{
				me = acter
			};

			battle.turns.Execute(func, op);


			return ProcEffectFlags.MetaEffect;
		}
	}
}
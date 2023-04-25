using System;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoonSharp.Interpreter;

namespace Combat.Toolkit
{
	public class TurnQueryAnim : BattleAnim
	{
		private readonly TurnFunc _runner;

		public TurnQueryAnim(Action<TurnSM> op)
		{
			_runner.action = op;
		}

		public TurnQueryAnim(Closure closure)
		{
			_runner.closure = closure;
		}

		public override void RunInstant()
		{
			battle.turns.Execute(_runner);
		}

		public override async UniTask RunAnimated()
		{
			TurnSM op    = battle.turns.Execute(_runner);
			Tween        tween = TurnUI.Animate(op);

			if (op.modified)
			{
				await tween.WithCancellation(cts.Token).SuppressCancellationThrow();
			}
		}


		// public static implicit operator TurnQueryAction(TurnQueryList query) => new TurnQueryAction(query);
	}
}
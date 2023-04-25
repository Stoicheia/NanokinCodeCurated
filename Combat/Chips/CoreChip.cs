using System;
using System.Collections.Generic;
using Combat.Components;
using Combat.Data;
using Combat.Toolkit;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Combat
{
	/// <summary>
	/// Implements core functionality of the battle system.
	/// - Wait
	/// - Handler execution & animation
	/// </summary>
	public class CoreChip : Chip
	{
		private UniTaskBatch  _tasks            = new UniTaskBatch();
		private List<Trigger> _nextTriggerFires = new List<Trigger>();

		[Title("Debug")]
		[ShowInPlay, CanBeNull]
		private BattleAnim _activeAnim;

		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();

			Handle(CoreOpcode.Wait, HandleWait);
			Handle(CoreOpcode.Emit, HandleEmit);
			Handle(CoreOpcode.Emit, HandleEmitAsync);
			Handle(CoreOpcode.Execute, HandleExecute);
			Handle(CoreOpcode.Execute, HandleExecuteAsync);
			Handle(CoreOpcode.AwaitExecution, HandleAwaitExecution);
		}

		public override bool CanHandle(CoreInstruction ins)
		{
			_nextTriggerFires.Clear();

			if (ins.op == CoreOpcode.Emit)
			{
				TriggerEvent  ev  = ins.triggerEvent;
				List<Trigger> ret = battle.EmitDry(ins.signal, ins.me ?? ev.me, ev);

				_nextTriggerFires.AddRange(ret);
				return ret.Count > 0;
			}

			return true;
		}

		private async UniTask HandleAwaitExecution(CoreInstruction ins)
		{
			await runner.AwaitActions();
		}

		private async UniTask HandleWait(CoreInstruction ins)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(ins.duration));
		}

		private void HandleEmit(ref CoreInstruction ins)
		{
			if (runner.animated) return;

			if (ins.me != null)
				ins.triggerEvent.me = ins.me;

			battle.Emit(ins.signal, ins.me ?? ins.triggerEvent.me, ins.triggerEvent);
		}

		private async UniTask HandleEmitAsync(CoreInstruction ins)
		{
			TriggerEvent ev = ins.triggerEvent;

			foreach (Trigger tg in _nextTriggerFires)
			{
				// await core.ExecuteActionAsync(new AdvanceTurnOrder(tg));
				await TurnUI.AnimateAdvance(tg);
				battle.FireTrigger(tg, ev);
				await runner.AwaitActions();
			}

			_nextTriggerFires.Clear();
		}

		/// <summary>
		/// Execute action without animation. (in simulated combat, such as unit tests)
		/// </summary>
		private void HandleExecute(ref CoreInstruction ins)
		{
			if (!runner.animated)
			{
				if (ins.anim != null)
				{
					runner.ExecuteAction(ins.anim);
				}

				if (ins.actions != null)
				{
					foreach (BattleAnim act in ins.actions)
					{
						if (act != null)
							runner.ExecuteAction(act);
					}
				}
			}
		}

		private async UniTask HandleExecuteAsync(CoreInstruction ins)
		{
			bool halts = true;

			if (ins.anim != null)
			{
				_tasks.Add(runner.ExecuteActionAsync(ins.anim));
				halts = halts && ins.anim.Halts;
			}

			if (ins.actions != null)
			{
				foreach (BattleAnim act in ins.actions)
				{
					if (act == null)
						continue;

					_tasks.Add(runner.ExecuteActionAsync(act));
					halts = halts && act.Halts;
				}
			}

			if (halts)
				await _tasks;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using Combat.Toolkit;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items.Scripting;
using UnityEngine;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat
{
	/// <summary>
	/// The standard battle flow in a match of Nanokin!
	/// Implements turn based combat.
	///
	/// Behold, the power of ascii
	///
	/// StartBattle
	/// ---- StartTurn <--------------
	/// ---- ---- ActTurn             \
	/// ---- ---- ---- ActTurnBrain   |
	/// ---- ---- EndTurn             |
	/// ---- ---- --- StepTurn        |
	/// ---- ---- --- ---- StartTurn  |
	///
	/// </summary>
	public class FlowChip : Chip, ILogger
	{
		private State              _state;
		private State              _savedFlowState;
		private List<TriggerState> _savedTriggerStates; // For decoration
		private bool               _simulating;

		public struct State
		{
			public FlowChip                    chip;
			public ITurnActer                  lastActingTurn;
			public Dictionary<ITurnActer, int> elapsedRoundTurns; // Tracks how many turns an event has had since the start of the round (does not need to act)

			public State(FlowChip chip)
			{
				this.chip         = chip;
				lastActingTurn    = null;
				elapsedRoundTurns = new Dictionary<ITurnActer, int>();
			}

			public void CopyTo(ref State state)
			{
				// Copy our values into the state, reusing existing dictionary and list
				state.lastActingTurn = lastActingTurn;
				state.elapsedRoundTurns.Clear();
				foreach (var kvp in elapsedRoundTurns)
					state.elapsedRoundTurns[kvp.Key] = kvp.Value;
			}
		}

		private TurnSystem turnsys => battle.turns;

	#region Logging

		public string LogID       => "FlowChip";
		public bool   LogSilenced => _simulating;

	#endregion

		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();

			// Battle flow:
			Handle(CoreOpcode.StartBattle, HandleStartBattle);
			Handle(CoreOpcode.StopBattle, HandleStopBattle);

			// Handler flow:
			Handle(CoreOpcode.StartRound, HandleUpdateRound);
			Handle(CoreOpcode.StartTurn, HandleStartTurn);
			Handle(CoreOpcode.StartAct, HandleStartAct);
			Handle(CoreOpcode.ActTurn, HandleActTurn);
			Handle(CoreOpcode.ActTurnBrain, HandleActTurnBrainAsync);
			Handle(CoreOpcode.EndTurn, HandleEndTurn);
			Handle(CoreOpcode.StepTurn, HandleStepTurn);
		}

		public override void Install()
		{
			_state              = new State(this);
			_savedFlowState     = new State(this);
			_savedTriggerStates = new List<TriggerState>();

			battle.turns.decorator = DecorateTurnOrder;
		}

		public override void Uninstall()
		{
			_state          = new State();
			_savedFlowState = new State();
		}

		private void HandleUpdateRound(ref CoreInstruction ins)
		{
			switch (turnsys.action.marker)
			{
				case ActionMarker.RoundHead:
					// Reset round state
					_state.lastActingTurn = null;
					_state.elapsedRoundTurns.Clear();
					foreach (ITurnActer ev in turnsys.acters)
						_state.elapsedRoundTurns.Add(ev, 0);

					break;

				case ActionMarker.RoundBody:
					break;

				case ActionMarker.RoundTail:
					break;
			}
		}

		private void HandleStartBattle(ref CoreInstruction ins)
		{
			// Register all skills
			// ----------------------------------------
			foreach (Fighter fighter in battle.fighters)
			{
				var skillInfos = new List<SkillInfo>();
				fighter.info.GetSkills(skillInfos);

				foreach (SkillInfo info in skillInfos)
				{
					SkillAsset skill = info.skill;
					if (skill.IsValid() && battle.RegisterSkill(fighter, skill, out BattleSkill skl))
					{
						skl.limb = info.limb;
					}
				}

				Debug.Log($"{fighter.info.Name} has {skillInfos.Count} skills ({string.Join(", ", fighter.skills.Select(s => s.EnvID))})");
			}

			foreach (BattleSkill skl in battle.skills)
			{
				Fighter fighter = skl.user;

				BattleAnim init = skl.Init();
				if (init != null)
					runner.ExecuteAction(init);

				BattleAnim load = skl.Load(fighter.info);
				if (load != null)
					runner.ExecuteAction(load);
			}


			// Generate initial turns
			// ----------------------------------------
			turnsys.FillBuffer();
			turnsys.SetToZero();

			// Register all plugins
			// ---------------------------------------
			foreach (BattleCorePlugin plugin in runner.initPlugins)
				plugin.Register(runner, battle);

			foreach (BattleCorePlugin plugin in runner.instancePlugins)
				plugin.Register(runner, battle);


			// Apply all passive skills
			// ----------------------------------------
			if (GameOptions.current.combat_passives)
			{
				foreach (Fighter fighter in battle.fighters)
				{
					List<SkillInfo> skillInfos = new List<SkillInfo>();
					fighter.info.GetSkills(skillInfos);

					foreach (BattleSkill instance in fighter.skills)
					{
						BattleAnim anim = instance.Passive();
						if (anim != null)
							runner.Submit(CoreOpcode.Execute, new CoreInstruction { anim = anim });
					}
				}
			}

			// Apply all stickers passives (and equipments)
			// ----------------------------------------
			foreach (Fighter fighter in battle.fighters)
			foreach (StickerInstance sticker in fighter.stickers)
			{
				BattleSticker instance = battle.GetSticker(fighter, sticker);
				BattleAnim    anim     = instance?.Passive();

				if (anim != null)
					runner.Submit(anim);
			}


			// Setup all coaches
			// ----------------------------------------
			foreach (Fighter fter in battle.fighters)
			{
				fter.coach?.SetIdle();
			}

			// Kick off the combat loop
			// ----------------------------------------

			if (battle.fighters.Count == 0)
			{
				Debug.LogWarning("There are no fighters in the battle, cannot start combat!");
				return;
			}

			if (runner.animated)
			{
				TurnUI.Initialize(battle);
				TurnUI.Sync();
			}

			runner.Submit(CoreOpcode.IntroduceTurns);
			runner.Submit(CoreOpcode.StartTurn);
		}

		private void HandleStopBattle(ref CoreInstruction ins)
		{
			throw new NotImplementedException();
		}

		private void HandleStartTurn(ref CoreInstruction ins)
		{
			Action     action = turnsys.action;
			ITurnActer acter  = action.acter;

			runner.Submit(CoreOpcode.FlushDeaths);

			switch (action.marker)
			{
				case ActionMarker.RoundHead:
					if (turnsys.round > -1)
						runner.SubmitEmit(Signals.end_round, GetRoundTriggerEvent());

					runner.Submit(CoreOpcode.StartRound);
					runner.SubmitEmit(Signals.start_round, GetRoundTriggerEvent());
					break;

				case ActionMarker.RoundBody:
					break;

				case ActionMarker.RoundTail:
					break;

				case ActionMarker.Action:
					TurnEvent @event = GetTurnTriggerEvent();
					@event.cancelable = true;

					if (runner.logTurns)
						this.LogEffect("--", $"-------------------- Handler {turnsys.Index} :: {acter} -------------------- ");

					// START ROUND
					IncrementRoundCounter(acter);

					// START TURNS
					if (_state.lastActingTurn != acter)
					{
						// battle.Emit(Signals.start_turns, ev, GetTurnEvent());
						runner.SubmitEmit(Signals.start_turns, acter, @event);

						if (acter is Fighter fter)
							fter.coach?.SetTurn();
					}

					// START TURN (FIRST)
					if (_state.elapsedRoundTurns[acter] == 1)
					{
						runner.SubmitEmit(Signals.start_turn_first, acter, @event);
					}

					// START TURN
					runner.SubmitEmit(Signals.start_turn, acter, @event);
					runner.Submit(CoreOpcode.StartAct);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			// END TURN
			runner.Submit(CoreOpcode.EndTurn);
		}

		private void HandleStartAct(ref CoreInstruction ins)
		{
			Action     action = turnsys.action;
			ITurnActer acter  = action.acter;

			// ACT TURN
			if (!battle.HasFlag(acter as Fighter, EngineFlags.skip_turn))
			{
				TurnEvent @event = GetTurnTriggerEvent();

				// START ACT
				if (_state.lastActingTurn != acter)
				{
					runner.SubmitEmit(Signals.start_acts, acter, @event);
				}

				// START TURN (FIRST)
				if (_state.elapsedRoundTurns[acter] == 1)
				{
					runner.SubmitEmit(Signals.start_act_first, acter, @event);
				}

				runner.SubmitEmit(Signals.start_act, acter, @event);
				runner.Submit(CoreOpcode.ActTurn);
				runner.SubmitEmit(Signals.end_act, acter, @event);

				// END ACT
			}

			// core.SubmitEmit(Signals.start_turn_first, ev, GetTurnEvent());
		}

		private void DecorateTurnOrder(List<Action> decoratedOrder)
		{
			battle.silent = true;
			_simulating   = true;

			foreach (Trigger trg in battle.triggers)
				_savedTriggerStates.Add(trg.state);

			TurnSystem turnsys = battle.turns;

			_state.CopyTo(ref _savedFlowState);

			int startIndex = turnsys.index;
			int startRound = turnsys.round;

			List<CoreInstruction> instructions = new List<CoreInstruction>
			{
				new CoreInstruction { op = CoreOpcode.StartTurn }
			};

			while (instructions.Count > 0 && turnsys.index < turnsys.Count - 1)
			{
				runner.BeginCapture();

				CoreInstruction ins = instructions[0];
				instructions.RemoveAt(0);

				switch (ins.op)
				{
					case CoreOpcode.StartTurn:
						HandleStartTurn(ref ins);
						break;

					case CoreOpcode.StartRound:
					case CoreOpcode.StartAct:
						OnActTurnStart(turnsys.action.acter);
						decoratedOrder.Add(turnsys.action);
						break;


					case CoreOpcode.StepTurn:
						turnsys.Advance(true, true);
						runner.Submit(CoreOpcode.StartTurn);
						break;

					case CoreOpcode.Emit:
						List<Trigger> emits = battle.EmitDry(ins.signal, ins.me ?? ins.triggerEvent.me, ins.triggerEvent);

						foreach (Trigger trigger in emits)
						{
							if (!trigger.enableTurnUI)
								continue;

							decoratedOrder.Add(new Action(trigger, trigger.state.numHandled));
							trigger.state.numHandled++;
						}

						break;

					case CoreOpcode.EndTurn:
						HandleEndTurn(ref ins);
						break;
				}

				List<CoreInstruction> results = runner.EndCapture();
				for (int i = results.Count - 1; i >= 0; i--)
					instructions.Insert(0, results[i]);
			}

			// Restore states
			turnsys.Index = startIndex;
			turnsys.round = startRound;

			_savedFlowState.CopyTo(ref _state);

			foreach (TriggerState state in _savedTriggerStates)
				state.value.state = state;
			_savedTriggerStates.Clear();

			_simulating   = false;
			battle.silent = false;
		}

		private void HandleActTurn(ref CoreInstruction ins)
		{
			// Get the team brain for the event.
			ITurnActer ev = battle.ActiveActer;
			OnActTurnStart(ev);

			runner.Submit(CoreOpcode.FlushDeaths);

			if (battle.animated)
				runner.Submit(new AdvanceTurnAnim(ev));

			// core.Submit(CoreOpcode.AwaitExecution);
			runner.Submit(CoreOpcode.ActTurnBrain);
		}

		private async UniTask HandleActTurnBrainAsync(CoreInstruction data)
		{
			// await core.AwaitActions();

			Action     action = turnsys.action;
			ITurnActer ev     = action.acter;

			if (ev is Fighter fter)
			{
				BattleBrain brain = battle.GetBrain(fter);
				if (brain != null)
				{
					brain.fighter = fter;
					brain.cts     = new CancellationTokenSource();

					BattleAnim baction = await brain.GrantAction();

					if (baction != null && baction.targeting != null && baction.fighter != null)
						battle.ModifyTargetPicks(baction.fighter, baction.useInfo, baction.targeting);

					brain.cts.Dispose();
					brain.cts = null;

					if (baction != null)
					{
						// core.Insert(new Instructions.Wait(GameOptions.current.combat_fast_pace ? 0.025f : 0.2f)); // Slight pause to add some breathing space.
						runner.Submit(baction);
						// core.Insert(new Instructions.Wait(GameOptions.current.combat_fast_pace ? 0.025f : 0.2f));
					}
				}
			}
		}

		private void OnActTurnStart([NotNull] ITurnActer ev)
		{
			_state.lastActingTurn = ev;
		}

		private void HandleEndTurn(ref CoreInstruction ins)
		{
			ITurnActer ev       = battle.ActiveActer;
			Action     action   = battle.ActiveAction;
			Action?    nextTurn = turnsys.FindNextTurn();

			runner.Submit(CoreOpcode.FlushDeaths);
			runner.Submit(CoreOpcode.FlushRevives);

			if (action.marker == ActionMarker.Action)
			{
				// ON END TURN
				runner.SubmitEmit(Signals.end_turn, GetTurnTriggerEvent());

				// ON END TURNS
				if (nextTurn.HasValue && ev != nextTurn.Value.acter)
				{
					// Next event is different
					runner.SubmitEmit(Signals.end_turns, GetTurnTriggerEvent());

					if (ev is Fighter fter)
						fter.coach?.SetIdle();
				}

				// ON END ACTS
				if (nextTurn.HasValue
				    && ev == _state.lastActingTurn
				    && _state.lastActingTurn != nextTurn.Value.acter)
				{
					runner.SubmitEmit(Signals.end_acts, GetTurnTriggerEvent());
				}
			}

			if (action.marker == ActionMarker.Action && !GameOptions.current.combat_fast_pace)
				runner.SubmitWait(0.1f);

			runner.Submit(CoreOpcode.FlushDeaths);
			runner.Submit(CoreOpcode.FlushRevives);
			runner.Submit(CoreOpcode.StepTurn);
		}

		private void HandleStepTurn(ref CoreInstruction ins)
		{
			// core.Insert(new Instructions.Wait(GameOptions.current?.combat_fast_pace == true ? 0.05f : 0.2f));
			battle.turns.Advance(true);

			runner.Submit(CoreOpcode.StartTurn);
		}

	#region Util

		private void IncrementRoundCounter([NotNull] ITurnActer ev)
		{
			if (!_state.elapsedRoundTurns.ContainsKey(ev))
				_state.elapsedRoundTurns[ev] = 0;

			_state.elapsedRoundTurns[ev]++;
		}

		public TurnEvent GetTurnTriggerEvent()
		{
			Action     action = turnsys.action;
			ITurnActer ev     = action.acter;

			return new TurnEvent(ev, action)
			{
				round_counter = _state.elapsedRoundTurns.GetOrDefault(ev)
			};
		}

		public RoundEvent GetRoundTriggerEvent()
		{
			return new RoundEvent(battle.RoundID);
		}

	#endregion
	}
}
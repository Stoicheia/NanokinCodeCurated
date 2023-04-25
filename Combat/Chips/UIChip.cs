using Anjin.Nanokin;
using Anjin.Utils;
using Combat.Components;
using Combat.Data;
using Combat.UI;
using Combat.UI.Info;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Combat
{
	/// <summary>
	/// Registers some triggers in the Battle
	/// and hooks events to the various UI elements of combat.
	/// </summary>
	public class UIChip : Chip
	{
		private const string BUFFID_POISON    = "poison";
		private const string BUFFID_CORRUPT   = "corrupt";
		private const string BUFFTAG_SP_DRAIN = "drain";
		private const string BUFFTAG_HP_DRAIN = "drain";

		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();

			Handle(CoreOpcode.StartBattle, HandleStartBattle);
		}

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync();
			await SceneLoader.GetOrLoadAsync(Addresses.Scene_UI_Combat);

			TargetUI.battle = runner.battle;
			// await core.HookHot(CombatUI.Live.GetComponentsInChildren<IChip>());

			// Currently can't be done, some recipe stuff needs the UI loaded by then
			// InstallBackground(core).ForgetWithErrors();

			// Register existing slots
			foreach (Slot slot in battle.slots)
				OnSlotAdded(slot);

			battle.SlotAdded += OnSlotAdded;

			battle.AddTriggerCsharp(OnAcceptDeath, new Trigger
			{
				ID     = "hud-entity-death",
				signal = Signals.acccept_death,
				filter = Trigger.FILTER_ANY
			});

			battle.AddTriggerCsharp(OnStartTurns, new Trigger
			{
				ID     = "hud-turn-start",
				signal = Signals.start_turns,
				filter = Trigger.FILTER_ANY
			});

			battle.AddTriggerCsharp(OnEndTurn, new Trigger
			{
				ID     = "hud-turn-end",
				signal = Signals.end_turn,
				filter = Trigger.FILTER_ANY
			});

			battle.AddTriggerCsharp(OnReceiveProc, new Trigger
			{
				ID     = "hud-receive-proc",
				signal = Signals.receive_proc,
				filter = Trigger.FILTER_ANY
			});

			battle.ComboChanged += (entity, newvalue) =>
			{
				ComboUI.UpdateCombo(newvalue).Forget();
			};

			battle.StateAdded            += OnStateAdded;
			battle.StateExpired          += OnStateExpired;
			battle.StateRefreshed        += OnStateRefreshed;
			battle.StateConsumed         += OnStateConsumed;
			battle.TriggerFiredEffective += OnTriggerFired;

			HealthbarUI.Live.SetBattle(battle);
		}

		public override void Uninstall()
		{
			base.Uninstall();

			SlotUI.Clear();
			StatusUI.Clear();
			TurnUI.Reset();
			StateUI.Clear();
			TargetUI.SetNone();
			CombatUI.Live.SetVisible(false);
			VirtualHomeUI.Kill();

			HealthbarUI.Live.SetBattle(null);
		}

		private void HandleStartBattle(ref CoreInstruction msg)
		{
			CombatUI.Live.SetVisible(true); // TODO check if this needs to be false for turnui, we're doing it in cardintoaction now

			StatusUI.Live.SnapToEnd();

			//TurnUI.Initialize(battle);
			//TurnUI.Sync();

			VirtualHomeUI.Initialize(battle);
			// VirtualHomeUI.Live.InitialiseFighterGraphics().GetAwaiter().GetResult();
		}

		private static void OnStateAdded(IStatee statee, State state)
		{
			StateUI.NotifyAdd(statee, state);
			RefreshStatus(statee);
		}

		private static void OnStateExpired(IStatee statee, State state)
		{
			StateUI.NotifyExpire(statee, state);
			RefreshStatus(statee);
		}

		private static void OnStateRefreshed(IStatee statee, State state)
		{
			StateUI.NotifyRefresh(statee, state);
			RefreshStatus(statee);
		}

		private static void OnStateConsumed(IStatee statee, State state)
		{
			StateUI.NotifyConsume(statee, state);
			RefreshStatus(statee);
		}

		private void OnTriggerFired(Trigger trigger)
		{
			if (trigger.Parent is State state)
			{
				foreach (IStatee statee in state.statees)
				{
					StateUI.NotifyEffect(statee, state);
				}
			}
		}

		private static void RefreshStatus(IStatee statee)
		{
			if (statee is Fighter fter && StatusUI.TryGetUI(fter, out StatusPanel ui))
			{
				var poisoned   = false;
				var corrupted  = false;
				var spdraining = false;
				var hpdraining = false;

				foreach (State state in fter.States)
				{
					if (state.ID == BUFFID_POISON) poisoned               = true;
					if (state.ID == BUFFID_CORRUPT) corrupted             = true;
					if (state.tags.Contains(BUFFTAG_HP_DRAIN)) hpdraining = true;
					if (state.tags.Contains(BUFFTAG_SP_DRAIN)) spdraining = true;
				}

				ui.poisoned   = poisoned;
				ui.corrupted  = corrupted;
				ui.spdraining = spdraining;
				ui.hpdraining = hpdraining;
			}
		}

		private static void OnReceiveProc(TriggerEvent ev)
		{
			var evproc = (ProcEvent)ev;
			if (evproc.hurts)
			{
				StatusUI.Hurt((Fighter)evproc.victim);
			}
		}

		private static void OnSlotAdded(Slot slot)
		{
			SlotUI.AddSlot(slot).Forget();
		}


		private void OnStartTurns([NotNull] TriggerEvent @event) { }

		private void OnEndTurn([NotNull] TriggerEvent @event) { }

		private void OnAcceptDeath(TriggerEvent @event)
		{
			// if (@event.Listener is CombatNanokinEntity actingParticipant && _hudsByUnit.TryGetValue(actingParticipant, out MonsterStatusUI unit))
			// {
			// 	MonsterStatusUI hudElement = _hudsByUnit[actingParticipant];
			// 	hud.SetWithFighter(fighter);
			// }
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Anjin.Utils;
using Combat.Components;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Toolkit;
using Combat.UI;
using Combat.UI.Info;
using Combat.UI.Notifications;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items.Scripting;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityUtilities;
using Util.RenderingElements.Barrel;
using Util.RenderingElements.PanelUI.Graphics;
using Util.RenderingElements.TriangleMenu;
using Action = Combat.Features.TurnOrder.Action;
using Object = UnityEngine.Object;

namespace Combat
{
	/// <summary>
	/// Main team brain for the player which implements UI interaction for picking actions.
	/// It is a FSM controlled by ChangeState(). Gameplay and visual elements that are common
	/// to several states are coordinated using their respective Update functions:
	/// - UpdateMenuPanels
	/// - UpdateInfoPanel
	/// - UpdateCategoryPanels
	/// - UpdateTarget
	/// - UpdateStatusUI
	/// - UpdateCamera
	/// - UpdateOverdrive
	/// These functions can be called at any point to update the respective front-end feature
	/// to its correct state according to internal state of the brain.
	/// This makes it fairly easy to plug in new features and make tweaks, and minimizing bugs in the process.
	/// Simply call the frontend functions you think are relevant after a change to the internal state,
	/// and the rest falls into place.
	/// </summary>
	[Serializable]
	public class PlayerBrain : BattleBrain
	{
		private const int    HORIZONTAL_STICKER_INDEX   = 4; // (limb, limb, limb, limb, stickers)
		private const int    ACT_PANEL_WINDOW_SPAN      = 3;
		private const string ADDR_OVERDRIVE_ENTRY       = "Combat/UI/Overdrive Entry";
		private const string HEALTHBAR_ENABLE_FLAG_NAME = "player_brain_hold";

		[NonSerialized] public BattleClient netplayClient;

		private Dictionary<Fighter, MemorizationState> _memorizationStates;
		private AsyncLazy<Scene>                       _triangleLoadTask;

		private States     _state = States.None;
		private BattleAnim _actAnim;
		private bool       _actDone;
		private bool       _keepOpen;
		private bool       _showingInfo = false;

		// Overdrive ------------------------------
		private int               _overdriveSlots;   // How many overdrive ticks to use this action.
		private List<TurnCommand> _overdriveActions; // The command list for this action's overdrive.
		private int               _overdriveTotalCost;

		// Selections ------------------------------
		private BattleSkill   _skillsel;
		private BattleSticker _stickersel;

		// UI ------------------------------
		private TriangleMenuUI      _triangle;
		private PanelMenu           _menu;
		private PanelScrollingLabel _hswitcher;
		[CanBeNull]
		private StatusPanel _statusPanel;

		// Targeting ------------------------------
		private Targeting                        _targets;
		private Targeting                        _unoccupiedSlots;
		private Target                           _selectedTarget;
		private Target                           _selectedUnoccupiedSlot;
		private int                              _targetIndex;
		private int                              _unoccupiedSlotIndex;
		private GameObject                       _indicatorPrefab;
		private List<GameObject>                 _indicators;
		private Dictionary<Fighter, Stack<Slot>> _virtualHomeStacks;
		private Dictionary<Fighter, Slot>        _tempNewVirtualHomes;

		// Visuals ------------------------------
		private BlinkVFX            _blinkFighter, _blinkStatusUI, _blinkCoach;
		private float               _elapsedFlashTime;
		private int                 _stickerselIndex;
		private List<BattleSticker> _actableStickers;

		private Fighter _absolvent;

		private ArenaCamera camera => Runner.io.arena.Camera;

		public enum States
		{
			None,

			/// <summary>
			/// Choosing whether to act, hold, flee ...
			/// </summary>
			MainChoose,

			/// <summary>
			/// Choose the action to perform.
			/// </summary>
			ActChoose,

			/// <summary>
			/// Choose the target for the action.
			/// </summary>
			ActTarget,

			/// <summary>
			/// Choose the destination for a formation change.
			/// </summary>
			MoveTarget,

			/// <summary>
			/// Confirm the hold. (target UI on self)
			/// </summary>
			HoldConfirm,

			/// <summary>
			/// Confirm the overdrive.
			/// </summary>
			OverdriveConfirm,

			/// <summary>
			/// Select the slot the Nanokin will revive into
			/// </summary>
			SelectRevivalSlot
		}

		public enum MainActions { Act, Hold, Move, Flee, Skip }

		public class MemorizationState
		{
			public MainActions       action;
			public int               actCategory;
			public List<BattleSkill> skills = new List<BattleSkill>();
			public BattleSticker     sticker;
			public object            actTargetKey;
			public int               actTargetIndex;
			public int               unoccupiedSlotIndex;
		}

		public MemorizationState Memorization
		{
			get
			{
				if (!_memorizationStates.TryGetValue(fighter, out MemorizationState state))
					return _memorizationStates[fighter] = new MemorizationState();

				return state;
			}
		}

		public  bool IsTargeting                  => _state == States.ActTarget || _state == States.MoveTarget;
		private bool IsOverdriving                => _overdriveSlots > 0;
		private bool IsOverdriveSequenceFull      => _overdriveActions.Count == _overdriveSlots;
		private bool IsOverdriveSequenceLastEntry => _overdriveActions.Count == _overdriveSlots - 1;
		private bool CanHold                      => !IsOverdriving;
		private bool CanFlee                      => !Runner.arena.FleeingDisabled && Runner.io.canFlee;

		[CanBeNull]
		private List<Target> Targets => _targets.options.Count == 0 ? null : _targets.options[_targetIndex];

		[CanBeNull]
		private List<Target> UnoccupiedSlots => _unoccupiedSlots.options.Count == 0 ? null : _unoccupiedSlots.options[_unoccupiedSlotIndex];

		public override void Cleanup()
		{
			if (_triangle != null)
			{
				_triangle.Leave();
			}
		}

		public override void OnRegistered()
		{
			base.OnRegistered();

			_memorizationStates = new Dictionary<Fighter, MemorizationState>();
			_overdriveActions   = new List<TurnCommand>();
			_blinkFighter       = new BlinkVFX(0.66f, 0.5f);
			_blinkCoach         = new BlinkVFX(0.66f, 0.5f);
			_blinkStatusUI      = new BlinkVFX(0.35f, 0.5f);
			_actableStickers    = new List<BattleSticker>();
			_indicators         = new List<GameObject>();
			_virtualHomeStacks  = new Dictionary<Fighter, Stack<Slot>>();

			Load().Forget();
		}

		private async UniTask Load()
		{
			_indicatorPrefab = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Selection_Indicator");

			_triangleLoadTask = SceneLoader.GetSceneAsync("UI_Triangle").ToAsyncLazy();
			await _triangleLoadTask;

			SceneActivator.Set("UI_Triangle", true);
		}

		public override async UniTask<BattleAnim> OnGrantActionAsync()
		{
			if (!_keepOpen)
			{
				InitTurn();
				Scene          scene = await _triangleLoadTask;
				TriangleMenuUI ui    = scene.FindRootComponent<TriangleMenuUI>();

				_triangle = ui;
				_menu     = ui.menu;

				// Entrance
				// ----------------------------------------
				SlotUI.ResetOpacity();
				StatusUI.SetHighlight(fighter, true);
				_statusPanel = StatusUI.GetUI(fighter);
				if (_statusPanel != null)
					_statusPanel.vfxman.Add(_blinkStatusUI);

				// We give the entity a vfx that will make the color flash
				_elapsedFlashTime = 0;
				fighter.actor.vfx.Add(_blinkFighter);
				fighter.coach?.actor.vfx.Add(_blinkCoach);

				// Find usable stickers
				foreach (StickerInstance sticker in fighter.stickers)
				{
					BattleSticker instance = battle.GetSticker(fighter, sticker);
					if (instance != null && instance.HasUse)
					{
						_actableStickers.Add(instance);
					}
				}
			}

			CombatUI.Live.OverdriveButtonHint.gameObject.SetActive(battle.GetOverdrivePotential(fighter) > 0);

			_state = States.None;
			ChangeState(States.MainChoose);

			// Wait for action
			// ----------------------------------------
			while (!_actDone && !cts.IsCancellationRequested)
				await UniTask.NextFrame();

			// Cleanup
			// ----------------------------------------

			// TODO we can also support hold when it is used at the end of the round and no changes will happen (use battle.turns.Simulate)
			_keepOpen = _actAnim is MoveAnim action;
			if (_keepOpen)
			{
				Action nextAction = battle.turns.decoratedOrder[battle.turns.DecorationIndex + 1];
				_keepOpen = _keepOpen && nextAction.marker == ActionMarker.Action && nextAction.acter == fighter;
			}

			_keepOpen = false; // TODO too buggy atm

			if (!_keepOpen)
			{
				ChangeState(States.None);

				fighter.actor.vfx.Remove(_blinkStatusUI);
				fighter.coach?.actor.vfx.Remove(_blinkCoach);

				CombatUI.Live.OverdriveButtonHint.gameObject.SetActive(false);
				ClearOverdrive();

				camera.PlayState(ArenaCamera.States.idle);

				SlotUI.ResetOpacity();
				StatusUI.SetHighlight(fighter, false);

				fighter.actor.vfx.Remove(_blinkFighter);
				if (_statusPanel != null)
				{
					_statusPanel.vfxman.Remove(_blinkFighter);
					_statusPanel = null;
				}
			}
			else
			{
				ChangeState(States.MainChoose);
			}


			// Exit
			// ----------------------------------------
			var ret = _actAnim;

			_actAnim = null;
			_actDone = false;

			return ret;
		}

		private void InitTurn()
		{
			_skillsel               = null;
			_stickersel             = null;
			_stickerselIndex        = 0;
			_selectedTarget         = null;
			_selectedUnoccupiedSlot = null;

			_targets         = new Targeting(); // Needs to be a fresh new targeting each action since we pass it off and it could be stored elsewhere for long-term use
			_unoccupiedSlots = new Targeting();
			_actableStickers.Clear();
			_overdriveSlots     = 0;
			_overdriveTotalCost = 0;
			_overdriveActions.Clear();

			foreach (var stack in _virtualHomeStacks)
			{
				stack.Value.Clear();
			}
		}

		private void InitTargeting()
		{
			_selectedTarget = null;
			_targets.Clear();
			_targetIndex = 0;
			TargetUI.SetNone();
		}

		private void InitRevivalSlotTargeting()
		{
			_selectedUnoccupiedSlot = null;
			_unoccupiedSlots.Clear();
			_unoccupiedSlotIndex = 0;
			TargetUI.SetNone();
		}

		private void InitStickerTarget([NotNull] BattleSticker sticker)
		{
			InitTargeting();
			battle.GetStickerTargets(sticker, _targets);
		}

		private void InitSkillTarget([NotNull] BattleSkill skill)
		{
			InitTargeting();
			battle.GetSkillTargets(skill, _targets);
		}

		private void InitMoveTarget()
		{
			InitTargeting();
			battle.GetFormationTargets(fighter, _targets, true);
		}

		private void InitRevivalSlotTarget()
		{
			InitRevivalSlotTargeting();
			battle.GetUnoccupiedSlots(fighter, _unoccupiedSlots);
		}

		private void ChangeState(States nextstate)
		{
			States prevstate = _state;

			if (prevstate == nextstate)
				return;

			// LEAVE old state
			// ----------------------------------------
			LeaveState(prevstate);

			_menu.ExitPanels();

			_state = nextstate;

			// ENTER the new state.
			// ----------------------------------------
			EnterState(nextstate);

			if (!MemApply(true))
			{
				UpdateTrianglePanels();
			}

			MemStore();
			UpdateInfoPanel();
			UpdateTarget(changed: true);
			UpdateCamera();
			UpdateOverdrive();
			UpdateTurnOrder();
		}

		private void LeaveState(States prevstate)
		{
			switch (prevstate)
			{
				case States.ActTarget:
				case States.MoveTarget:
				case States.HoldConfirm:
					TargetUI.SetNone();
					break;

				case States.ActChoose:
					//BattleInfoPanelUI.Hide();
					break;
			}
		}

		private void EnterState(States nextstate)
		{
			switch (nextstate)
			{
				case States.None:
					CloseTriangle();
					break;

				case States.MainChoose:
					fighter.actor.layer = Layers.AboveUI;
					OpenTriangle();

					GameObject panelPrefab = Runner.animConfig.playerTriangleMenu.pfbAction;


					ActCategoryPanel actEntry = _menu.Add<ActCategoryPanel>(panelPrefab);
					actEntry.ValueInt = (int)MainActions.Act;
					actEntry.Icon     = Runner.animConfig.texAct;

					ActCategoryPanel moveEntry = _menu.Add<ActCategoryPanel>(panelPrefab);
					moveEntry.ValueInt = (int)MainActions.Move;
					moveEntry.Icon     = Runner.animConfig.texMove;


					ActCategoryPanel holdEntry = _menu.Add<ActCategoryPanel>(panelPrefab);
					holdEntry.ValueInt = (int)MainActions.Hold;
					holdEntry.Icon     = Runner.animConfig.texHold;

					if (CanFlee)
					{
						ActCategoryPanel fleeEntry = _menu.Add<ActCategoryPanel>(panelPrefab);
						fleeEntry.ValueInt = (int)MainActions.Flee;
						fleeEntry.Icon     = Runner.animConfig.texFlee;
					}


					// Present everything.
					_menu.FlushAdd();

					UpdateCategoryPanels();
					break;

				case States.HoldConfirm:
					TargetUI.SetReticle(fighter);
					break;

				case States.MoveTarget:
					InitMoveTarget();
					break;

				case States.SelectRevivalSlot:
					InitRevivalSlotTarget();
					break;

				case States.ActChoose:
					OpenTriangle();

					// Create horizontal switcher panel
					// ----------------------------------------
					ListPanel switcherPanel = _menu.Add(Runner.animConfig.playerTriangleMenu.pfbSkillCategory);
					switcherPanel.selectable = false;

					_hswitcher = switcherPanel.GetComponentInChildren<PanelScrollingLabel>();
					_hswitcher.titles.Clear();

					foreach (FighterInfo.SkillGroup group in fighter.info.SkillGroups)
					{
						_hswitcher.Add(group.name);
					}

					if (_actableStickers.Count > 0)
						_hswitcher.Add("Stickers");

					// Create action panels
					// ----------------------------------------
					for (var i = 0; i < ACT_PANEL_WINDOW_SPAN; i++)
						_menu.Add<ActionPanel>(Runner.animConfig.playerTriangleMenu.pfbSkill);
					_menu.FlushAdd();

					// UpdateMenuPanels();
					_menu.SelectAt(1); // index 0 is hswitcher

					// _triangleUI.menu.SelectFirst<BattleActionPanel>(p => p.);
					break;

				case States.ActTarget:
					if (_stickersel != null)
						InitStickerTarget(_stickersel);
					else
						InitSkillTarget(_skillsel);
					break;
			}
		}

		private UniTask PrepareCommand([NotNull] TurnCommand cmd)
		{
			if (cmd.GetAction(battle) is SkillAnim command)
			{
				command.skill.Prepare();
			}

			return UniTask.CompletedTask;
		}

		private UniTask UnprepareCommand([NotNull] TurnCommand cmd)
		{
			if (cmd.GetAction(battle) is SkillAnim command)
			{
				command.skill.Unprepare();
			}

			return UniTask.CompletedTask;
		}

		private UniTask ConfirmCommand([NotNull] TurnCommand cmd)
		{
			if (netplayClient != null)
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.BATTLE_CLIENT_COMMAND(cmd);
				ServerPacketCreator.End(netplayClient);
			}

			PrepareCommand(cmd);
			_actAnim = cmd.GetAction(battle);
			_actDone = true;

			return UniTask.CompletedTask;
		}

		private void UpdateTarget([CanBeNull] Target selection = null, bool changed = true)
		{
			if (_menu.SelectedPanel != null && changed)
			{
				if (_state == States.MainChoose && _menu.SelectedPanel.ValueInt == (int)MainActions.Move)
				{
					InitMoveTarget();
					TargetUI.SetPreview(_targets, _targetIndex, false);
					return;
				}
				else if (_state == States.SelectRevivalSlot)
				{
					InitRevivalSlotTarget();
					TargetUI.SetPreview(_unoccupiedSlots, _targetIndex, false);
					return;
				}
				else if (_state == States.ActChoose && _menu.SelectedPanel.Value is BattleSkill skill)
				{
					InitSkillTarget(skill);
					TargetUI.SetPreview(_targets, _targetIndex);
					return;
				}
				else if (_state == States.ActChoose && _menu.SelectedPanel.Value is BattleSticker stick)
				{
					InitStickerTarget(stick);
					TargetUI.SetPreview(_targets, _targetIndex);
					return;
				}
			}

			if (_state == States.HoldConfirm)
				return;


			// Preview target for the selected action
			// ----------------------------------------
			if (_state == States.ActTarget || _state == States.MoveTarget)
			{
				_selectedTarget = selection ?? _selectedTarget ?? Targeting.FindBest(Targets, fighter);
				MemStore();

				// ui
				// if (changed)
				// {
				// TargetUI.SetPreview(_targets, _targetIndex);
				// }

				if (_selectedTarget != null)
				{
					TargetUI.SetSelection(_targets, _targetIndex, _selectedTarget);
				}
			}
			else if (_state == States.SelectRevivalSlot)
			{
				_selectedUnoccupiedSlot = selection ?? _selectedUnoccupiedSlot ?? Targeting.FindBest(UnoccupiedSlots, fighter);
				MemStore();

				// ui
				// if (changed)
				// {
				// TargetUI.SetPreview(_targets, _targetIndex);
				// }

				if (_selectedUnoccupiedSlot != null)
				{
					TargetUI.SetSelection(_unoccupiedSlots, _unoccupiedSlotIndex, _selectedUnoccupiedSlot);
				}
			}
			else
			{
				// Auto-hide targeting
				TargetUI.SetNone();
			}

			UpdateStatusUI();
			UpdateTurnOrder();
		}

		private void UpdateCamera()
		{
			// camera
			switch (_state)
			{
				case States.MainChoose:
					camera.PlayState(ArenaCamera.States.choose_category, true);
					break;

				case States.ActChoose:
					camera.PlayState(ArenaCamera.States.choose_action);
					break;

				case States.HoldConfirm:
					camera.PlayState(ArenaCamera.States.confirm_hold);
					break;

				case States.ActTarget:
					camera.target = _selectedTarget;
					camera.PlayState(ArenaCamera.States.choose_action_target, true);
					break;

				case States.MoveTarget:
					camera.target = _selectedTarget;
					camera.PlayState(ArenaCamera.States.choose_formation_target, true);
					break;

				case States.SelectRevivalSlot:
					camera.target = _selectedTarget;
					camera.PlayState(ArenaCamera.States.choose_formation_target, true);
					break;
			}
		}

		private void UpdateStatusUI()
		{
			StatusUI.SetHighlight(false);
			StatusUI.selection.normalState = IsTargeting ? SelectUIStates.Dim : SelectUIStates.Normal;
			_blinkStatusUI.paused          = IsTargeting;

			if (IsTargeting)
			{
				StatusUI.SetHighlight(_selectedTarget, true);
			}
			else
			{
				StatusUI.SetHighlight(fighter, true);
			}

			if (_statusPanel != null)
			{
				if (IsOverdriving)
				{
					_statusPanel.odstate              = StatusPanel.ODState.Entry;
					_statusPanel.overdriveSlotNum     = AllowedOvedriveActions - _overdriveSlots;
					_statusPanel.overdriveSlotInverse = _statusPanel.overdriveSlotNum;
					_statusPanel.overdriveActionNum   = Math.Min(_overdriveActions.Count, AllowedOvedriveActions - 1);
				}
				else
				{
					_statusPanel.odstate            = StatusPanel.ODState.Normal;
					_statusPanel.overdriveSlotNum   = 0;
					_statusPanel.overdriveActionNum = 0;
				}
			}
		}

		private void UpdateTurnOrder()
		{
			TurnUI.SetHighlight(false);
			TurnUI.selection.normalState = IsTargeting ? SelectUIStates.Dim : SelectUIStates.Normal;

			if (IsTargeting)
			{
				TurnUI.SetHighlight(_selectedTarget, true);
			}
		}

	#region Memorization

		/// <summary>
		/// Write information to memorization.
		/// </summary>
		private void MemStore()
		{
			if (_menu.SelectedPanel != null)
			{
				switch (_menu.SelectedPanel.Value)
				{
					case BattleSkill skill:
						Memorization.skills.Remove(skill);
						Memorization.skills.Insert(0, skill);
						break;

					case BattleSticker sticker:
						Memorization.sticker = sticker;
						break;
				}
			}

			switch (_state)
			{
				case States.MainChoose:
					Memorization.action = (MainActions)_menu.SelectedPanel.ValueInt;
					break;

				case States.ActChoose:
					Memorization.actCategory = _hswitcher.selectedIndex;
					break;

				case States.MoveTarget:
					break;

				case States.ActTarget:
					Memorization.actTargetKey   = (object)_skillsel ?? _stickersel;
					Memorization.actTargetIndex = Targets.IndexOf(_selectedTarget);
					break;

				case States.SelectRevivalSlot:
					Memorization.unoccupiedSlotIndex = UnoccupiedSlots.IndexOf(_selectedUnoccupiedSlot);
					break;
			}
		}

		/// <summary>
		/// Apply information from memorization.
		/// </summary>
		private bool MemApply(bool stateChange = false)
		{
			if (!GameOptions.current.combat_memory_cursors) return false;

			Target target = null;

			// Set initial target selection
			// ----------------------------------------
			switch (_state)
			{
				case States.MainChoose:
					_triangle.menu.SelectByValue((int)Memorization.action);
					UpdateTrianglePanels();
					break;

				case States.ActChoose:
					_hswitcher.Select(Memorization.actCategory, stateChange);
					UpdateTrianglePanels();
					if (_hswitcher.selectedIndex == HORIZONTAL_STICKER_INDEX)
					{
						_triangle.menu.SelectByValue(Memorization.sticker);
					}
					else
					{
						for (var iskill = 0; iskill < Memorization.skills.Count; iskill++)
						{
							_triangle.menu.SelectByValue(iskill);
						}
					}

					break;

				case States.MoveTarget:
					break;

				case States.ActTarget:
				{
					object asset = (object)_skillsel?.asset ?? _stickersel?.asset;
					if (Memorization.actTargetKey == asset)
					{
						target = Targets.SafeGet(Memorization.actTargetIndex);
					}

					if (target == null)
					{
						target = Targeting.FindBest(Targets, fighter);
					}

					UpdateTarget(target);
					break;
				}

				case States.SelectRevivalSlot:
				{
					target = UnoccupiedSlots.SafeGet(Memorization.unoccupiedSlotIndex);

					if (target == null)
					{
						target = Targeting.FindBest(UnoccupiedSlots, fighter);
					}

					UpdateTarget(target);
					break;
				}
			}

			return true;
		}

	#endregion

	#region Panel UI

		public void OpenTriangle()
		{
			if (fighter != null && fighter.deathMarked && _absolvent != fighter) //refuse to open a triangle on a dead nanokin.
			{
				OnConfirm_Hold(true);
				_absolvent = fighter;
				return;
			}

			_triangle.transform.parent = null;
			var position = CastingActor.transform.position;
			_triangle.worldRaycast.SetWorldPos(position + Vector3.up * fighter.actor.height / 2f);

			// Little hack: manual ordering for triangle menu to be correct
			// ----------------------------------------
			float triangleDistance = GameCams.Live.UnityCam.transform.position.Distance(position);

			CastingActor.layer = Layers.AboveUI;
			foreach (Fighter f in battle.fighters)
			{
				if (f == fighter) continue;
				if (f.actor == null) continue;

				float dist = GameCams.Live.UnityCam.transform.position.Distance(CastingActor.transform.position);
				if (dist < triangleDistance)
				{
					f.actor.layer = Layers.AboveUI;
				}
			}

			// Finally enter the triangle.
			_triangle.Enter();
		}

		public void CloseTriangle()
		{
			_triangle.Leave();

			// Restore the layers
			// ----------------------------------------
			fighter.actor.layer = Layers.Default;
			foreach (Fighter entity in battle.fighters)
			{
				entity.actor.layer = Layers.Default;
			}
		}

		private void UpdateTrianglePanels()
		{
			if (_state == States.MainChoose)
			{
				bool overdriveFull = IsOverdriving && IsOverdriveSequenceFull;

				foreach (ListPanel panel in _menu.AllPanels)
				{
					if (panel is ActCategoryPanel actPanel)
					{
						switch ((MainActions)actPanel.ValueInt)
						{
							case MainActions.Act:
								actPanel.confirmable = !battle.HasFlag(fighter, EngineFlags.lock_act) && !overdriveFull;
								break;

							case MainActions.Hold:
								actPanel.confirmable = !IsOverdriving;
								break;

							case MainActions.Move:
								actPanel.confirmable = !battle.HasFlag(fighter, EngineFlags.unmovable) && !battle.HasFlag(fighter, EngineFlags.lock_formation) && !overdriveFull;
								break;

							case MainActions.Flee:
								actPanel.confirmable = CanFlee && !IsOverdriving;
								break;

							case MainActions.Skip:
								break;

							default: throw new ArgumentOutOfRangeException();
						}
					}
				}
			}
			else if (_state == States.ActChoose)
			{
				if (_hswitcher.selectedIndex == HORIZONTAL_STICKER_INDEX)
				{
					// STICKER LIST
					// ----------------------------------------

					// Skip the first panel which is the category switcher.
					int windowMid   = Mathf.FloorToInt(ACT_PANEL_WINDOW_SPAN / 2f);
					int windowStart = Mathf.Clamp(_stickerselIndex - windowMid, 0, _actableStickers.Count - ACT_PANEL_WINDOW_SPAN);

					if (_actableStickers.Count <= ACT_PANEL_WINDOW_SPAN)
					{
						windowStart = 0;
					}

					for (var i = 0; i < _menu.AllPanels.Count - 1; i++)
					{
						BattleSticker sticker = _actableStickers.SafeGet(windowStart + i);
						var           panel   = (ActionPanel)_menu[1 + i]; // 1 to account for the switcher panel at the top.

						panel.Value = sticker;
						panel.Text  = sticker != null ? $"{sticker.asset.DisplayName} ({sticker.instance.Charges})" : "";
						panel.Icon  = sticker != null ? null : panel.iconLocked;

						// Check that there are valid targets for
						// this skill (unselectable otherwise)
						// ----------------------------------------
						var hasTargets = false;
						if (sticker != null)
						{
							_targets.Clear();
							battle.GetStickerTargets(sticker, _targets);
							hasTargets = _targets.options.Count > 0;
						}

						try
						{
							panel.confirmable = sticker != null
							                    && sticker.HasCharges()
							                    && sticker.UsableOnFighter(fighter)
							                    && !sticker.IsPassive
							                    && hasTargets;
						}
						catch (Exception e)
						{
							DebugLogger.LogException(e);
							panel.confirmable = false;
						}
					}
				}
				else
				{
					// SKILL LIST
					// ----------------------------------------

					FighterInfo.SkillGroup skillgroup = fighter.info.SkillGroups[_hswitcher.selectedIndex];

					// Skip the first panel which is the category switcher.
					for (var i = 1; i < _menu.AllPanels.Count; i++)
					{
						ListPanel menuPanel = _menu.AllPanels[i];

						SkillAsset  asset = skillgroup.skills.SafeGet(i - 1);
						BattleSkill skill = battle.GetSkill(fighter, asset);


						bool selectable = menuPanel.selectable;
						//bool confirmable = menuPanel.confirmable;

						(bool canUse, string _) = battle.CanUse(skill, selectable, overdriveActive: IsOverdriving, overdriveTotalCost: _overdriveTotalCost);

						// Check that there are valid targets for
						// this skill (unselectable otherwise)
						// ----------------------------------------
						bool usable = skill != null && canUse;

						if (usable)
						{
							_targets.Clear();
							battle.GetSkillTargets(skill, _targets);

							if (_targets.options.Count == 0)
							{
								usable = false;
							}
							else
							{
								// Make sure every target group has options in it
								foreach (List<Target> options in _targets.options)
								{
									if (options.Count == 0)
									{
										usable = false;
										break;
									}
								}
							}
						}

						// Update the panel
						// ----------------------------------------
						var panel = (ActionPanel)_menu[i];

						string displayName;

						if (asset != null)
						{
							if (!asset.CustomDisplayName)
							{
								displayName = asset.DisplayName;
							}
							else
							{
								displayName = skill.DisplayName();
							}
						}
						else
						{
							displayName = "???";
						}

						panel.Value = skill;
						//panel.Text  = asset != null ? asset.DisplayName : "???";
						panel.Text = displayName;
						panel.Icon = asset != null ? null : panel.iconLocked;

						panel.confirmable = usable;
					}
				}
			}

			_menu.FixSelectionIndex(-1);

			if (_menu.SelectedPanel is ActionPanel actpanel)
			{
				var         skill    = actpanel.Value as SkillAsset;
				BattleSkill instance = battle.GetSkill(fighter, skill);
				BattleInfoPanelUI.Show(battle, instance);
			}
		}

		private void UpdateCategoryPanels() { }

		private void ScrollHorizontal(int offset)
		{
			MemApply();

			_hswitcher.Select(_hswitcher.selectedIndex + offset);
			_stickerselIndex = _menu.SmartSelectionIndex - 1; // Temporary, just so that we don't get scrolling oddities from the selection cursor being restored vertically without updating _stickerselIndex which is separate

			GameSFX.PlayGlobal(Runner.animConfig.adMenuScrollActCategory, this, offset > 0 ? 1 : 0.95f);

			UpdateTrianglePanels();
			UpdateInfoPanel();
			UpdateTarget(changed: true);
			MemStore();
			MemApply();
		}

		private void ScrollVertical(int offset)
		{
			if (_state == States.ActChoose && _hswitcher.selectedIndex == HORIZONTAL_STICKER_INDEX)
			{
				_stickerselIndex = Mathf.Clamp(_stickerselIndex + offset, 0, _actableStickers.Count - 1);

				// weird shit but this works for now, might not need anything fancier....... yet
				// if the window ever changes from 3, maybe rewrite this to be more flexible?

				if (_actableStickers.Count <= ACT_PANEL_WINDOW_SPAN)
				{
					_menu.SelectAt(_stickerselIndex + 1);
				}
				else
				{
					// starts at 1 because index 0 = horizontal switcher
					if (_stickerselIndex == 0) _menu.SelectAt(1);
					else if (_stickerselIndex == _actableStickers.Count - 1) _menu.SelectAt(3);
					else _menu.SelectAt(2);
				}


				UpdateTrianglePanels();
				//return;
			}
			else
			{
				_menu.SelectAt(_menu.SmartSelectionIndex + offset);
			}

			UpdateInfoPanel();
			UpdateTarget(changed: true);
			MemStore();

			GameSFX.PlayGlobal(
				offset > 0
					? _triangle.SFX_ScrollDown
					: _triangle.SFX_ScrollUp,
				this);
		}

	#endregion

	#region Overdrive

		private float OverdriveSlotChangePitch => Runner.animConfig.adOverdriveEnablePitch.Lerp((_overdriveSlots - 1) / (float)(StatConstants.MAX_OP - 1));

		/// <summary>
		/// Increase the overdrive number by 1.
		/// </summary>
		private void GrowOverdrive(bool animate = true)
		{
			if (AllowedOvedriveActions == 0)
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuError);
				return;
			}

			if (_overdriveSlots < AllowedOvedriveActions)
			{
				_overdriveSlots++;
				UpdateOverdrive();
				UpdateTrianglePanels();

				if (animate)
				{
					GameSFX.PlayGlobal(Runner.animConfig.adOverdriveGrow, this, OverdriveSlotChangePitch);
					var aura = new FX("FX/overdrive_up", battle.ActiveFighter.offset(0, -0.5f, -0.2f));
					aura.Start();
				}

				if (_overdriveSlots == 1) // Minimum 2 actions BUG: we can still overdrive with just 1 action
					GrowOverdrive(false);
			}
			else
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuError, this);
			}
		}

		private int AllowedOvedriveActions => battle.GetOverdrivePotential(fighter);

		/// <summary>
		/// Decrease the number of open overdrive slots by 1.
		/// </summary>
		private void ShrinkOverdrive(bool animate = true)
		{
			if (AllowedOvedriveActions == 0)
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuError);
				return;
			}

			if (IsOverdriving)
			{
				if (IsOverdriveSequenceLastEntry)
					PopOverdrive();

				_overdriveSlots--;
				UpdateOverdrive();
				UpdateTrianglePanels();

				if (animate)
				{
					GameSFX.PlayGlobal(Runner.animConfig.adOverdriveShrink, this, OverdriveSlotChangePitch);
					var aura = new FX("FX/overdrive_down", battle.ActiveFighter.offset(0, -0.5f, -0.2f));
					aura.Start();
				}

				if (_overdriveSlots == 1) // Minimum 2 actions
					ShrinkOverdrive();
			}
		}


		private void ConfirmOverdrive()
		{
			var action = new OverdriveCommand(fighter, _overdriveActions.ToList());

			ClearOverdrive();
			CloseTriangle();

			ConfirmCommand(action).Forget();

			GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
		}

		/// <summary>
		/// Push a command to the overdrive stack.
		/// UI is updated accordingly.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		private async UniTask PushOverdrive([NotNull] TurnCommand cmd)
		{
			if (IsOverdriveSequenceFull)
			{
				DebugLogger.LogError("Attempting to add into the overdrive list while it is already full.", LogContext.Combat, LogPriority.Low);
				return;
			}

			GameObject uiObject = await Addressables.InstantiateAsync(ADDR_OVERDRIVE_ENTRY);

			PrepareCommand(cmd);

			if (cmd.GetAction(battle) is SkillAnim skillAction)
			{
				_overdriveTotalCost += skillAction.skill.Cost();

				var   endDataDict  = skillAction.skill.EndData();
				Table newSlotTable = null;
				if (endDataDict != null && endDataDict.ContainsKey("slot"))
				{
					newSlotTable = endDataDict["slot"];
				}

				if (newSlotTable != null)
				{
					foreach (var pair in newSlotTable.Pairs)
					{
						var occupier = pair.Key;
						var occupied = pair.Value;
						if (occupier.AsUserdata(out Fighter f) && occupied.AsUserdata(out Slot s))
						{
							if (!_virtualHomeStacks.ContainsKey(f))
							{
								_virtualHomeStacks[f] = new Stack<Slot>();
							}

							_virtualHomeStacks[f].Push(s);
						}
					}
				}
			}

			if (cmd.GetAction(battle) is MoveAnim moveAction)
			{
				if (!_virtualHomeStacks.ContainsKey(fighter))
				{
					_virtualHomeStacks[fighter] = new Stack<Slot>();
				}

				_virtualHomeStacks[fighter].Push(moveAction.Slot);

				Fighter swapee = battle.GetOwner(moveAction.Slot);
				if (swapee != null)
				{
					if (!_virtualHomeStacks.ContainsKey(swapee))
					{
						_virtualHomeStacks[swapee] = new Stack<Slot>();
					}

					_virtualHomeStacks[swapee].Push(battle.GetHome(fighter));
				}
			}

			uiObject.transform.SetParent(CombatUI.Live.Root_OverdriveEntries, false);

			OverdriveCommandPanel ui = uiObject.GetComponent<OverdriveCommandPanel>();
			ui.Label.text = cmd.Text;

			if (_overdriveActions.Count > 0)
			{
				ui.ArrowLabel.gameObject.SetActive(true);
			}

			_overdriveActions.Add(cmd);

			UpdateSlotData();
			ChangeState(States.MainChoose);
			UpdateOverdrive();

			if (IsOverdriveSequenceFull)
			{
				// Became maxed
				// ----------------------------------------
				// ChangeState(States.OverdriveConfirm);
				ConfirmOverdrive();
			}
		}

		private void UpdateSlotData()
		{
			// Set fighters' virtual homes
			foreach (Fighter f in battle.fighters)
			{
				if (!_virtualHomeStacks.ContainsKey(f) || _virtualHomeStacks[f].Count == 0)
				{ }
				else
				{
					_virtualHomeStacks[f].Peek();
				}
			}

			virtualFighter = VirtualHomeUI.GetFighterCaster(fighter);
		}

		private void UpdateOverdrive()
		{
			if (_state == States.None)
			{
				_overdriveSlots = 0;
			}

			bool enabled = IsOverdriving;

			CombatUI.Live.Root_OverdriveUI.SetActive(enabled);
			CombatUI.SetOverdriveInput(IsOverdriving);

			// Toggle things off and on, fixes some positioning issues with ugui
			for (var i = 0; i < CombatUI.Live.Root_OverdriveEntries.childCount; i++)
			{
				Transform t = CombatUI.Live.Root_OverdriveEntries.GetChild(i);
				t.gameObject.SetActive(false);
			}

			// Toggle things off and on, fixes some positioning issues with ugui
			for (var i = 0; i < CombatUI.Live.Root_OverdriveEntries.childCount; i++)
			{
				RectTransform t = CombatUI.Live.Root_OverdriveEntries.GetChild(i).GetComponent<RectTransform>();
				t.gameObject.SetActive(true);
				LayoutRebuilder.ForceRebuildLayoutImmediate(t);
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(CombatUI.Live.Root_OverdriveEntries.GetComponent<RectTransform>());

			UpdateStatusUI();
			UpdateTarget(changed: true);
		}

		/// <summary>
		/// Pop a command from the overdrive stack, if there is any.
		/// UI is updated accordingly.
		/// </summary>
		private void PopOverdrive()
		{
			if (_overdriveActions.Count == 0)
				return;

			var action = _overdriveActions[_overdriveActions.Count - 1];

			if (action.GetAction(battle) is SkillAnim skillAction)
			{
				_overdriveTotalCost = Mathf.Max(_overdriveTotalCost - skillAction.skill.Cost(), 0);

				var   endDataDict  = skillAction.skill.EndData();
				Table newSlotTable = null;
				if (endDataDict != null && endDataDict.ContainsKey("slot"))
				{
					newSlotTable = endDataDict["slot"];
				}

				if (newSlotTable != null)
				{
					foreach (var pair in newSlotTable.Pairs)
					{
						var occupier = pair.Key;
						var occupied = pair.Value;
						if (occupier.AsUserdata(out Fighter f) && occupied.AsUserdata(out Slot s))
						{
							if (s != _virtualHomeStacks[f].Pop())
							{
								DebugLogger.LogError("Slot inconsistencies detected in overdrive data.", LogContext.Combat);
							}
						}
					}
				}
			}

			if (action.GetAction(battle) is MoveAnim moveAction)
			{
				if (moveAction.Slot != _virtualHomeStacks[fighter].Pop())
				{
					DebugLogger.LogError("Slot inconsistencies detected in overdrive data.", LogContext.Combat, LogPriority.Low);
				}

				var swapee = battle.GetOwner(moveAction.Slot);
				if (swapee != null)
				{
					_virtualHomeStacks[swapee].Pop();
				}
			}

			UnprepareCommand(action);
			_overdriveActions.RemoveAt(_overdriveActions.Count - 1);


			// Destroy its panel entry
			GameObject uiEntry = CombatUI.Live.Root_OverdriveEntries.GetChild(CombatUI.Live.Root_OverdriveEntries.childCount - 1).gameObject;
			Object.Destroy(uiEntry);

			if (_overdriveActions.Count == 0)
			{
				CombatUI.Live.Root_OverdriveUI.SetActive(false);
			}

			UpdateCategoryPanels();
			UpdateSlotData();
			UpdateOverdrive();

			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
		}

		public void ClearOverdrivesFromWin()
		{
			ClearOverdrive();
		}

		private void ClearOverdrive()
		{
			_overdriveActions.Clear();
			_overdriveTotalCost = 0;

			Transform entries = CombatUI.Live.Root_OverdriveEntries;
			foreach (Transform entry in entries)
			{
				Object.Destroy(entry.gameObject);
			}

			UpdateOverdrive();
		}

	#endregion


		private bool _isMoving;

		public override void Update()
		{
			// Buttons
			// ----------------------------------------
			Keyboard keyb = Keyboard.current;

			bool btnConfirm       = GameInputs.confirm.IsPressed;
			bool btnBack          = GameInputs.cancel.IsPressed;
			bool btnOverdriveUp   = GameInputs.overdriveUp.IsPressed;
			bool btnOverdriveDown = GameInputs.overdriveDown.IsPressed;

			//bool btnLeft  = GameInputs.move.left.IsPressed;
			//bool btnRight = GameInputs.move.right.IsPressed;
			//bool btnUp    = GameInputs.move.up.IsPressed;
			//bool btnDown  = GameInputs.move.down.IsPressed;

			// Basic inputs
			// ----------------------------------------

			switch (_state)
			{
				case States.MainChoose:
					DoTriangleNav();
					DoInfoHold();
					break;

				case States.ActChoose:
					DoTriangleNav();
					DoHorizontalNav();
					DoInfoHold();
					break;

				case States.ActTarget:
				case States.MoveTarget:
				case States.SelectRevivalSlot:
				{
					if (_showingInfo)
					{
						foreach (Fighter e in battle.fighters)
						{
							HealthbarUI.EnableFlag(e, HEALTHBAR_ENABLE_FLAG_NAME, false);
						}

						_showingInfo = false;
					}

					DoTargetNav();
					break;
				}
			}

			if (btnOverdriveUp)
			{
				GrowOverdrive();
			}
			else if (btnOverdriveDown)
			{
				ShrinkOverdrive();
			}

			if (fighter != null && fighter.deathMarked)
				CloseTriangle();

			// State Transitions
			// ----------------------------------------
			// ReSharper disable RedundantCast

		#region State Transitions

			Func<TurnCommand, UniTask> confirmCommand = IsOverdriving
				? (Func<TurnCommand, UniTask>)PushOverdrive
				: (Func<TurnCommand, UniTask>)ConfirmCommand;

			switch (_state)
			{
				case States.MainChoose when btnBack:
					OnBack_MainChoose();
					break;

				case States.MainChoose when btnConfirm:
					OnConfirm_MainChoose();
					break;

				case States.HoldConfirm when btnConfirm:
					OnConfirm_Hold();
					break;

				case States.HoldConfirm when btnBack:
					OnBack_Hold();
					break;

				case States.MoveTarget when btnConfirm:
					OnConfirm_MoveTarget(confirmCommand);
					break;

				case States.MoveTarget when btnBack:
					OnBackMove();
					break;

				case States.ActChoose when btnBack:
					OnBack_ActChoose();
					break;

				case States.ActChoose when btnConfirm:
					OnConfirm_ActChoose();
					break;

				case States.ActTarget when btnBack:
					OnBack_ActTarget();
					break;

				case States.ActTarget when btnConfirm:
					OnConfirm_ActTarget(confirmCommand);
					break;

				case States.SelectRevivalSlot when btnBack:
					OnBack_SelectRevivalSlot();
					break;

				case States.SelectRevivalSlot when btnConfirm:
					OnConfirm_SelectRevivalSlot(confirmCommand);
					break;

				case States.OverdriveConfirm when btnBack:
					OnBack_OverdriveConfirm();
					break;

				case States.OverdriveConfirm when btnConfirm:
					OnConfirm_OverdriveConfirm();
					break;
			}

		#endregion


		#region Shortcuts

			if (isActiveTurn)
			{
				if (GameInputs.cancel.IsHeld(0))
				{
					if (AnyDirIsPressed && !_isMoving)
					{
						ChangeState(States.MoveTarget);
						DoTargetNav();

						_isMoving = true;
						CloseTriangle();
					}

					if (!_isMoving && GameInputs.cancel.IsHeld(0.5f, resets: true))
					{
						OnConfirm_Skip(true);
					}
				}
				else
				{
					if (_isMoving)
					{
						OnConfirm_MoveTarget(confirmCommand);
					}

					_isMoving = false;
				}

				if (GameInputs.hold.IsDown)
				{
#if UNITY_EDITOR
					// Faster for debugging
					if (GameInputs.cancel.IsDown)
					{
						OnConfirm_Skip();
					}
					else
					{
						OnConfirm_Hold();
					}
#else
					OnConfirm_Hold();
#endif
				}

#if UNITY_EDITOR

				Vector2Int dir = Vector2Int.zero;

				if (GameInputs.IsShortcutPressed(Key.UpArrow, Key.LeftCtrl)) dir    += Vector2Int.up;
				if (GameInputs.IsShortcutPressed(Key.DownArrow, Key.LeftCtrl)) dir  += Vector2Int.down;
				if (GameInputs.IsShortcutPressed(Key.LeftArrow, Key.LeftCtrl)) dir  += Vector2Int.left;
				if (GameInputs.IsShortcutPressed(Key.RightArrow, Key.LeftCtrl)) dir += Vector2Int.right;

				if (dir.magnitude > Mathf.Epsilon)
				{
					Slot       curSlot    = battle.GetHome(fighter);
					Vector2Int newSlotPos = curSlot.coord + dir;
					Slot       newSlot    = battle.GetSlot(newSlotPos);

					if (newSlot == null)
						return;

					bool isSameTeam = battle.GetTeam(newSlot) == battle.GetTeam(fighter);
					if (!isSameTeam)
						return;

					DebugLogger.Log($"Moving to slot at ({newSlotPos}) on the grid", LogContext.Combat, LogPriority.Low);
				}
#endif
			}


#if UNITY_EDITOR
			if (GameInputs.IsShortcutPressed(Key.H))
				OnConfirm_Hold();

			if (GameInputs.IsShortcutPressed(Key.W, Key.LeftCtrl)) // CTRL-W win
			{
				CloseTriangle();
				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
				ConfirmCommand(new WinCommand());
			}
			else if (GameInputs.IsShortcutPressed(Key.L, Key.LeftCtrl)) // CTRL-L lose
			{
				CloseTriangle();
				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
				ConfirmCommand(new LoseCommand());
			}
#endif

		#endregion

			// VFX update
			// ----------------------------------------
			_blinkFighter.paused   = _state == States.None;
			_blinkCoach.elapsed    = _blinkFighter.elapsed;
			_blinkStatusUI.elapsed = _blinkFighter.elapsed;

			if (_triangle != null)
			{
				// Update triangle menu position. EDIT: this causes jittering since the VisualCenter is not guaranteed to remain. Even an average of the last frames doesn't work too well...
				// TriangleMenu.worldRaycast.worldPosition = TriangleMenu.worldRaycast.worldPosition.WeightedAverage(ActingEntity.VisualCenter, 7f);
			}

#if UNITY_EDITOR

		#region Editor Features

			if (GameInputs.IsShortcutPressed(Key.Home))
			{
				// Add a stub panel
				ActCategoryPanel panel = _menu.Add<ActCategoryPanel>(Runner.animConfig.playerTriangleMenu.pfbAction);
				panel.ValueInt = (int)MainActions.Act;
				panel.Icon     = Runner.animConfig.texAct;
				panel.Text     = "Panel";

				_menu.FlushAdd();
			}

			if (GameInputs.IsShortcutPressed(Key.LeftBracket))
			{
				// Decrease by 10% everyone's HP
				foreach (Fighter e in battle.fighters)
				{
					float maxhp = battle.GetMaxPoints(e).hp;
					battle.AddPoints(e, new Pointf(maxhp * -0.10f));
				}
			}

			if (GameInputs.IsShortcutPressed(Key.RightBracket))
			{
				// Heal everyone to max (all points)
				foreach (Fighter e in battle.fighters)
				{
					battle.AddPoints(e, new Pointf(int.MaxValue, int.MaxValue, int.MaxValue));
				}
			}

		#endregion

#endif
		}

		private void DoInfoHold()
		{
			bool btnShowInfoHeld     = GameInputs.showInfo.IsDown;
			bool btnShowInfoReleased = GameInputs.showInfo.IsReleased;

			if (btnShowInfoHeld)
			{
				if (!_showingInfo)
				{
					_showingInfo = true;

					foreach (Fighter e in battle.fighters)
					{
						if (e != null && e.team != team && !e.deathMarked)
						{
							HealthbarUI.EnableFlag(e, HEALTHBAR_ENABLE_FLAG_NAME, true);
						}
					}
				}
			}
			else if (_showingInfo)
			{
				foreach (Fighter e in battle.fighters)
				{
					if (e != null && e.team != team && !e.deathMarked)
					{
						HealthbarUI.EnableFlag(e, HEALTHBAR_ENABLE_FLAG_NAME, false);
					}
				}

				_showingInfo = false;
			}
		}

	#region State Transitions

		private void OnBack_MainChoose()
		{
			if (_overdriveActions.Count > 0)
				PopOverdrive();
		}

		private void OnConfirm_MainChoose()
		{
			if (_menu.SelectedPanel != null && _menu.SelectedPanel.confirmable)
			{
				var panel = (ActCategoryPanel)_menu.SelectedPanel;
				_menu.ExitPanels();

				switch ((MainActions)panel.ValueInt)
				{
					case MainActions.Act:
						ChangeState(States.ActChoose);
						GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirm, this);
						break;

					case MainActions.Hold:
						CloseTriangle();
						ChangeState(States.HoldConfirm);
						GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirm, this);
						break;

					case MainActions.Move:
						CloseTriangle();
						ChangeState(States.MoveTarget);
						GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirm, this);
						break;

					case MainActions.Flee:
						CloseTriangle();
						GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
						ConfirmCommand(new FleeCommand());
						break;


					case MainActions.Skip:
						CloseTriangle();
						GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
						ConfirmCommand(new SkipCommand());
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuError, this);
			}
		}

		private void OnConfirm_MoveTarget(Func<TurnCommand, UniTask> confirmCommand)
		{
			if (_selectedTarget == null) return;
			if (_selectedTarget.Slot == fighter.HomeTargeting)
			{
				ChangeState(States.MainChoose);
				return;
			}

			TurnCommand moveCmd = new MoveCommand(fighter, _selectedTarget.Slot);

			confirmCommand(moveCmd);
			TargetUI.Confirm();
			TargetUI.SetNone();
			GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
		}

		private void OnConfirm_OverdriveConfirm() { ConfirmOverdrive(); }

		private void OnBack_OverdriveConfirm()
		{
			PopOverdrive();
			ChangeState(States.MainChoose);
		}

		private void OnConfirm_ActTarget(Func<TurnCommand, UniTask> confirmCommand)
		{
			// Check for the selection of dead allies (signifies revival)

			// BUG there is nothing that specifies if this is a healing skill, and this whole healing state is unnecessary really. Healing skills should be multi-target
			// var revivalSlots    = _selectedTarget.slots.FindAll(x => x.owner?.deathMarked == true);
			// var revivalFighters = _selectedTarget.fighters.FindAll(x => x.deathMarked);
			//
			// if ((revivalSlots.Count > 0) || (revivalFighters.Count > 0))
			// {
			// 	ChangeState(States.SelectRevivalSlot);
			// 	return;
			// }

			_targets.AddPick(_selectedTarget);
			_targetIndex++;

			BattleInfoPanelUI.Hide();

			if (_targetIndex == _targets.options.Count)
			{
				TurnCommand cmd;

				if (_skillsel != null)
					cmd = new SkillCommand(fighter, _skillsel, _targets.Clone());
				else if (_stickersel != null)
					cmd = new StickerCommand(fighter, _stickersel, _targets.Clone());
				else
					throw new NotImplementedException();

				TargetUI.Confirm();
				TargetUI.SetNone(true);

				for (int i = 0; i < _indicators.Count; i++)
				{
					GameObject indicator = _indicators[i];
					Object.Destroy(indicator);
				}

				_indicators.Clear();

				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);

				confirmCommand(cmd);
			}
			else
			{
				bool alreadyInSelection = _targets.slots.Where(x => x.coord == _selectedTarget.Slot.coord).ToList().Count > 1;

				if (!alreadyInSelection)
				{
					GameObject indicator = Object.Instantiate(_indicatorPrefab);

					Vector3 selectionCenter = _selectedTarget.center;

					Vector3 indicatorPosition = indicator.transform.position;
					indicatorPosition.x          = selectionCenter.x;
					indicatorPosition.y          = 3;
					indicatorPosition.z          = selectionCenter.z;
					indicator.transform.position = indicatorPosition;

					TargetUI.SetSelected(_selectedTarget.Slot);
					_selectedTarget.Slot.persistentHighlight = true;
					//SlotUI.ChangeMaterial(_selectedTarget.Slot, true);

					TextMeshPro TMP_Number = indicator.GetComponentInChildren<TextMeshPro>();

					TMP_Number.text = _targetIndex.ToString();

					_indicators.Add(indicator);
				}
				else
				{
					GameObject indicator = _indicators[_indicators.Count - 1];

					TextMeshPro TMP_Number = indicator.GetComponentInChildren<TextMeshPro>();

					TMP_Number.text = string.Format("{0}  {1}", TMP_Number.text, _targetIndex.ToString());
				}
			}
		}

		private void OnBack_ActTarget()
		{
			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
			TargetUI.SetNone(true);

			for (int i = 0; i < _indicators.Count; i++)
			{
				GameObject indicator = _indicators[i];
				Object.Destroy(indicator);
			}

			_indicators.Clear();
			_targets.slots.Clear();

			ChangeState(States.ActChoose);
		}

		private void OnConfirm_ActChoose()
		{
			if (_menu.SelectedPanel.confirmable)
			{
				var actMenuPanel = (ActionPanel)_menu.SelectedPanel;

				_skillsel   = actMenuPanel.Value as BattleSkill;
				_stickersel = actMenuPanel.Value as BattleSticker;

				_menu.ExitPanels(true);
				CloseTriangle();

				ChangeState(States.ActTarget);
				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirm, this);
			}
			else
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuError, this);

				var actMenuPanel = (ActionPanel)_menu.SelectedPanel;

				_skillsel = actMenuPanel.Value as BattleSkill;

				if (_skillsel != null)
				{
					(bool _, string error) = _skillsel.battle.CanUse(_skillsel, actMenuPanel.selectable, actMenuPanel.confirmable, overdriveActive: IsOverdriving, overdriveTotalCost: _overdriveTotalCost);
					CombatNotifyUI.DoGeneralNotificationPopup(error).Forget();
				}
			}
		}

		private void OnBack_ActChoose()
		{
			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
			ChangeState(States.MainChoose);
		}

		private void OnBackMove()
		{
			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
			TargetUI.SetNone();
			ChangeState(States.MainChoose);
		}

		private void OnBack_Hold()
		{
			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
			TargetUI.SetNone();
			ChangeState(States.MainChoose);
		}

		private void OnConfirm_Hold(bool withSFX = false)
		{
			TargetUI.Confirm();
			TargetUI.SetNone();

			if (withSFX)
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
				ConfirmCommand(new HoldCommand(fighter));
			}
			else
			{
				ConfirmCommand(new HoldCommand(fighter));
			}
		}

		private void OnConfirm_Skip(bool withSFX = false)
		{
			TargetUI.Confirm();
			TargetUI.SetNone();

			if (withSFX)
			{
				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);
				ConfirmCommand(new SkipCommand());
			}
			else
			{
				ConfirmCommand(new SkipCommand());
			}
		}

		private void OnBack_SelectRevivalSlot()
		{
			GameSFX.PlayGlobal(Runner.animConfig.adMenuCancel, this);
			TargetUI.SetNone();

			for (int i = 0; i < _indicators.Count; i++)
			{
				GameObject indicator = _indicators[i];
				Object.Destroy(indicator);
			}

			_indicators.Clear();
			_unoccupiedSlots.slots.Clear();

			ChangeState(States.ActTarget);
		}

		private void OnConfirm_SelectRevivalSlot(Func<TurnCommand, UniTask> confirmCommand)
		{
			// TODO we need to change this, this will not work for AI
			_selectedTarget.Fighter.home = _selectedUnoccupiedSlot.Slot;

			_targets.AddPick(_selectedTarget);
			_targetIndex++;

			BattleInfoPanelUI.Hide();

			if (_targetIndex == _targets.options.Count)
			{
				TurnCommand cmd;

				if (_skillsel != null)
					cmd = new SkillCommand(fighter, _skillsel, _targets.Clone());
				else if (_stickersel != null)
					cmd = new StickerCommand(fighter, _stickersel, _targets.Clone());
				else
					throw new NotImplementedException();

				TargetUI.Confirm();
				TargetUI.SetNone(true);

				for (int i = 0; i < _indicators.Count; i++)
				{
					GameObject indicator = _indicators[i];
					Object.Destroy(indicator);
				}

				_indicators.Clear();

				GameSFX.PlayGlobal(Runner.animConfig.adMenuConfirmFinal, this);

				confirmCommand(cmd);
			}
			else
			{
				bool alreadyInSelection = _targets.slots.Where(x => x.coord == _selectedTarget.Slot.coord).ToList().Count > 1;

				if (!alreadyInSelection)
				{
					GameObject indicator = Object.Instantiate(_indicatorPrefab);

					Vector3 selectionCenter = _selectedTarget.center;

					Vector3 indicatorPosition = indicator.transform.position;
					indicatorPosition.x          = selectionCenter.x;
					indicatorPosition.y          = 3;
					indicatorPosition.z          = selectionCenter.z;
					indicator.transform.position = indicatorPosition;

					TargetUI.SetSelected(_selectedTarget.Slot);
					_selectedTarget.Slot.persistentHighlight = true;
					//SlotUI.ChangeMaterial(_selectedTarget.Slot, true);

					TextMeshPro TMP_Number = indicator.GetComponentInChildren<TextMeshPro>();

					TMP_Number.text = _targetIndex.ToString();

					_indicators.Add(indicator);
				}
				else
				{
					GameObject indicator = _indicators[_indicators.Count - 1];

					TextMeshPro TMP_Number = indicator.GetComponentInChildren<TextMeshPro>();

					TMP_Number.text = string.Format("{0}  {1}", TMP_Number.text, _targetIndex.ToString());
				}
			}
		}

	#endregion

	#region Navigation

		private void DoHorizontalNav()
		{
			// Skill category
			if (LeftIsPressed) ScrollHorizontal(-1);
			if (RightIsPressed) ScrollHorizontal(1);
			//if (GameInputs.move.left.IsHeld(0.3f) || GameInputs.menuNavigate.left.IsHeld(0.3f)) ScrollHorizontal(-1);
			//if (GameInputs.move.right.IsHeld(0.3f) || GameInputs.menuNavigate.right.IsHeld(0.3f)) ScrollHorizontal(1);
		}

		private void DoTriangleNav()
		{
			if (UpIsPressed) ScrollVertical(-1);
			if (DownIsPressed) ScrollVertical(1);
			//if (GameInputs.move.up.IsHeld(0.3f) || GameInputs.menuNavigate.up.IsHeld(0.3f)) ScrollVertical(-1);
			//if (GameInputs.move.down.IsHeld(0.3f) || GameInputs.menuNavigate.down.IsHeld(0.3f)) ScrollVertical(1);
		}

		private bool DoTargetNav()
		{
			bool btnLeft  = LeftIsPressed;
			bool btnRight = RightIsPressed;
			bool btnUp    = UpIsPressed;
			bool btnDown  = DownIsPressed;

			// Target picking
			Camera cam = GameCams.Live.UnityCam;

			Vector3? dir = default;

			if (btnUp) dir         = cam.transform.forward.Change3(y: 0).normalized;
			else if (btnDown) dir  = -cam.transform.forward.Change3(y: 0).normalized;
			else if (btnLeft) dir  = -cam.transform.right.Change3(y: 0).normalized;
			else if (btnRight) dir = cam.transform.right.Change3(y: 0).normalized;

			if (dir.HasValue)
			{
				if (_state == States.SelectRevivalSlot)
				{
					Target target = Targeting.FindTowards(_selectedUnoccupiedSlot, dir.Value, UnoccupiedSlots);
					if (target != _selectedUnoccupiedSlot)
					{
						UpdateTarget(target, false);
						GameSFX.PlayGlobal(Runner.animConfig.adMenuScroll, this);
					}
				}
				else
				{
					Target target = Targeting.FindTowards(_selectedTarget, dir.Value, Targets);
					if (target != _selectedTarget)
					{
						UpdateTarget(target, false);
						GameSFX.PlayGlobal(Runner.animConfig.adMenuScroll, this);
					}
				}
			}

			return btnLeft || btnRight || btnUp || btnDown;
		}

		private static bool DownIsPressed   => GameInputs.move.down.IsPressed || GameInputs.menuNavigate.down.IsPressed;
		private static bool UpIsPressed     => GameInputs.move.up.IsPressed || GameInputs.menuNavigate.up.IsPressed;
		private static bool RightIsPressed  => GameInputs.move.right.IsPressed || GameInputs.menuNavigate.right.IsPressed;
		private static bool LeftIsPressed   => GameInputs.move.left.IsPressed || GameInputs.menuNavigate.left.IsPressed;
		private static bool AnyDirIsPressed => DownIsPressed || UpIsPressed || RightIsPressed || LeftIsPressed;

	#endregion


		private void UpdateInfoPanel()
		{
			if (_menu.SelectedPanel == null) return;

			// if (_state == States.ActChoose || _state == States.ActTarget)
			switch (_menu.SelectedPanel.Value)
			{
				case BattleSkill skill:
					BattleInfoPanelUI.Show(battle, skill);
					break;

				case BattleSticker sticker:
					BattleInfoPanelUI.Show(battle, sticker);
					break;

				default:
					BattleInfoPanelUI.Hide();
					break;
			}
		}
	}
}
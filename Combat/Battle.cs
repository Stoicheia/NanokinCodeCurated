using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using Combat.Data.VFXs;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine.Assertions;
using Pathfinding.Util;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using SaveFiles.Elements.Inventory.Items.Scripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat
{
	/// <summary>
	/// The full state of battle.
	///
	/// Useful info for documentation later:
	///
	/// States:
	///		- KillState (expire and consume) --> LoseState(statee, state) --> deregistered from combat on the last statee
	///
	/// Captures:
	///		- Capture all operations that lead to removing/adding a resource so they can be done later.
	///		- Prevents very rare corner cases
	///
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Battle : ILogger
	{
		public BattleRunner runner;
		public int          procCount;
		public bool         silent;
		public bool         cameraMotions;

		public readonly List<Fighter>     fighters    = new List<Fighter>(8); // Fighters removed from combat (e.g. on death) are not in this list
		public readonly List<Fighter>     allFighters = new List<Fighter>(8); // All fighters, even those removed from combat, are in this list
		public readonly List<BattleSkill> skills      = new List<BattleSkill>(24);
		public readonly List<Death>       deaths      = new List<Death>(8);
		public readonly List<Team>        teams       = new List<Team>(2);
		public readonly List<State>       states      = new List<State>(64);
		public readonly List<Trigger>     triggers    = new List<Trigger>(64);
		public readonly List<Slot>        slots       = new List<Slot>(18);
		public readonly List<SlotGrid>    slotGroups  = new List<SlotGrid>(2);
		public readonly TurnSystem        turns       = new TurnSystem(48);
		public readonly AnimSM            globalASM;

		private readonly Dictionary<Vector2Int, Slot> _slotmap = new Dictionary<Vector2Int, Slot>();
		// private readonly Dictionary<(Fighter user, SkillAsset skill), BattleSkill> _skillInstances   = new Dictionary<(Fighter user, SkillAsset skill), BattleSkill>();
		private readonly Dictionary<(Fighter, StickerInstance), BattleSticker> _stickerInstances = new Dictionary<(Fighter, StickerInstance), BattleSticker>();

		// Internal Functions
		// ----------------------------------------
		private int _nextFighterID;

		private Stack<Capture>    _captures = new Stack<Capture>();
		private BasePool<Capture> _capturePool;

		#region Work Data

		private static readonly List<State>           _tmpStates         = new List<State>();
		private static readonly List<Trigger>         _tmpTriggers       = new List<Trigger>();
		private static readonly List<StickerInstance> EMPTY_STICKER_LIST = new List<StickerInstance>();
		private static readonly List<State>           EMPTY_STATE_LIST   = new List<State>();

		#endregion

		private bool IsCapturing => _captures.Count > 0;

		public Capture CurrentCapture => _captures.Peek();

		public delegate void StateEvent(params object[] args);

		public StateEvent OnSignalEmitted;

		public Battle([CanBeNull] BattleRunner runner = null)
		{
			this.runner = runner;

			globalASM     = new AnimSM();
			cameraMotions = animated && GameOptions.current.combat_camera_motions;

			_capturePool = new GenericPool<Capture> { initSize = 8, maxSize = -1 };

			// Post-process rounds with states' action operations
			turns.RoundCreated += delegate(int headIndex)
			{
				foreach (State state in states)
				{
					foreach (IStatee statee in state.statees)
					foreach (TurnFunc func in state.turnops)
					{
						var op = new TurnSM(turns)
						{
							ptr = headIndex,
							me  = (ITurnActer)statee
						};

						turns.Execute(func, op);
					}
				}
			};
		}

		// Shortcuts and Utilities
		// ----------------------------------------
		public bool animated => runner?.animated ?? false;

		public Vector3 center   => arena.transform.position;
		public Vector3 pos      => arena.transform.position;
		public Vector3 position => arena.transform.position;

		[CanBeNull]
		public Arena arena => runner.arena;

		[CanBeNull]
		public Team PlayerTeam => teams.FirstOrDefault(t => t.isPlayer);

		[NotNull]
		public Fighter ActiveFighter => (Fighter)ActiveActer;

		[NotNull]
		public ITurnActer ActiveActer => ActiveAction.acter; // We never stop on a marker action, so there's always an event

		public Action ActiveAction => turns.action;

		public int RoundID => turns.round;

		#region Logging

		[NotNull]
		public string LogID => "Battle";

		public bool LogSilenced => silent || (!runner?.logState ?? false);

		#endregion

		#region Events

		public event FighterDelegate     FighterAdded;
		public event FighterDelegate     FighterRemoved;
		public event Action<Trigger>     TriggerAdded;
		public event Action<Trigger>     TriggerRemoved;
		public event Action<Slot>        SlotAdded;
		public event Action<Team>        TeamAdded;
		public event ComboChangeDelegate ComboChanged;
		public event ProcDelegate        ProcApplied;
		public event ProcDelegate        ProcMissed;
		public event PointChangeDelegate FighterPointsChanged;
		public event StateDelegate       StateAdded;
		public event StateDelegate       StateRefreshed;
		public event StateDelegate       StateEffect;
		public event StateDelegate       StateExpired;
		public event StateDelegate       StateConsumed;
		public event TriggerDelegate     TriggerFired;
		public event TriggerDelegate     TriggerFiredEffective;

		public event TurnOperatorDelegate  TurnOperationApplied;
		public event TurnOperatorsDelegate TurnOperationsApplied;

		public delegate void ComboChangeDelegate(object fighter, float newvalue);

		public delegate void PointChangeDelegate(Fighter fighter, PointChange chg);

		public delegate void ProcDelegate(ProcContext proc);

		public delegate void StateDelegate(IStatee fighter, State state);

		public delegate void TriggerDelegate(Trigger trigger);

		public delegate void FighterDelegate(Fighter fter);

		public delegate void TurnOperatorDelegate(TurnOperationType type, TurnSM op);

		public delegate void TurnOperatorsDelegate(TurnOperationType type, List<TurnSM> ops);

		/// <summary>
		/// Special action operation events emitted by the battle state.
		/// </summary>
		public enum TurnOperationType
		{
			SpeedChange
		}

		#endregion

		#region Setup

		[CanBeNull]
		public SlotGrid AddSlots(SlotLayout comp)
		{
			if (comp == null) return null;

			// Check if already added
			foreach (SlotGrid g in slotGroups)
				if (g.component == comp)
					return g;

			return AddSlots(new SlotGrid(comp)
			{
				battle = this
			});
		}

		[NotNull]
		public SlotGrid AddSlots([NotNull] SlotGrid grid)
		{
			foreach (Slot slot in grid.all)
			{
				if (animated && slot.actor != null)
				{
					SceneManager.MoveGameObjectToScene(slot.actor.gameObject, runner.Scene);
				}

				AddSlot(slot);

				slot.grid = grid;
			}

			slotGroups.Add(grid);
			return grid;
		}

		[NotNull]
		public Slot AddSlot(Vector2Int coord) =>
			AddSlot(new Slot
			{
				coord = coord
			});

		[NotNull]
		public Slot AddSlot([NotNull] Slot slot)
		{
			slot.battle = this;

			_slotmap.Add(slot.coord, slot);
			slots.Add(slot);
			SlotAdded?.Invoke(slot);

			if (animated)
			{
				// These values match the UI as of 2022-10-26
				const float SLOT_SIZE  = 2;
				const float SLOT_DEPTH = 0.2f;

				var go = new GameObject($"Battle Slot ({slot.coord.x}, {slot.coord.y})")
				{
					layer = Layers.Actor
				};

				// A collider for animation
				BoxCollider box = go.AddComponent<BoxCollider>();
				box.size = new Vector3(SLOT_SIZE, SLOT_DEPTH, SLOT_SIZE);

				BattleActor actor = go.AddComponent<BattleActor>();
				actor.facing             = slot.facing;
				actor.radius             = SLOT_SIZE;
				actor.transform.position = slot.position;
				actor.transform.rotation = Quaternion.LookRotation(slot.facing, Vector3.up);
				actor.position           = slot.position;

				slot.actor = actor;

				GameObject v = slot.actor.gameObject;
				runner.objects.Add(v);
				SceneManager.MoveGameObjectToScene(v.gameObject, runner.Scene);
			}

			return slot;
		}

		public void AddSticker([NotNull] Fighter fighter, StickerInstance sticker)
		{
			fighter.stickers.Add(sticker);
		}

		/// <summary>
		/// Give a sticker to a fighter.
		/// For a consumable, charges are maxed out by default.
		/// </summary>
		public void AddSticker([NotNull] Fighter fighter, StickerAsset asset, int charges = -1)
		{
			fighter.stickers.Add(new StickerInstance
			{
				Asset   = asset,
				Charges = charges == -1 ? asset.Charges : charges
			});
		}

		public Team AddTeam(
			[CanBeNull] BattleBrain brain      = null,
			[CanBeNull] SlotLayout  slotlayout = null,
			SlotGrid                slotgroup  = null,
			bool                    auslots    = true
		)
		{
			if (slotgroup == null && slotlayout)
			{
				slotgroup = AddSlots(slotlayout);
			}

			if (slotgroup == null && auslots)
			{
				foreach (SlotGrid sg in slotGroups)
				{
					if (sg.team == null)
					{
						slotgroup = sg;
						break;
					}
				}
			}

			var team = new Team
			{
				brain = brain,
				slots = slotgroup
			};

			return AddTeam(team);
		}

		[NotNull]
		public Team AddTeam([NotNull] Team team)
		{
			team.battle = this;

			BattleBrain brain = team.brain;
			if (brain != null)
			{
				brain.team = team;
				RegisterBrain(brain);
			}

			SlotGrid teamslots = team.slots;

			if (teamslots != null)
			{
				teamslots.team = team;

				if (!slotGroups.Contains(teamslots))
				{
					AddSlots(teamslots);
				}

				foreach (Slot slot in teamslots.all)
				{
					slot.team = team;
				}
			}

			teams.Add(team);
			TeamAdded?.Invoke(team);

			return team;
		}

		[CanBeNull]
		public Team GetTeam([CanBeNull] object member)
		{
			if (member == null) return null;
			if (member is Team team) return team;
			if (member is Slot slot) return slot.team;
			if (member is Fighter fighter) return fighter.team;
			if (member is SlotLayout)
			{
				for (var i = 0; i < teams.Count; i++)
				{
					Team t = teams[i];
					if (t.slots.component == member)
						return t;
				}
			}

			return null;
		}

		/// <summary>
		/// Get all teams opposing the provided team, and fill them into the provided list. (Clears the list.)
		/// </summary>
		/// <param name="myTeam"></param>
		/// <param name="list"></param>
		[NotNull]
		public List<Team> GetEnemyTeams(Team myTeam, List<Team> list = null)
		{
			list = list ?? new List<Team>();
			list.Clear();

			for (int i = 0; i < teams.Count; i++)
			{
				if (teams[i] != myTeam)
					list.Add(teams[i]);
			}

			return list;
		}

		public bool IsAlly(object self, object other) => GetTeam(self) != GetTeam(other);

		public bool IsEnemy(object self, object other) => GetTeam(self) != GetTeam(other);

		[NotNull]
		// public Fighter SpawnObject(
		// 	[NotNull] GameObject toSpawn,
		// 	[NotNull] Slot       slot,
		// 	bool                 exists,
		// 	bool                 hasHome,
		// 	[CanBeNull] Table    props)
		// {
		// 	// GameObject spawned = !exists
		// 	// 	? Object.Instantiate(toSpawn)
		// 	// 	: toSpawn;
		// 	//
		// 	// FxInfo fxInfo = spawned.GetComponent<FxInfo>();
		// 	// if (fxInfo != null)
		// 	// {
		// 	// 	fxInfo.enabled = false;
		// 	// }
		// 	//
		// 	// MotionBehaviour motionBehaviour = spawned.GetComponent<MotionBehaviour>();
		// 	// if (motionBehaviour != null)
		// 	// {
		// 	// 	motionBehaviour.enabled = false;
		// 	// }
		// 	//
		// 	// ObjectFighterActor actor = spawned.GetComponent<ObjectFighterActor>();
		//
		// 	AddFighter(new MockInfo
		// 	{
		// 		name         = actor.Name,
		// 		points       = actor.asset.Points,
		// 		stats        = actor.asset.Stats,
		// 		efficiencies = actor.asset.Efficiencies
		// 	}, slot, turnEvent: false, actor: spawned, auslot: hasHome);
		// 	actor.fighter.prefab = prefab;
		//
		// 	actor.fighter.props = props;
		// 	actor.Initialize();
		//
		// 	return actor.fighter;
		// }
		//
		// public void HideObject([NotNull] Fighter fighter)
		// {
		// 	fighter.actor.gameObject.SetActive(false);
		// }
		//
		// public void DespawnObject([NotNull] Fighter fighter)
		// {
		// 	RemoveFighter(fighter);
		// 	fighter.actor.gameObject.SetActive(false);
		//
		// 	//UnityEngine.Object.Destroy(fighter.actor.gameObject);
		//
		// 	//fighter = null;
		// }
		public Fighter AddFighter(
			[CanBeNull] FighterInfo info   = null,
			[CanBeNull] Slot        slot   = null,
			[CanBeNull] Team        team   = null,
			[CanBeNull] BattleBrain brain  = null,
			bool                    turns  = true,
			Vector2Int?             islot  = default,
			bool                    auslot = true
		)
		{
			info = info ?? new MockInfo();

			var s = team != null ? team.id.ToString() : "noteam";
			var fighter = new Fighter(info)
			{
				id         = $"{s}-{info.Name}",
				home       = slot,
				team       = team,
				brain      = brain,
				turnEnable = turns,
			};

			return AddFighter(fighter, islot, auslot);
		}

		/// <summary>
		/// Add a fighter to the battle.
		/// </summary>
		/// <param name="info">Static info of the fighter. (base stats)</param>
		/// <param name="slot">Slot to set the fighter at.</param>
		/// <param name="team">Team to add fighter to.</param>
		/// <param name="stickers">Stickers to give the fighter.</param>
		/// <param name="brain">A brain to give the fighter (overrides the team's brain).</param>
		/// <param name="turnEvent">Whether or not to add the fighter to the action order.</param>
		/// <param name="islot">Coordinate to lookup for the slot.</param>
		/// <param name="auslot">Automatically assign the slot based on the team's slot configuration. (last resort if no slot)</param>
		/// <returns></returns>
		[NotNull]
		public Fighter AddFighter(
			[NotNull] Fighter fighter,
			Vector2Int?       islot  = default,
			bool              auslot = true
		)
		{
			if (IsCapturing)
			{
				CurrentCapture.addedFighters.Add(fighter);
				return fighter;
			}

			if (fighter.brain != null)
			{
				RegisterBrain(fighter.brain);
			}

			fighter.battle = this;

			if (fighter.script != null)
			{
				fighter.script.Reinitialize();
				ExecuteAction(fighter.script.Init(), fighter);
			}

			Emit(Signals.add_fighter, fighter);

			// Register Fighter Features
			// ----------------------------------------
			fighters.Add(fighter);
			allFighters.Add(fighter);

			fighter.existence = Existence.Exist;
			fighter.points    = fighter.info.StartPoints;
			fighter.combo     = new ComboState();

			// Turns
			// ----------------------------------------
			if (fighter.turnEnable)
			{
				turns.AddEvent(fighter);
			}

			// Slots
			// ----------------------------------------
			if (fighter.home == null && islot != null)
			{
				fighter.home = GetSlot(islot.Value);
			}

			if (fighter.home == null && auslot && fighter.team != null)
			{
				if (runner != null && runner.step < BattleRunner.States.Playing)
				{
					SlotGrid slotg = fighter.team.slots;
					fighter.home = slotg.GetDefaultSlot(fighter.team.fighters.Count - 1);
				}
			}

			if (fighter.home != null)
			{
				if (auslot)
				{
					SetHome(fighter, fighter.home);
					fighter.snap_home();
				}
				else
				{
					fighter.snap_slot(fighter.home);
				}
			}

			// Team
			// ----------------------------------------
			if (fighter.team != null)
			{
				fighter.team.AddFighter(fighter, auslot);
			}

			// Stickers
			// ----------------------------------------
			if (fighter.info.Stickers != null)
			{
				foreach (StickerAsset sticker in fighter.info.Stickers)
					AddSticker(fighter, sticker);
			}

			// Configuration State
			// ----------------------------------------
			FighterInfo info      = fighter.info;
			State       baseState = fighter.baseState;

			fighter.status.Remove(baseState);
			baseState.Clear();
			if (info.BaseState.invincible)
				baseState.Add(new StateCmd(StatOp.up, StateStat.def, 1));
			if (info.BaseState.set_hurt >= 0)
				baseState.Add(new StateCmd(StatOp.set, StateStat.hurt, info.BaseState.set_hurt));
			if (info.BaseState.set_heal >= 0)
				baseState.Add(new StateCmd(StatOp.set, StateStat.heal, info.BaseState.set_heal));
			fighter.status.Add(baseState);

			FighterAdded?.Invoke(fighter);

			return fighter;
		}

		public void RegisterBrain([NotNull] BattleBrain brain)
		{
			brain.battle = this;
			brain.OnRegistered();
		}

		#endregion

		#region Fighter

		public void ReviveFighter([NotNull] Fighter revived)
		{
			revived.reviveMarked = false;
			revived.deathMarked  = false;
			revived.existence    = Existence.Exist;

			// Find and remove the death
			for (int i = 0; i < deaths.Count; i++)
			{
				if (deaths[i].fighter == revived)
				{
					deaths.RemoveAt(i);
					i--;
				}
			}

			Emit(Signals.revive_fighter, revived, new ReviveEvent { me = revived });

			AddFighter(revived);
			revived.coach?.OnRevive();

			// Re-insert turns into the action order
			// --------------------------------------------------
			int  spentRounds      = 0;
			bool currentRoundDone = false;

			int currentTurnCount = turns.order.Count;

			for (int i = turns.index; i < turns.order.Count; i++)
			{
				Action current   = turns[i];
				int    nextIndex = i + 1;

				if (nextIndex < turns.order.Count)
				{
					Action     next = turns[nextIndex];
					ITurnActer evt  = next.acter;

					if (next.marker == ActionMarker.RoundTail && spentRounds == 0)
					{
						//Current round: insert a single action at the end of that round
						turns.InsertTurn(nextIndex, turns.NewTurn(revived));
						i = nextIndex;
					}
					else if (spentRounds > 0 && !currentRoundDone)
					{
						// Future rounds: insert normal amount of turns according to priority
						if (evt != null && evt.TurnPriority < revived.TurnPriority || next.marker == ActionMarker.RoundTail)
						{
							// maybe make this reusable, this is most likely duplicated elsewhere
							for (int j = 0; j < 1 + revived.TurnBonus; j++)
							{
								turns.InsertTurn(nextIndex, turns.NewTurn(revived));
							}

							i = nextIndex + 1 + revived.TurnBonus;

							currentRoundDone = true;
						}
					}
				}

				if (current.marker == ActionMarker.RoundHead)
				{
					++spentRounds;

					currentRoundDone = false;
				}
			}

			//HoldCommand cmd = new HoldCommand(revived);
			//BattleAction action = cmd.GetAction(this);
			//action.RunInstant();
		}

		public void RemoveFighter([NotNull] Fighter fter, bool is_kill = false)
		{
			if (IsCapturing)
			{
				if (is_kill)
					CurrentCapture.killedFighters.Add(fter);
				else
					CurrentCapture.removedFighters.Add(fter);

				return;
			}

			if (is_kill)
			{
				var death = new Death(fter);

				var deathEvent = new DeathEvent { me = fter };
				if (EmitCancel(Signals.acccept_death, fter, deathEvent))
					return;

				Emit(Signals.kill_fighter, fter, deathEvent);
				Emit(Signals.kill, fter, deathEvent);

				deaths.Add(death);
			}

			Emit(Signals.remove_fighter, fter);

			// Release this fighter's resources
			// --------------------------------------------------

			// Release states
			var myStates = fter.states.ToList();
			foreach (State state in myStates)
				LoseState(fter, state);

			// Release team
			Team team = GetTeam(fter);
			team?.RemoveFighter(fter);
			team?.AddDead(fter);

			// Release slot
			SetHome(fter, null);

			// Release turns
			turns.RemoveEvent(fter);

			// Remove from battle
			fighters.Remove(fter);
			FighterRemoved?.Invoke(fter);

			// Update existence
			if (is_kill)
			{
				fter.existence = Existence.Dead;
				//fter.coach?.SetAnim(AnimID.CombatHurt);
				fter.coach?.OnDeath();
			}
			else
			{
				fter.existence = Existence.Removed;
				fter.coach?.SetIdle();
			}

			OnResourceRemoved(fter);
		}

		private bool                 _isRemovingResources = false;
		private List<BattleResource> _removedResources    = new List<BattleResource>();

		public void OnResourceRemoved(BattleResource res)
		{
			if (_isRemovingResources)
			{
				_removedResources.Add(res);
			}

			_isRemovingResources = true;

			for (var i = 0; i < skills.Count; i++)
			{
				BattleSkill skill = skills[i];
				if (skill.HasParent(res) && skill.RequiresParent)
				{
					RemoveSkill(skill);
					Debug.Log($"Removed skill {skill} because its parent was removed");
				}
			}

			for (var i = 0; i < triggers.Count; i++)
			{
				Trigger trigger = triggers[i];
				if (trigger.HasParent(res) && trigger.RequiresParent)
				{
					RemoveTrigger(trigger);
					Debug.Log($"Removed trigger {trigger} because its parent was removed");
				}
			}

			for (var i = 0; i < states.Count; i++)
			{
				State state = states[i];
				if (state.HasParent(res) && state.RequiresParent)
				{
					RemoveState(state);
					Debug.Log($"Removed state {state} because its parent was removed");
				}
			}

			for (var i = 0; i < fighters.Count; i++)
			{
				Fighter fighter = fighters[i];
				if (fighter.HasParent(res) && fighter.RequiresParent)
				{
					RemoveFighter(fighter);
					Debug.Log($"Removed fighter {fighter} because its parent was removed");
				}
			}

			_isRemovingResources = false;

			List<BattleResource> temp = ListPool<BattleResource>.Claim();
			temp.AddRange(_removedResources);
			foreach (BattleResource removed in temp)
				OnResourceRemoved(removed);
			ListPool<BattleResource>.Release(temp);


			_removedResources.Clear();
		}

		#endregion

		#region Fighter Stats

		public void MarkRevival([NotNull] Fighter fighter, float healAmount)
		{
			fighter.reviveMarked = true;

			float newHP = fighter.max_points.hp * healAmount;

			AddHP(fighter, newHP);
		}

		public void AddHP([NotNull] Fighter fighter, float hp)
		{
			AddPoints(fighter, new Pointf(hp));
		}

		public void AddSP([NotNull] Fighter fighter, float sp)
		{
			AddPoints(fighter, new Pointf(0, sp));
		}

		public void AddOP([NotNull] Fighter fighter, float op)
		{
			AddPoints(fighter, new Pointf(0, 0, op));
		}

		public void AddPoints([NotNull] Fighter fighter, PointChange chg)
		{
			Pointf points = fighter.points;
			Pointf max    = GetMaxPoints(fighter);
			var    amount = chg.value;

			if (!GameOptions.current.combat_hurt)
				amount.hp = amount.hp.Minimum(0);

			points += amount;
			points =  points.Max(max);
			points =  points.Floored(); // Remove floating points

			if (points.hp <= 0 && !fighter.deathMarked)
			{
				if (GameOptions.current.combat_deaths)
				{
					fighter.deathMarked = true;
					Emit(Signals.mark_dead, fighter);
					//fighter.coach?.OnDeath();
				}
				else
				{
					while (points.hp <= 0)
						points.hp += max.hp;

					fighter.LogEffect("--", $":: wrap HP below 0 to {points.hp} (options.{nameof(GameOptions.current.combat_deaths)})");
				}
			}

			fighter.points = points;
			FighterPointsChanged?.Invoke(fighter, chg);

			fighter.coach?.OnPointsChanged();

			fighter.LogEffect("--", $":: gain {amount}, now={fighter.points.ToString(GetMaxPoints(fighter))}");
		}

		public void SetPoints([NotNull] Fighter fighter, Pointf set)
		{
			fighter.points = set.Floored(); // Remove floating points
			fighter.LogEffect("--", $":: set points, now={fighter.points.ToString(GetMaxPoints(fighter))}");
		}

		/// <summary>
		/// Get the final max points of the fighter. (with all states and multipliers applied)
		/// </summary>
		public Pointf GetMaxPoints([NotNull] Fighter fter)
		{
			Pointf points = fter.info.Points.Mod(fter.status.points);

			points.op = 8;

			return points;
		}

		/// <summary>
		/// Get final stats of the fighter. (with all states and multipliers applied)
		/// </summary>
		public Statf GetStats([NotNull] Fighter fighter) => fighter.info.Stats.Mod(fighter.status.stat);

		public bool StatusApplied([NotNull] Fighter fighter, DynValue dv)
		{
			if (dv.AsEnum(out Elements elem))
			{
				ResistanceBracket bracket = Elementf.GetBracket(fighter.info.Resistances[elem]);

				return bracket != ResistanceBracket.Immune &&
				       bracket != ResistanceBracket.Absorb;
			}

			return true;
		}

		public Elementf GetBracketEffects([NotNull] Fighter fighter)
		{
			return GetResistance(fighter).GetBracketEffects();
		}

		/// <summary>
		/// Get final efficiency of the fighter. (with all states and multipliers applied)
		/// This final efficiency is then used for both damage efficency and resist efficiency.
		/// </summary>
		public Elementf GetResistance([NotNull] Fighter fighter)
		{
			return fighter.info.Resistances.Mod(fighter.status.resistance);
		}

		/// <summary>
		/// Get final damage stat of the fighter. (with all states and multipliers applied)
		/// NOTE:
		/// this is not a final damage value to apply to a victim or anything,
		/// this is a special battle stat derived from efficency
		/// which is used in damage formulas and computations.
		/// </summary>
		public Elementf GetOffense([NotNull] Fighter fighter)
		{
			return Elementf.Zero.Mod(fighter.status.atk);
		}

		/// <summary>
		/// Get final resist stat of the fighter. (with all states and multipliers applied)
		/// NOTE:
		/// this is a special battle stat derived from efficency
		/// which is used in damage formulas and computations.
		/// </summary>
		public Elementf GetDefense([NotNull] Fighter fighter)
		{
			Elementf resis = GetResistance(fighter);

			// Remove absorption
			resis = resis.Clamp(-1, 1);

			return resis.Mod(fighter.status.def);
		}

		/// <summary>
		/// Get final absorption stat of the fighter. (with all states applied)
		/// NOTE:
		/// this is a special battle stat derived from efficency
		/// which is used in damage formulas and computations.
		/// </summary>
		public Elementf GetAbsoption([NotNull] Fighter fighter)
		{
			Elementf resis = GetResistance(fighter);

			// keep everything between 1-2 and renormalize to 0-1
			resis -= 1;
			resis =  resis.Clamp(0, 1);

			return resis;
		}

		/// <summary>
		/// Get the current overdrive potential for the fighter.
		/// (max number of actions this fighter could execute in one overdrive)
		/// </summary>
		public int GetOverdrivePotential([NotNull] Fighter fighter)
		{
			int actions = fighter.points.op.Floor().Minimum(0);
			if (actions >= 1) // At 1 tick, you get 2 actions because it acts as a bonus to the action's action.
				actions++;

			return actions;
		}

		#endregion

		#region Fighter Combo

		public void ResetCombo([NotNull] Fighter fighter, bool broadcast = true)
		{
			fighter.combo.value = 0;

			if (broadcast)
				ComboChanged?.Invoke(fighter, fighter.combo.value);
		}

		public void IncrementCombo([NotNull] Fighter fighter, float value = 0.5f)
		{
			if (!fighter.combo.enabled)
				return;

			fighter.combo.value += value;

			ComboChanged?.Invoke(fighter, fighter.combo.value);
		}

		public void EnableCombo([NotNull] Fighter fighter, bool enable)
		{
			fighter.combo.enabled = enable;
		}

		public void ZeroCombo([NotNull] Fighter fighter)
		{
			fighter.combo.value = 0;
		}

		public float GetComboCount([CanBeNull] Fighter fighter)
		{
			if (fighter == null) return 0;
			return fighter.combo.enabled ? fighter.combo.value : 0;
		}

		public float GetComboMultiplier([CanBeNull] Fighter fighter)
		{
			if (fighter == null) return 0;
			return fighter.combo.enabled
				? Mathf.Lerp(1f, 2f, fighter.combo.value / 10)
				  + Mathf.LerpUnclamped(0f, 0.025f, Mathf.Max(0, fighter.combo.value - 10))
				: 1;
		}

		#endregion

		#region Fighter State

		[NotNull]
		public List<State> GetStates(IStatee statee, string tag_or_id)
		{
			_tmpStates.Clear();
			for (var i = 0; i < states.Count; i++)
			{
				State b = states[i];
				if (b.tags.Contains(tag_or_id)) _tmpStates.Add(b);
				if (b.ID == tag_or_id) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		[NotNull]
		public List<State> GetStates(string tag_or_id)
		{
			_tmpStates.Clear();

			for (var i = 0; i < states.Count; i++)
			{
				State b = states[i];
				if (b.tags.Contains(tag_or_id)) _tmpStates.Add(b);
				if (b.ID == tag_or_id) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		[NotNull]
		public List<State> GetStatesByTag(string tag)
		{
			_tmpStates.Clear();

			for (var i = 0; i < states.Count; i++)
			{
				State b = states[i];
				if (b.tags.Contains(tag)) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		[NotNull]
		public List<State> GetStatesByTag([NotNull] Fighter fighter, [NotNull] string tag)
		{
			_tmpStates.Clear();

			for (var i = 0; i < fighter.states.Count; i++)
			{
				State b = fighter.states[i];
				if (b.tags.Contains(tag)) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		[NotNull]
		public List<State> GetStatesByID([NotNull] string id)
		{
			_tmpStates.Clear();

			for (var i = 0; i < states.Count; i++)
			{
				State b = states[i];
				if (b.ID == id) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		[NotNull]
		public List<State> GetStatesByID([NotNull] IStatee statee, [NotNull] string id)
		{
			_tmpStates.Clear();

			for (var i = 0; i < statee.Status.Count; i++)
			{
				State b = statee.Status[i];
				if (b.ID == id) _tmpStates.Add(b);
			}

			return _tmpStates;
		}

		public void AddState([NotNull] State state)
		{
			foreach (IStatee statee in state.statees)
			{
				AddState(statee, state);
			}
		}

		/// <summary>
		/// Add a state to a fighter, with full state logic:
		/// - Emits gain-state, accept-state, refresh-state
		/// - Respects max stack size
		/// </summary>
		/// <param name="statee">The fighter gaining the state.</param>
		/// <param name="state>The state to gain</param>
		public void AddState([NotNull] IStatee statee, [NotNull] State state)
		{
			if (IsCapturing)
			{
				CurrentCapture.addStates.Add(new Capture.State(statee, state));
				return;
			}

			statee.LogEffect("++", $"gain ({state.ID}, life={state.maxlife})");

			var @event = new Data.StateEvent(state);

			// Check against existing stack
			// ----------------------------------------

			var stack = GetStatesByID(statee, state.ID).ToList();
			if (stack.Count > 0)
			{
				if (state.stackRefresh)
				{
					if (!EmitCancel(Signals.refresh_buff, state, @event))
					{
						foreach (State b in stack)
						{
							b.Refresh();
							statee.LogEffect("++++", $"({state.ID}) refreshed, life={b.life}/{state.maxlife}");
							StateRefreshed?.Invoke(statee, state);
						}
					}
				}

				// Max stack size reached.
				if (stack.Count >= state.stackMax)
				{
					Emit(Signals.maxed_stack, state, @event);
					statee.LogEffect("++++", $"({state.ID}) maxed state stack, stack={stack.Count}/{state.stackMax}");
					return;
				}
			}

			if (EmitCancel(Signals.gain_buff, statee, @event)) return;

			// Add the state
			// ----------------------------------------
			if (!states.Contains(state))
			{
				state.battle = this;
				states.Add(state);
			}

			state.AddStatees(statee);
			foreach (VFX vfx in state.vfx)
			{
				if (statee.Actor != null)
					statee.Actor.vfx.Add(vfx);
			}

			foreach (Trigger tg in state.triggers)
				AddTrigger(tg);

			this.LogEffect("--", $"({statee}) :: gain state ({state.ID})");

			state.isActive = true;

			// Recalculate status state
			// ----------------------------------------

			Statf   prevstats = Statf.Zero;
			Statf   newstats  = Statf.Zero;
			Fighter fter      = statee as Fighter;
			bool    isFighter = fter != null;

			if (isFighter)
			{
				prevstats = GetStats(fter);
			}

			statee.Status.Add(state);

			if (statee.Actor != null)
				statee.Actor.OnStateApplied(state);

			if (isFighter)
			{
				newstats = GetStats(fter);
				float d = newstats.speed - prevstats.speed;

				if (Mathf.Abs(d) > 0.00025f)
					OnSpeedRefresh(fter, (int)d);
			}

			state.Refresh();
			state.isActive = true;

			EmitCancel(Signals.enable, state, @event);
			EmitCancel(Signals.enable_state, state, @event);
			EmitCancel(Signals.enable_state, statee, @event);

			StateAdded?.Invoke(statee, state);
		}

		public void OnSpeedRefresh(IStatee statee, int change)
		{
			// Handler order transformation for speed refresh
			// ----------------------------------------
			if (statee is Fighter fter)
			{
				float  newspeed = GetStats(fter).speed;
				TurnSM sm       = new TurnSM(turns);

				do
				{
					// ReSharper disable once VariableHidesOuterVariable
					turns.Execute(o =>
					{
						TurnSM.Range g;

						bool left  = change > 0; // speed has raised
						bool right = change < 0; // speed went down

						// Select each our groups across the entire action order
						o.lim_clear();
						o.sel(o.seek_group(fter));

						do
						{
							// Seek forward or backward (depending on speed up/down) until the speeds are ordered
							o.lim_segment();
							g = o.seek_group(left ? -1 : 1);

							if (turns.order[g.min].acter is Fighter ft)
							{
								if (right && newspeed > ft.speed) break;     // Speed went down
								else if (left && newspeed < ft.speed) break; // Speed went up
							}
						} while (!o.is_touching_bounds()); // until we're aligned with the limits

						o.move(left
							? g.before
							: g.after);
					}, sm);

					if (sm.error)
						break;

					TurnOperationApplied?.Invoke(TurnOperationType.SpeedChange, sm);
				} while (true);
			}
		}

		public void RemoveState(State state, bool kill = true)
		{
			// if (IsCapturing)
			// {
			// 	if (kill)
			// 		CurrentCapture.killedStates.Add(state);
			// 	else
			// 		CurrentCapture.removedState.Add(state);
			//
			// 	return;
			// }

			state.LogEffect("--", $"{nameof(RemoveState)}({state}) at life={state.life}");

			if (kill)
			{
				state.dead = true;

				var @event = new Data.StateEvent(state);

				if (state.deathMode == State.Deaths.Consume)
					Emit(Signals.accept_consume, state, @event);
				else if (state.deathMode == State.Deaths.Consume)
					Emit(Signals.accept_expire, state, @event);

				if (@event.Cancel())
				{
					state.Refresh(state.deathLife);
					return;
				}

				// Regular emits
				if (state.deathMode == State.Deaths.Consume)
				{
					state.LogEffect("--", "consume");
					Emit(Signals.consume, state, @event);      // auto filter
					Emit(Signals.consume_buff, state, @event); // any filter

					if (animated && state.GetEnv().ContainsKey($"__{state.ID}_consume"))
						ExecuteAction(new CoplayerAnim(state.GetEnv().table, $"__{state.ID}_consume", new[] { @event }));
				}
				else
				{
					state.LogEffect("--", "expire");
					Emit(Signals.expire, state, @event);      // auto filter
					Emit(Signals.expire_buff, state, @event); // any filter

					if (animated && state.GetEnv().ContainsKey($"__{state.ID}_expire"))
						ExecuteAction(new CoplayerAnim(state.GetEnv().table, $"__{state.ID}_expire", new[] { @event }));
				}

				// c# events
				foreach (IStatee statee in state.statees)
				{
					if (state.deathMode == State.Deaths.Consume)
						StateConsumed?.Invoke(statee, state);
					else
						StateExpired?.Invoke(statee, state);
				}
			}

			// Remove from statees
			List<IStatee> statees = ListPool<IStatee>.Claim(state.statees.Capacity);
			statees.AddRange(state.statees);

			foreach (IStatee statee in statees)
				LoseState(statee, state);

			ListPool<IStatee>.Release(statees);

			OnResourceRemoved(state);
		}

		/// <summary>
		/// Remove states from a fighter using a table to filter.
		/// </summary>
		/// <param name="fighter"> Fighter which loses the state</param>
		/// <param name="filter">
		/// id: string or Table
		/// <string>
		/// @tag: string or Table<string>
		/// </param>
		/// <returns>Number of removed states</returns>
		public int LoseStates(
			[NotNull] Fighter fighter,
			[NotNull] Table   filter,
			int               count = -1
		)
		{
			AjLog.LogTrace("--", $"Battle.LoseStates({fighter.LogID}, filter:{filter}, count:{count})");

			List<State> mystates = fighter.states;

			filter.TryGet("id", out string id);
			filter.TryGet("tag", out string tag);
			filter.TryGet("id", out Table ids);
			filter.TryGet("tag", out Table tags);

			int numRemoved = 0;
			for (var i = 0; i < mystates.Count; i++)
			{
				State state = states[i];

				var matches = false;

				matches |= state.ID == id;
				matches |= state.tags.Contains(tag);

				if (ids != null)
				{
					for (var j = 1; j <= ids.Length; j++) matches |= state.ID == ids.Get(j).String;
				}

				if (tags != null)
				{
					for (var j = 1; j <= tags.Length; j++) matches |= state.tags.Contains(tags.Get(j).String);
				}

				for (var j = 1; j <= filter.Length; j++)
				{
					DynValue dv = filter.Get(j);

					if (dv.AsString(out string token))
					{
						matches = state.ID == token || state.tags.Contains(token);
					}
				}

				if (matches)
				{
					LoseState(fighter, state);
					numRemoved++;
					if (numRemoved >= count)
						break;
				}
			}

			return numRemoved;
		}

		/// <summary>
		/// Remove states from a fighter, using an id or string for filtering.
		/// </summary>
		/// <param name="fighter"></param>
		/// <param name="idFilter"></param>
		/// <param name="tagFilter"></param>
		/// <param name="count">Max number of states to remove.</param>
		/// <returns>Number of removed states</returns>
		public int LoseStates([NotNull] Fighter fighter,
			[CanBeNull]                 string  idFilter  = null,
			[CanBeNull]                 string  tagFilter = null,
			int                                 count     = -1
		)
		{
			if ((idFilter ?? tagFilter) == null)
			{
				Debug.LogError("Cannot remove state because neither check is set! (idCheck == null && stateCheck == null)");
				return 0;
			}

			var checkstr = $"id:{idFilter ?? "---"}, tag:{tagFilter ?? "---"}";

			AjLog.LogTrace("--", $"Battle.LoseStates({fighter.LogID}, {checkstr}");

			int numRemoved = 0;
			for (var i = 0; i < fighter.Status.states.Count; i++)
			{
				State state = fighter.Status.states[i];
				if (state.ID == idFilter || state.tags.Contains(tagFilter))
				{
					LoseState(fighter, state);
					i--;
					numRemoved++;
					if (numRemoved >= count && count > 0)
						break;
				}
			}

			return numRemoved;
		}

		/// <summary>
		/// Remove a state from a fighter, with signals.
		/// If the state is empty after this, it's removed from the battle.
		/// </summary>
		/// <param name="statee"></param>
		/// <param name="state></param>
		/// <param name="idx"></param>
		/// <returns></returns>
		public bool LoseState(
			[NotNull] IStatee statee,
			[NotNull] State   state
		)
		{
			if (!state.isActive)
			{
				Debug.LogError("Cannot remove inactive state!");
				return false;
			}

			if (IsCapturing)
			{
				CurrentCapture.loseStates.Add(new Capture.State(statee, state));
				return false;
			}

			// Remove
			// ----------------------------------------
			if (!statee.Status.Remove(state))
				return false;

			if (EmitCancel(Signals.lose_buff, statee, new Data.StateEvent(state)))
				return false;

			foreach (VFX fx in state.vfx)
				statee.RemoveVFX(fx);

			Statf   prevstats = Statf.Zero;
			Statf   newstats  = Statf.Zero;
			Fighter fter      = statee as Fighter;
			bool    isFighter = fter != null;

			if (isFighter)
			{
				prevstats = GetStats(fter);
			}

			state.statees.Remove(statee);
			statee.Status.Remove(state);

			if (statee.Actor)
				statee.Actor.OnStateRemoved(state);

			if (isFighter)
			{
				newstats = GetStats(fter);
				float d = newstats.speed - prevstats.speed;

				if (Mathf.Abs(d) > 0.00025f)
					OnSpeedRefresh(fter, (int)d);
			}

			// Disable
			// ----------------------------------------

			if (state.statees.Count == 0) // State may still be active for other fighters
			{
				// Unregister from the battle
				if (!state.isActive)
				{
					state.LogError("Cannot State.Disable because the state is already inactive.");
				}
				else
				{
					foreach (VFX fx in state.vfx)
						fx.Leave();

					foreach (Trigger trigger in state.triggers)
						state.battle.RemoveTrigger(trigger);
				}

				states.Remove(state);
				state.isActive = false;
			}

			statee.LogEffect("--", $"rem state ({state})");

			return true;
		}

		/// <summary>
		/// Check if the target fighter has the state. (by string id)
		/// </summary>
		/// <param name="target"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool HasState([CanBeNull] Fighter target, string id)
		{
			if (target == null)
			{
				Debug.LogError("Null fighter passed into HasState!");
				return true;
			}

			return target.states.Any(b => b.ID == id);
		}

		public bool HasState([CanBeNull] Slot target, string id)
		{
			if (target == null)
			{
				Debug.LogError("Null fighter passed into HasState!");
				return true;
			}

			return target.states.Any(b => b.ID == id);
		}

		/// <summary>
		/// Check if the target fighter has the state with a specific tag. (by string id)
		/// </summary>
		/// <param name="target"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool HasTag([CanBeNull] Fighter target, string id)
		{
			if (target == null)
			{
				Debug.LogError("Null fighter passed into HasState!");
				return true;
			}

			return target.states.Any(b => b.tags.Contains(id));
		}

		/// <summary>
		/// Check if the target slot has the state with a specific tag. (by string id)
		/// </summary>
		/// <param name="target"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool HasTag([CanBeNull] Slot slot, string id)
		{
			if (slot == null)
			{
				Debug.LogError("Null fighter passed into HasState!");
				return true;
			}

			return slot.states.Any(b => b.tags.Contains(id));
		}

		public int CountStates([NotNull] Fighter fighter, [NotNull] string id)
		{
			return fighter.states.Count(state => state.ID == id);
		}

		public bool HasFlag([CanBeNull] Fighter fighter, EngineFlags flag) => fighter != null && fighter.status.engineFlags.Contains(flag);
		public bool HasFlag([CanBeNull] Slot    slot,    EngineFlags flag) => slot != null && slot.status.engineFlags.Contains(flag);

		#endregion

		#region Fighter Skill

		public bool RegisterSkill(Fighter user, SkillAsset asset, [CanBeNull] out BattleSkill instance)
		{
			if (asset == null)
			{
				instance = null;
				return false;
			}

			// Instantiate Skill
			// ----------------------------------------
			instance = null;

			try
			{
				instance = new BattleSkill(this, asset)
				{
					battle = this,
					user   = user
				};

				instance.Parent = user;

				user.skills.Add(instance);
				skills.Add(instance);
			}
			catch (Exception e)
			{
				Debug.LogError($"Battle: failed to instantiate BattleSkill({asset.name}).", asset);
				Debug.LogException(e, asset);
				return false;
			}

			return true;
		}

		public void RemoveSkill(BattleSkill skill)
		{
			if (skill == null)
			{
				Debug.LogError("Null skill passed into RemoveSkill!");
				return;
			}

			if (skill.user != null)
				skill.user.skills.Remove(skill);

			skills.Remove(skill);

			OnResourceRemoved(skill);
		}

		[CanBeNull]
		public BattleSkill GetSkillOrRegister([NotNull] Fighter user, [CanBeNull] SkillAsset asset)
		{
			var ret = GetSkill(user, asset);
			if (ret != null)
				return ret;

			if (RegisterSkill(user, asset, out ret))
				return ret;

			return null;
		}

		[CanBeNull]
		public BattleSkill GetSkill([NotNull] Fighter user, [CanBeNull] SkillAsset asset)
		{
			foreach (BattleSkill skill in user.skills)
			{
				if (skill.asset == asset)
					return skill;
			}

			return null;
		}


		/// <summary>
		/// Get the cost of the skill. (with all states and multipliers)
		/// </summary>
		public Pointf GetSkillCost([NotNull] BattleSkill skill)
		{
			Status stats = skill.user.status;

			float sp = skill.Cost();
			stats.ModifyCost(skill, ref sp);

			return new Pointf(0, sp);
		}

		public void GetSkillTargets([NotNull] BattleSkill skill, [NotNull] Targeting targeting)
		{
			targeting.Clear();
			skill.Target(targeting);

			Status status = skill.user.status;

			for (var i = 0; i < targeting.options.Count; i++)
			{
				List<Target> options = targeting.options[i];
				for (var j = 0; j < options.Count; j++)
				{
					Target tg = options[j];

					bool valid = true;

					// Remove untargetable monsters
					for (var f = 0; f < tg.fighters.Count; f++)
					{
						Fighter ft = tg.fighters[f];
						if (!ft.targetable || HasFlag(ft, EngineFlags.untargetable))
						{
							valid = false;
						}
					}

					// Check allowed by stats
					if (valid && status.IsTargetForbidden(skill, tg))
						valid = false;

					// Remove empty targets
					if (valid && tg.IsEmpty)
						valid = false;

					if (!valid)
					{
						options.RemoveAt(j--);
					}
				}
			}

			status.ModifyTargetOptions(skill.user, skill, targeting);
		}

		public void ModifyTargetPicks([NotNull] Fighter user, UseInfo info, Targeting targeting)
		{
			user.status.ModifyTargetPicks(user, info, targeting);
		}

		public (bool, string) CanUse([CanBeNull] BattleSkill skill, bool selectable = true, bool confirmable = true, LimbType limb = LimbType.None, bool overdriveActive = false, int overdriveTotalCost = 0)
		{
			if (skill == null) return (false, "Nonexistent skill");
			if (skill.IsPassive) return (false, "Cannot select passive skill");

			// State
			if (skill.user.status.IsSkillForbidden(skill) || !selectable)
				return (false, "Limb locked");

			// Costs
			// ----------------------------------------

			Pointf cost = GetSkillCost(skill);
			Pointf now  = skill.user.points;

			if (cost.hp > now.hp) return (false, "Not enough HP");

			if (GameOptions.current.combat_use_cost)
			{
				if (!overdriveActive && cost.sp > now.sp || overdriveActive && overdriveTotalCost + cost.sp > now.sp)
					return (false, "Not enough SP");
			}

			if (cost.op > now.op) return (false, "Not enough OP");

			(bool usable, string error) = skill.Usable();
			if (!usable || !confirmable)
			{
				error = error ?? skill.GetError();

				return string.IsNullOrEmpty(error)
					? (false, "No valid targets")
					: (false, error);
			}

			if (!GameOptions.current.combat_use_cost)
				return (true, "");

			return (true, "");
		}

		#endregion

		#region Fighter Sticker

		/*[CanBeNull]
		public BattleSticker GetSticker([NotNull] Fighter stickerUser, [NotNull] StickerInstance sticker) => GetSticker(stickerUser, sticker.Asset);*/

		[CanBeNull]
		public BattleSticker GetSticker([NotNull] Fighter stickerUser, [NotNull] StickerInstance sticker)
		{
			if (sticker.Asset == null || sticker.Entry == null)
			{
				Debug.LogWarning($"Sticker Instance '{sticker}' does not have does not have a set asset.");
				return null;
			}

			(Fighter user, StickerInstance sticker) key = (stickerUser, sticker);

			if (_stickerInstances.TryGetValue(key, out BattleSticker script))
				return script;

			// Instantiate Sticker
			// ----------------------------------------
			if (sticker.Asset.script.Asset == null)
			{
				if (sticker.Asset.IsEquipment)
					return null;

				Debug.LogWarning($"Sticker '{sticker}' does not have a script.", sticker.Asset);
				return null;
			}
			else
			{
				script = new BattleSticker(this, sticker);
			}

			_stickerInstances[key] = script;

			script.battle = this;
			script.user   = stickerUser;

			return script;
		}

		public void GetStickerTargets([NotNull] BattleSticker battleSticker, [NotNull] Targeting targeting)
		{
			targeting.Clear();
			battleSticker.Target(targeting);
		}

		#endregion

		#region Proc

		/// <summary>
		/// Proccing works like this
		/// 1. Collect each victim
		///		2. Setup a proc context
		///		3. Emit proc signals (accept_proc, receive_proc, deal_proc)
		///		4. Collect each effect
		///			5. Call ProcEffect.BeforeApply (some effects modify the proc itself)
		///		6. Collect each effect
		///			7. Emit effect signals (accept_effect, deal_effect, accept_damage, deal_damage, accept_heal, deal_heal, ...)
		///			8. Apply the effect
		/// </summary>
		/// <param name="proc"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void Proc([NotNull] Proc proc)
		{
			if (proc.fired)
			{
				proc.LogError("Proc already fired");
				return;
			}

			if (proc.effects.Count == 0)
			{
				Debug.LogWarning($"Proc from '{proc.dealer}' has no effects.");
				return;
			}

			proc.battle = this;
			proc.GetEnv().Set("battle", this);

			var procEvent       = new ProcEvent(proc.dealer, null);
			var procEffectEvent = new ProcEffectEvent(proc.dealer, null);
			var combo           = false;

			ProcContext  ctx     = ObjectPoolSimple<ProcContext>.Claim();
			List<object> victims = ListPool<object>.Claim();

			try
			{
				// 1. Collect victims
				// ----------------------------------------
				if (proc.GetVictims(victims))
					return;

				// 2. Apply to victims
				// ----------------------------------------
				proc.LogEffect("--", $"deal to {fighters} victims");

				foreach (object victim in victims)
				{
					if (victim == null)
					{
						Debug.LogWarning("Skipping null in proc.victims");
						continue;
					}

					victim.LogEffect("--", $"deal proc ({proc.ID})");

					// Setup the context for effects ----------------------------------------
					SetupProcContext(proc, ctx, victim);
					if (EmitProcSignals(proc, ctx, victim, procEvent))
						continue;

					// BeforeApply() ----------------------------------------
					for (var i = 0; i < ctx.extendedEffects.Count; i++)
					{
						ProcEffect eff = ctx.extendedEffects[i];
						SetupProcEffect(ctx, eff);

						// Here, some effects can modify the context itself, such as extending with additional effects
						eff.BeforeApply();
					}

					// ApplyFighter() / ApplySlot() ----------------------------------------
					for (var i = 0; i < ctx.extendedEffects.Count; i++)
					{
						ProcEffect      eff = ctx.extendedEffects[i];
						ProcEffectEvent ev  = procEffectEvent;

						AjLog.LogTrace("----", $"Battle.Proc({proc.LogID}) #{i + 1}: ({eff})");
						if (RNG.Chance(ctx.status.EffectLuck(eff.chance)))
						{
							SetupProcEffect(ctx, ctx.extendedEffects[i]);
							if (EmitProcEffectSignals(ctx, victim, eff, ev))
								continue;

							switch (proc.kind)
							{
								case ProcKinds.Fighter:
									if (eff.fighter != null)
										combo |= eff.TryApplyFighter() == ProcEffectFlags.VictimEffect;
									break;

								case ProcKinds.Slot:
									if (eff.slot != null)
										combo |= eff.TryApplySlot() == ProcEffectFlags.VictimEffect;
									break;

								default:
									throw new ArgumentOutOfRangeException();
							}

							EmitProcEffectSignals_After(ctx, victim, eff, ev);
						}
						else
						{
							AjLog.LogTrace("----", $"CHANCE FAILED");
						}
					}

					// Send off events
					// ----------------------------------------
					Emit(Signals.receive_proc, victim, procEvent);
					ProcApplied?.Invoke(ctx);

					ctx.Reset();
				}

				if (combo && proc.dealer != null)
				{
					IncrementCombo(proc.dealer);
				}
			}
			finally
			{
				proc.fired = true;

				ctx.Reset();

				ObjectPoolSimple<ProcContext>.Release(ref ctx);
				ListPool<object>.Release(ref victims);
			}

			procCount++;
		}

		public void SetupProcContext([NotNull] Proc proc, [NotNull] ProcContext ctx, object victim)
		{
			ctx.Reset();
			ctx.proc   = proc;
			ctx.victim = victim;
			ctx.extendedEffects.AddRange(proc.effects);

			for (var i = 0; i < ctx.extendedEffects.Count; i++)
			{
				SetupProcEffect(ctx, ctx.extendedEffects[i]);
			}

			// Traverse up the ancestry and apply their procstats
			BattleResource res = proc;
			while (res != null)
			{
				ctx.status.Add(res.pstats);
				res = res.Parent;
			}
		}

		private void SetupProcEffect([NotNull] ProcContext ctx, [NotNull] ProcEffect eff)
		{
			eff.battle  = this;
			eff.ctx     = ctx;
			eff.proc    = ctx.proc;
			eff.dealer  = ctx.proc.dealer;
			eff.slot    = ctx.victim as Slot;
			eff.fighter = ctx.victim as Fighter;
		}

		private bool EmitProcSignals(
			[NotNull] Proc      proc,
			ProcContext         ctx,
			object              victim,
			[NotNull] ProcEvent @event)
		{
			@event.Reset();
			@event.context = ctx;

			if (proc.dealer != null && EmitCancel(Signals.deal_proc, proc.dealer, @event)) return true;
			if (EmitCancel(Signals.accept_proc, victim, @event)) return true;

			if (@event.missed) // TODO Display miss instead of anything else
				return true;

			return false;
		}

		private bool EmitProcEffectSignals(
			[NotNull] ProcContext     ctx,
			object                    fter,
			ProcEffect                eff,
			[NotNull] ProcEffectEvent @event)
		{
			@event.context = ctx;
			@event.noun    = eff;

			// Signals
			// ----------------------------------------
			if (EmitCancel(Signals.deal_effect, ctx.dealer, @event)) return true;
			if (EmitCancel(Signals.accept_effect, fter, @event)) return true;

			if (@event.hurts)
			{
				if (EmitCancel(Signals.deal_damage, ctx.dealer, @event)) return true;
				if (EmitCancel(Signals.accept_damage, fter, @event)) return true;
				// if (EmitCancel(Signals.accept_damage, fter, @event)) return true;
			}

			if (eff is AddState addState)
			{
				@event.noun = addState.state;

				// TODO use tag specific
				if (EmitCancel(Signals.deal_state, ctx.dealer, @event)) return true;
				if (EmitCancel(Signals.accept_state, fter, @event)) return true;
				// if (EmitCancel(Signals.accept_damage, fter, @event)) return true;
			}

			return false;
		}

		private bool EmitProcEffectSignals_After(
			[NotNull] ProcContext     ctx,
			object                    fter,
			ProcEffect                eff,
			[NotNull] ProcEffectEvent @event)
		{
			@event.context = ctx;
			@event.noun    = eff;

			// Signals
			// ----------------------------------------
			if (EmitCancel(Signals.dealt_effect, ctx.dealer, @event)) return true;
			if (EmitCancel(Signals.accepted_effect, fter, @event)) return true;

			if (@event.hurts)
			{
				if (EmitCancel(Signals.dealt_damage, ctx.dealer, @event)) return true;
				if (EmitCancel(Signals.accepted_damage, fter, @event)) return true;
			}

			if (eff is AddState addState)
			{
				@event.noun = addState.state;

				// TODO use tag specific
				if (EmitCancel(Signals.dealt_state, ctx.dealer, @event)) return true;
				if (EmitCancel(Signals.accepted_state, fter, @event)) return true;
				// if (EmitCancel(Signals.accept_damage, fter, @event)) return true;
			}

			return false;
		}

		#endregion

		#region Trigger

		private List<BattleAnim> _tmpActions = new List<BattleAnim>();

		private static TriggerEvent _nullEvent = new TriggerEvent();

		[NotNull] public Trigger AddTriggerCsharp([NotNull] Action<TriggerEvent> action, string id, Signals signal, [CanBeNull] object filter = null) =>
			AddTriggerCsharp(action, new Trigger
			{
				ID = id, signal = signal, filter = filter, enableTurnUI = false
			});

		[NotNull]
		public Trigger AddTriggerCsharp([NotNull] Action<TriggerEvent> fire, [NotNull] Trigger trigger)
		{
			trigger.LogSilenced  = true;
			trigger.enableTurnUI = false;
			trigger.AddHandlerDV(new Trigger.Handler(Trigger.HandlerType.action, fire));
			AddTrigger(trigger);
			return trigger;
		}

		public void AddTrigger([NotNull] Trigger tgr)
		{
			if (IsCapturing)
			{
				_captures.Peek().addedTriggers.Add(tgr);
				return;
			}

			// Warnings
			if (tgr.filter == Trigger.FILTER_AUTO)
			{
				tgr.LogWarn("FILTER_AUTO should've already been consumed by now.");
			}

			tgr.battle = this;
			triggers.Add(tgr);
			if (animated)
				tgr.DecideAnimID();

			tgr.LogEffect("++", "add");

			TriggerAdded?.Invoke(tgr);
		}

		/// <summary>
		/// Emit and get the list of triggers that will fire
		/// WARNING:
		/// The return list is a singleton and will cause Collection modified exceptions
		/// The result should be transfered to another list (with listpool) if there could be another emit in the middle of looping these triggers.
		/// </summary>
		/// <param name="signal"></param>
		/// <param name="me"></param>
		/// <param name="event"></param>
		/// <returns></returns>
		[NotNull]
		public List<Trigger> EmitDry(Signals signal, object me, TriggerEvent @event = null)
		{
			@event = @event ?? _nullEvent;

			_tmpTriggers.Clear();

			object tmpmp = @event.me;
			@event.me = me;

			if (runner.logEmits)
				AjLog.LogTrace(">>", $"Battle.EmitDry({signal}) to {triggers.Count(t => !t.LogSilenced)} triggers");

			foreach (Trigger tg in triggers)
			{
				if (!tg.IsAlive) continue;
				if (!tg.enabled) continue;
				if (tg.signal != signal) continue;
				if (@event is ProcEvent pevent && pevent.proc.Parent == tg) continue; // Prevents a loop where a trigger fires a proc that triggers the same trigger

				if (tg.MatchesFilter(@event.me))
				{
					tg.Decay();
					if (tg.WillFireNow())
					{
						_tmpTriggers.Add(tg);
					}
				}
			}

			@event.me = tmpmp;
			return _tmpTriggers;
		}

		/// <summary>
		/// Emit a signal, but allowing it to be canceled.
		/// </summary>
		/// <param name="signal"></param>
		/// <param name="me"></param>
		/// <param name="event"></param>
		/// <returns></returns>
		public bool EmitCancel(Signals signal, object me, TriggerEvent @event = null)
		{
			@event = @event ?? _nullEvent;

			// Save values
			object tmpme     = @event.me;
			bool   tmpcancel = @event.cancelable;

			// Assign overrides
			@event.me         = me;
			@event.cancelable = true;

			Emit(signal, me, @event);

			// Restore values
			@event.me         = tmpme;
			@event.cancelable = tmpcancel;

			return @event.Cancel();
		}

		/// <summary>
		/// Emit a signal with an event. The triggers that match this signal will fire a BattleAction that gets
		/// executed through the BattleCore.
		/// </summary>
		/// <param name="signal"></param>
		/// <param name="event"></param>
		public void Emit(Signals signal, object me, [CanBeNull] TriggerEvent @event = null)
		{
			@event = @event ?? _nullEvent;

			var tmpme = @event.me;
			@event.me = me;

			List<Trigger> triggers    = EmitDry(signal, me, @event);
			List<Trigger> tmpTriggers = ListPool<Trigger>.Claim();
			tmpTriggers.AddRange(triggers);

			try
			{
				BeginCapture();
				foreach (Trigger trg in tmpTriggers)
				{
					FireTrigger(trg, @event);
				}
			}
			finally
			{
				EndCapture();
				ListPool<Trigger>.Release(ref tmpTriggers);

				//OnSignalEmitted?.Invoke(signal, me);
			}

			CleanupDeadTriggers();

			@event.me = tmpme;
		}


		/// <summary>
		/// Fire a trigger, assuming its filter has been matched for already.
		/// </summary>
		[NotNull]
		public List<BattleAnim> FireTrigger([NotNull] Trigger tg, [CanBeNull] TriggerEvent @event)
		{
			@event = @event ?? _nullEvent;

			object restoreMe = @event.me;

			// This allows to watch for fighter triggers on slots instead
			if (tg.MatchesFilterFighterSlotConversion(@event.me))
			{
				var fter = (Fighter)@event.me;
				@event.me = fter.home;
			}

			AjLog.LogTrace(">>", $"Battle.FireTrigger({tg.LogID})");

			List<BattleAnim> tmpactions = ListPool<BattleAnim>.Claim();
			{
				tg.Fire(@event, tmpactions);
				foreach (BattleAnim action in tmpactions)
				{
					ExecuteAction(action, @event.me as Fighter);
				}

				TriggerFired?.Invoke(tg);
				if (tmpactions.Count > 0) // The trigger did something
					TriggerFiredEffective?.Invoke(tg);
			}
			ListPool<BattleAnim>.Release(ref tmpactions);

			@event.me = restoreMe;
			return tmpactions;
		}

		private void ExecuteAction(BattleAnim action, Fighter me = null)
		{
			if (animated)
				runner.ExecuteActionAsync(action, me).Forget();
			else
				runner.ExecuteAction(action, me);
		}


		private void CleanupDeadTriggers()
		{
			for (var i = 0; i < triggers.Count; i++)
			{
				Trigger tgr = triggers[i];
				if (tgr.ShouldDie)
					RemoveTrigger(tgr, i--);
			}
		}

		public bool RemoveTrigger(Trigger trigger, int index = -1)
		{
			if (index > -1)
			{
				trigger = triggers[index];
				trigger.LogEffect("xx", "rem");
				triggers.RemoveAt(index);
				return true;
			}

			bool ret = triggers.Remove(trigger);
			this.LogEffect("xx", "rem");

			OnResourceRemoved(trigger);
			return ret;
		}

		#endregion

		#region Slot

		/// <summary>
		/// Invoke the script function for the targeting.
		/// </summary>
		/// <param name="fter"></param>
		/// <param name="targeting"></param>
		/// <param name="include_current"></param>
		[NotNull]
		public Targeting GetUnoccupiedSlots(Fighter fter, [NotNull] Targeting targeting)
		{
			targeting.Clear();

			Table script = Lua.NewScript("target-api");
			script[LuaEnv.OBJ_BATTLE]    = this;
			script[LuaEnv.OBJ_TARGETING] = targeting;
			script[LuaEnv.OBJ_USER]      = fter;

			Lua.Invoke(script, "pick_slot_unoccupied_ally");
			Assert.AreEqual(1, targeting.options.Count);

			//if (include_current)
			//{
			//	var any = false;
			//	foreach (Target t in targeting.options[0])
			//	{
			//		if (t.Slot == fter.home)
			//		{
			//			any = true;
			//			break;
			//		}
			//	}

			//	if (!any)
			//	{
			//		targeting.options[0].Add(new Target(fter.home));
			//	}
			//}

			return targeting;
		}

		public void ClearHome([NotNull] Fighter fter) => SetHome(fter, null);

		// public void AssignSlot([NotNull] Fighter owner, Slot slot)
		// {
		// 	SetHome(owner, null);
		// }

		public void SetHome(Fighter me, [CanBeNull] Slot slot)
		{
			if (me == null)
			{
				Debug.LogError("SetHome: me is null");
				return;
			}

			if (slot == null)
			{
				me.home?.FreeSlot();
				return;
			}

			if (slot.owner != null)
			{
				slot.owner.home = null;
				slot.owner      = null;
			}

			slot.owner = me;
			me.home    = slot;

			this.LogTrace("--", $"{me} claimed {this}.");
		}

		public SlotSwap SwapHome([NotNull] Fighter me, [CanBeNull] Slot goal, MoveSemantic moveSemantic = MoveSemantic.Auto)
		{
			Slot start = me.home; // old/current slot

			var ret = new SlotSwap
			{
				me   = new SlotSwap.Change(me, me.home),
				from = me.home
			};

			if (goal == null)
			{
				this.LogError("Battle.Swap: Cannot swap to a null slot!", nameof(SwapHome));
				return ret;
			}

			if (HasFlag(me, EngineFlags.unmovable))
			{
				this.LogEffect("--", $"{me} :: SwapHome prevented by eflag.unmovable");
				return ret;
			}

			// Move semantics
			// ----------------------------------------

			if (moveSemantic == MoveSemantic.Auto)
				moveSemantic = me.moveSemantic;

			// ICY FLAG
			if (moveSemantic.IsAffectedByIce() && me.home != null)
			{
				Vector2Int dir = goal.coord - me.home.coord;
				dir.Clamp(
					new Vector2Int(-1, -1),
					new Vector2Int(1, 1));
				Team team = goal.team;

				// Extend the movement until it doesn't
				while (HasFlag(goal, EngineFlags.icy) || HasFlag(me, EngineFlags.icy))
				{
					Slot v = GetSlot(goal.coord + dir);
					if (v == null || v.team != null && v.team != team) // Can't slide onto an enemy slot
						break;

					goal = v;
				}
			}

			// REGULAR SLOT SWAP
			// ----------------------------------------
			Fighter them = GetOwner(goal);

			if (start == null && them != null) // Slot swap without already being on a slot ---> we don't know what happens to the monster on the goal slot
			{
				// In the future this might be needed, at which point we could swap to the nearest free slot or something..
				this.LogError("Battle.Swap: Cannot swap to a slot with a fighter when we do not yet have a home already!", nameof(SwapHome));
				return new SlotSwap(new SlotSwap.Change(me, me.home));
			}
			else if (start == goal) // No slot change (moved to same slot)
			{
				this.LogTrace("", $"Formation swap stub (unchanged slot for {me})");
				return new SlotSwap(new SlotSwap.Change(me, start));
			}
			else // Regular slot swap
			{
				this.LogEffect("--", $"{me} :: swap home to {goal})");

				// Fire off an accept event with the swap information
				var @event = new FormationEvent
				{
					swap = new SlotSwap(new SlotSwap.Change(me, goal))
				};
				@event.swap.from = start;

				if (them != null)
					@event.swap.swapee = new SlotSwap.Change(them, start);

				if (EmitCancel(Signals.accept_formation, me, @event))
					return new SlotSwap(new SlotSwap.Change(me, start));

				// Apply the modifications to the battle state
				goal.FreeSlot();
				start.FreeSlot();

				SetHome(me, goal);
				if (them != null)
					SetHome(them, start);

				return @event.swap;
			}


			throw new NotImplementedException();
		}

		[CanBeNull]
		public Fighter GetOwner([CanBeNull] Slot slot)
		{
			if (slot == null)
			{
				Debug.LogError("Cannot get slot owner for a null slot!");
				return null;
			}

			return slot.owner;
		}

		[CanBeNull]
		public Slot GetHome([CanBeNull] Fighter fighter)
		{
			if (fighter == null)
			{
				Debug.LogError("Cannot get owned slot for a null fighter!");
				return null;
			}

			return fighter.home;
		}

		[CanBeNull] public Slot GetSlot(Vector2Int coord) => _slotmap.SafeGet(coord);

		[CanBeNull] public Slot GetSlot(int x, int y) => _slotmap.SafeGet(new Vector2Int(x, y));

		[CanBeNull] public Slot GetSlot(Fighter fighter) => GetHome(fighter);

		public Slot GetSlot(Slot slot) => slot;

		#endregion

		/// <summary>
		/// During a capture, state or trigger cannot be added or removed from the battle.
		/// Instead, they are buffered and applied at the end of the capture.
		/// This is used to wrap certain features of Battle.
		/// </summary>
		/// <param name="enable"></param>
		private void BeginCapture()
		{
			_captures.Push(_capturePool.Rent());
		}

		/// <summary>
		/// End the capture and apply changes that were captured during capture.
		/// </summary>
		private void EndCapture()
		{
			if (!IsCapturing)
				throw new InvalidOperationException("Not currently in any Battle capture.");

			Capture tx = _captures.Pop();

			foreach (State state in tx.removedState) state.battle.RemoveState(state, false);
			foreach (State state in tx.killedStates) state.battle.RemoveState(state, true);
			foreach (Fighter fter in tx.removedFighters) fter.battle.RemoveFighter(fter);
			foreach (Fighter fter in tx.killedFighters) fter.battle.RemoveFighter(fter, true);
			for (var i = 0; i < tx.loseStates.Count; i++)
			{
				Capture.State lose = tx.loseStates[i];
				LoseState(lose.fighter, lose.state);
			}

			foreach (Fighter fter in tx.addedFighters) fter.battle.AddFighter(fter);
			for (var i = 0; i < tx.addedTriggers.Count; i++) AddTrigger(tx.addedTriggers[i]);

			for (var i = 0; i < tx.addStates.Count; i++)
			{
				Capture.State add = tx.addStates[i];
				AddState(add.fighter, add.state);
			}


			CleanupDeadTriggers();

			_capturePool.Return(tx);
		}

		/// <summary>
		/// Captures changes to the battle state in order to defer them to later.
		/// </summary>
		public class Capture : IRecyclable
		{
			public readonly List<State>      loseStates      = new List<State>(16);
			public readonly List<Data.State> removedState    = new List<Data.State>(16);
			public readonly List<Fighter>    removedFighters = new List<Fighter>(16);
			public readonly List<Data.State> killedStates    = new List<Data.State>(16);
			public readonly List<Fighter>    killedFighters  = new List<Fighter>(16);

			public readonly List<State>   addStates     = new List<State>(16);
			public readonly List<Trigger> addedTriggers = new List<Trigger>(16);
			public readonly List<Fighter> addedFighters = new List<Fighter>(16);

			public void Recycle()
			{
				addStates.Clear();
				loseStates.Clear();
				killedStates.Clear();
				removedState.Clear();
				addedTriggers.Clear();
				addedFighters.Clear();
				removedFighters.Clear();
			}

			public readonly struct State
			{
				public readonly IStatee    fighter;
				public readonly Data.State state;

				public State(IStatee fighter, Data.State state)
				{
					this.fighter = fighter;
					this.state   = state;
				}
			}
		}

		public struct ComboState
		{
			public bool  enabled;
			public float value;
		}

		/// <summary>
		/// Details about a past death in the battle.
		/// </summary>
		[LuaUserdata]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public readonly struct Death
		{
			public readonly Fighter fighter;
			public readonly Slot    slot;
			public readonly Team    team;

			public Death([NotNull] Fighter fighter)
			{
				this.fighter = fighter;
				slot         = fighter.home;
				team         = fighter.team;
			}
		}

		/// <summary>
		/// Details about a monster having moved to another slot.
		/// </summary>
		[UsedImplicitly]
		public struct SlotSwap
		{
			public SlotSwap(Change me)
			{
				this.me = me;
				swapee  = null;
				from    = null;
			}

			public SlotSwap(Change me, Change swapee)
			{
				this.me     = me;
				this.swapee = swapee;
				from        = null;
			}

			public SlotSwap(Change me, Slot from)
			{
				this.me   = me;
				swapee    = null;
				this.from = from;
			}

			public SlotSwap(Change me, Change swapee, Slot from)
			{
				this.me     = me;
				this.swapee = swapee;
				this.from   = from;
			}

			public Change  me;
			public Change? swapee;
			public Slot    from;

			public readonly struct Change
			{
				public Change(Fighter fighter, Slot slot)
				{
					this.fighter = fighter;
					this.slot    = slot;
					path         = new List<Slot>();
				}

				public readonly Fighter    fighter;
				public readonly Slot       slot;
				public readonly List<Slot> path;
			}
		}

		public void Cleanup()
		{
			foreach (Fighter fighter in fighters)
				fighter.brain?.Cleanup();

			foreach (Team team in teams)
				team.brain?.Cleanup();

			foreach (BattleSkill skill in skills)
				skill.Cleanup();

			fighters.Clear();
			teams.Clear();
		}

		public BattleBrain GetBaseBrain(Fighter fter)
		{
			if (fter == null)
				return null;

			BattleBrain teamBrain = GetTeam(fter)?.brain;
			BattleBrain fterBrain = fter.brain;

			return teamBrain ?? fterBrain;
		}

		public BattleBrain GetBrain(Fighter fter)
		{
			return fter.status.ModifyBrain(GetBaseBrain(fter));
		}

		/// <summary>
		/// Invoke the script function for the targeting.
		/// </summary>
		/// <param name="fter"></param>
		/// <param name="targeting"></param>
		/// <param name="include_current"></param>
		[NotNull]
		public Targeting GetFormationTargets([NotNull] Fighter fter, [NotNull] Targeting targeting, bool include_current = false)
		{
			targeting.Clear();

			Table script = Lua.NewScript("target-api");
			script[LuaEnv.OBJ_BATTLE]    = this;
			script[LuaEnv.OBJ_TARGETING] = targeting;
			script[LuaEnv.OBJ_USER]      = fter;

			string targeter = fter.info.FormationMethod;
			if (string.IsNullOrEmpty(targeter))
				targeter = "slot_ally";

			Lua.Invoke(script, $"pick_{targeter}");
			Assert.AreEqual(1, targeting.options.Count);

			if (include_current)
			{
				var any = false;
				foreach (Target t in targeting.options[0])
				{
					if (t.Slot == fter.home)
					{
						any = true;
						break;
					}
				}

				if (!any)
				{
					targeting.options[0].Add(new Target(fter.home));
				}
			}

			var slots = targeting.slots
				.Where(x => !x.status.engineFlags.Contains(EngineFlags.unmovable))
				.ToList();
			//targeting.s
			return targeting;
		}
	}
}
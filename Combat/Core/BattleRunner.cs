using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Anjin.Audio;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Features.TurnOrder.Events;
using Combat.Startup;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine.Assertions;
using SaveFiles;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Util.Addressable;
using Util.Odin.Attributes;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Combat
{
	/// <summary>
	/// The core of the battle.
	/// Implements the lifecycle of combat.
	/// Instructions are pushed to the core and then handled here, one by one. (in ProcessAsync)
	/// The core can be extended with chips that can implement intructions and block them.
	/// - During this, instructions are stateered and then placed left-to-right after the instruction.
	/// - Async can be used to block the instruction (in a CoreChip)
	/// Important files & classes for further reading:
	/// - CoreInstruction: all instructions currently used to implement Nanokin battles.
	/// - CoreChip:
	/// - FlowChip:
	/// - Battle: holds ALL of the stateful information involved in a Nanokin battle.
	/// </summary>
	public class BattleRunner : ILogger
	{
		public enum States { Zero, Initializing, Initialized, Playing, Crashed, Stopping }

		private const string COMBAT_SCENE   = "CORE_Combat";
		private const string ELAPSED_FORMAT = "s\\.fff";
		private const int    MAX_WAIT_TIME  = 500;

		/*
		 * Configuration
		 * Should be set before the battle starts.
		 ******************************************/

		/// <summary>
		/// Indicate that the core's battle is a simulation in memory only.
		/// (no visuals or interactions involving GameObjects)
		/// </summary>
		public bool animated = true;

		/// <summary>
		/// Config for animated fights.
		/// </summary>
		public BattleConfig animConfig;

		/// <summary>
		/// Battle info about the combat.
		/// </summary>
		public BattleIO io = new BattleIO();

		/// <summary>
		/// Chips to initialize with.
		/// </summary>
		public readonly List<Chip> initChips = new List<Chip>();

		/// <summary>
		/// Plugins to initialize with.
		/// </summary>
		public readonly List<BattleCorePlugin> initPlugins = new List<BattleCorePlugin>();

		/// <summary>
		/// Plugins to initialize with, which will be cleared after the current/next core execution.
		/// </summary>
		public readonly List<BattleCorePlugin> instancePlugins = new List<BattleCorePlugin>();

		/// <summary>
		/// Log instructions when they are executed.
		/// </summary>
		public bool logInitialization = true;

		/// <summary>
		/// Log instructions when they are executed.
		/// </summary>
		public bool logVisuals = true;

		/// <summary>
		/// Log instructions when they are executed.
		/// </summary>
		public bool logInstructions = false;

		/// <summary>
		/// Log turns with a nice header when they start.
		/// </summary>
		public bool logTurns = true;

		/// <summary>
		/// Log turns with a nice header when they start.
		/// </summary>
		public bool logState = true;

		/// <summary>
		/// Log trigger emissions
		/// </summary>
		public bool logEmits = false;

		/// <summary>
		/// Clear the logs on core start.
		/// Does nothing in builds.
		/// </summary>
		public bool logClearOnStart;

		/// <summary>
		/// Clear the logs on core restart.
		/// Does nothing in builds.
		/// </summary>
		public bool logClearOnRestart;

		/// <summary>
		/// core chip.
		/// </summary>
		public readonly CoreChip coreChip = new CoreChip();

		/*
		 * STATE
		 ******************************************/

		public States step;
		public Battle battle;
		public bool   skipNextCopyToFighterInfo;
		public bool   requestRestart;

		public AudioZone        music;
		public RawImage         screenFade;
		public AsyncHandles     handles = new AsyncHandles();
		public List<GameObject> objects = new List<GameObject>();

		private Stopwatch    _timer     = new Stopwatch();
		private UniTaskBatch _taskBatch = new UniTaskBatch(8);
		private List<Chip>   _chips     = new List<Chip>();
		private Table        _battleEnv;

		[ShowInPlay] private AsyncLazy               _stoptask;
		[ShowInPlay] private AsyncLazy               _playtask;
		[ShowInPlay] private CancellationTokenSource _playtaskCancelator;
		[ShowInPlay] private List<BattleAnim>        _activeActions = new List<BattleAnim>();
		[ShowInPlay] private List<CoreInstruction>   _instructions  = new List<CoreInstruction>();
		[ShowInPlay] private List<CoreInstruction>   _insertions    = new List<CoreInstruction>();
		[ShowInPlay] private CoreInstruction         _lastInstruction;
		[ShowInPlay] private int                     _lastInstructionID;


		private Func<bool> _funcAwaitActions;
		private Scene      _scene;


		public static Action onStartBattleSequence;

		public BattleRunner()
		{
			_funcAwaitActions = () => _activeActions.Count == 0;
		}

		/*
		 * Shortcuts and utilities
		 ******************************************/

		/// <summary>
		/// Indicates that the core has been restarted since we first started using it. (resets if the core is fully shutdown/recycled/etc)
		/// </summary>
		public bool hasRestarted { get; private set; }

		/// <summary>
		/// Shortcut to the input Arena.
		/// </summary>
		public Arena arena => io.arena;

		/// <summary>
		/// Shortcut to the input ArenaCamera.
		/// </summary>
		public ArenaCamera camera => io.arena.Camera;

		[NotNull]
		public string LogID => "BattleCore";

		public bool LogSilenced { get; set; }

		/// <summary>
		/// Gets a global dynamically created scene to use for battle.
		/// All battle resources are loaded into this scene to make it easier to manage.
		/// </summary>
		public Scene Scene
		{
			get
			{
				if (_scene.IsValid()) return _scene;

				for (var i = 0; i < SceneManager.sceneCount; i++)
				{
					Scene scene = SceneManager.GetSceneAt(i);

					if (scene.name == COMBAT_SCENE)
					{
						return _scene = scene;
					}
				}

				return _scene = SceneManager.CreateScene(COMBAT_SCENE);
			}
		}


		/// <summary>
		/// Launch a battle with a recipe. (init and play)
		/// </summary>
		public void Launch(BattleRecipe recipe)
		{
			if (step != States.Zero)
			{
				DebugLogger.LogError("Already initialized!", LogContext.Combat, LogPriority.Low);
				return;
			}

			io.recipe = recipe;
			_playtask = LaunchTask().ToAsyncLazy();
		}

		/// <summary>
		/// Launch a battle with io information. (init and play)
		/// </summary>
		public void Launch(BattleIO io)
		{
			if (step != States.Zero)
			{
				DebugLogger.LogError("Already initialized!", LogContext.Combat, LogPriority.Low);
				return;
			}

			this.io   = io;
			_playtask = LaunchTask().ToAsyncLazy();
		}

		private async UniTask LaunchTask()
		{
			await InitTask();
			await PlayTask();
		}

		/// <summary>
		/// Start playing the initialized BattleCore.
		/// </summary>
		public void Start()
		{
			if (step == States.Zero)
			{
				DebugLogger.LogWarning("BattleCore: the battle core is already busy playing.", LogContext.Combat, LogPriority.Low);
				return;
			}

			_playtask = PlayTask().ToAsyncLazy();
		}

		public async UniTask Play()
		{
			_playtask = PlayTask().ToAsyncLazy();
			await _playtask;
		}

		/// <summary>
		/// Stop playing the BattleCore.
		/// </summary>
		public AsyncLazy Stop()
		{
			if (_stoptask != null)
			{
				LogTrace("--", "Stop task already pending.");
				return _stoptask;
			}

			return _stoptask = StopTask().ToAsyncLazy();
		}

	#region Initialization

		/// <summary>
		/// Initialize the combat synchronously.
		/// - Lua core must already be initialized.
		/// - Chips only receive Install, not InstallAsync.
		/// - Recipe is not baked.
		/// </summary>
		public Battle Init()
		{
			if (step != States.Zero)
			{
				this.LogError("Core is already busy!");
				return null;
			}

			Reset();
			InitBegin();

			InitState();
			InitChips();
			InitStartup();

			InitEnd();

			return battle;
		}

		/// <summary>
		/// Init the battle using the BattleInfo stored in this core.
		/// </summary>
		public async UniTask<Battle> InitTask()
		{
			if (step != States.Zero)
			{
				this.LogError("Already initialized!");
				return null;
			}

#if UNITY_EDITOR
			if (logClearOnStart)
				ClearLog();
#endif

			Reset();
			InitBegin();

			if (animated)
			{
				await GameController.TillIntialized();

				LogTrace("**", "Core Game systems readied");
			}

			InitState();


			// CHIPS
			// ----------------------------------------
			InitChips();
			await _taskBatch;
			LogTrace("**", "Chips installed");


			// RECIPE
			// ----------------------------------------
			BattleRecipe recipe = io.recipe;
			if (recipe != null)
			{
				recipe.runner = this;
				await recipe.Bake();
				recipe.baked  = false;
				recipe.runner = null;

				LogTrace("**", "Recipe baked");
			}

			InitStartup();
			InitEnd();

			return battle;
		}

		private static void ClearLog()
		{
			ConsoleProDebug.Clear();
			Debug.ClearDeveloperConsole();
		}

		private void InitBegin()
		{
			onStartBattleSequence?.Invoke();
			_timer.Restart();
			step = States.Initializing;
			LogTrace("**", "Initializing...");
		}

		private void InitEnd()
		{
			LogTrace("**", "Initialized!");
			step = States.Initialized;
		}

		/// <summary>
		/// Init the core state of the battle.
		/// </summary>
		private void InitState()
		{
			io     = io ?? new BattleIO();
			battle = new Battle(this);

			if (animated)
			{
				// Get or create the scene

				// Start playing music
				music = AudioManager.AddMusic(io.music ?? io.arena.Music, 0.25f, 2000);

				screenFade = GameEffects.RentScreenFade();

				// Auto-register arena slot layouts
				foreach (Arena.SlotLayoutEntry entry in io.arena.SlotLayouts)
				{
					battle.AddSlots(entry.Layout);
				}

				BattleCoreSystem.activeCores.Add(this);
			}
		}

		/// <summary>
		/// Initialize and install chips.
		/// </summary>
		private void InitChips()
		{
			_chips.Add(coreChip);
			_chips.AddRange(initChips);

			foreach (Chip chip in _chips)
			{
				chip.runner = this;
				chip.battle = battle;

				chip.Install();
				if (animated)
				{
					_taskBatch.Add(chip.InstallAsync());
				}
			}
		}

		/// <summary>
		/// Insert the startup instructions.
		/// </summary>
		private void InitStartup()
		{
			Submit(CoreOpcode.IntroduceBattle);

			if (GameOptions.current.combat_autowin)
				Submit(CoreOpcode.WinBattle);

			Submit(CoreOpcode.PreStart);
			Submit(CoreOpcode.StartBattle);
		}

	#endregion

	#region Playing

		private static List<CoreInstruction> _capturedOpcodesTemp = new List<CoreInstruction>();

		private bool _isCapturingOpcodes = false;

		public void BeginCapture()
		{
			_isCapturingOpcodes = true;
			_capturedOpcodesTemp.Clear();
		}

		public List<CoreInstruction> EndCapture()
		{
			_isCapturingOpcodes = false;

			return _capturedOpcodesTemp;
		}

		public void Submit(CoreOpcode op, CoreInstruction data = new CoreInstruction())
		{
			data.op = op;

			if (_isCapturingOpcodes)
				_capturedOpcodesTemp.Add(data);
			else
				_insertions.Add(data);
		}

		public void Submit(BattleAnim instruction)
		{
			Submit(CoreOpcode.Execute, new CoreInstruction { anim = instruction });
		}

		public void SubmitWait(float f)
		{
			Submit(CoreOpcode.Wait, new CoreInstruction { duration = f });
		}

		public void SubmitReviveFlush()
		{
			Submit(CoreOpcode.FlushRevives);
		}

		public void SubmitDeathFlush()
		{
			Submit(CoreOpcode.FlushDeaths);
		}

		public void SubmitEmit(Signals signal, object me, TriggerEvent @event)
		{
			Submit(CoreOpcode.Emit, new CoreInstruction
			{
				signal       = signal,
				triggerEvent = @event,
				me           = me
			});
		}

		public void SubmitEmit(Signals signal, TriggerEvent @event)
		{
			Submit(CoreOpcode.Emit, new CoreInstruction
			{
				signal       = signal,
				triggerEvent = @event
			});
		}

		public void SubmitRestart()
		{
			skipNextCopyToFighterInfo = true;
			//Submit(CoreOpcode.Restart);
			Restart();
		}

		public void SubmitStop()
		{
			Submit(CoreOpcode.Stop);
		}


		/// <summary>
		/// Start the instruction processor which runs asynchronously.
		/// </summary>
		/// <returns></returns>
		private async UniTask PlayTask()
		{
			if (step != States.Initialized)
			{
				this.LogError("Core not initialized.");
				return;
			}

			LogTrace("--", "Play task starting...");

			step = States.Playing;

			while (!_playtaskCancelator.IsCancellationRequested)
			{
				try
				{
					while (_instructions.Count == 0 && _insertions.Count == 0)
						await UniTask.NextFrame();

					(bool result, UniTaskBatch tasks) = Next();
					if (!result)
					{
						step = States.Crashed;
						break;
					}

					if (tasks.IsWaitNecessary)
						await tasks;

					if (!_funcAwaitActions())
						await AwaitActions();

					// Special instructions
					switch (_lastInstruction.op)
					{
						case CoreOpcode.Stop:
							Stop();
							return;

						case CoreOpcode.Restart:
							Restart();
							return;
					}
				}
				catch
				{
					step = States.Crashed;
					DebugLogger.LogError("BattleCore crashed at a high-level. (not from a chip)", LogContext.Combat, LogPriority.Critical);
				}
			}
		}

		private async UniTask StopTask()
		{
			// We'll stop as soon as we can!
			LogTrace("--", "Waiting ");
			while (step == States.Initializing || step == States.Initialized)
				await UniTask.NextFrame();

			if (_playtask.Task.Status == UniTaskStatus.Pending)
			{
				LogTrace("--", "Scheduling stop");

				_playtaskCancelator?.Cancel();
				while (_playtask.Task.Status == UniTaskStatus.Pending)
				{
					CancelBrains();
					CancelAction();
					await UniTask.NextFrame();
				}
			}

			LogTrace("--", "Stopping");
			step = States.Stopping;

			bool restart = requestRestart;

			Cleanup();
			Reset();

			_stoptask = null;
			if (restart)
			{
				hasRestarted = true;
				Launch(io);
			}
		}

		/// <summary>
		/// Execute the next queued instruction.
		/// Animated/async executions are returned and can be awaited.
		/// </summary>
		public (bool, UniTaskBatch) Next()
		{
			step = States.Playing;

			if (_insertions.Count > 0)
			{
				_instructions.InsertRange(0, _insertions);
				_insertions.Clear();
			}

			if (_instructions.Count != 0)
			{
				_lastInstruction = _instructions.Pop();
				_lastInstructionID++;

				if (logInstructions)
					Log(_lastInstruction);

				foreach (Chip chip in _chips)
				{
					try
					{
						bool canHandle = chip.CanHandle(_lastInstruction);
						if (canHandle)
						{
							chip.Execute(ref _lastInstruction);
							if (animated)
								_taskBatch.Add(chip.ExecuteAsync(_lastInstruction));
						}

						// This lets a high-priority chip take control of things and abort all remaining chips.
						if (_playtaskCancelator.IsCancellationRequested)
							break;
					}
					catch (Exception e)
					{
						DebugLogger.LogException(e);
						DebugLogger.LogError($"BattleCore.Next crashed trying to execute a chip: {chip.GetType().Name}.", LogContext.Combat, LogPriority.Critical);
						return (false, _taskBatch);
					}
				}
			}

			return (true, _taskBatch);
		}

		/// <summary>
		/// Execute all next queued instructions.
		/// </summary>
		public void ForceNext()
		{
			step = States.Playing;

			if (_insertions.Count > 0)
			{
				_instructions.InsertRange(0, _insertions);
				_insertions.Clear();
			}

			while (_instructions.Count != 0)
			{
				_lastInstruction = _instructions.Pop();
				_lastInstructionID++;

				if (logInstructions)
				{
					Log(_lastInstruction);
				}

				foreach (Chip chip in _chips)
				{
					try
					{
						chip.Execute(ref _lastInstruction);
						if (animated)
						{
							_taskBatch.Add(chip.ExecuteAsync(_lastInstruction));
						}
					}
					catch (Exception e)
					{
						DebugLogger.LogException(e);
					}
				}
			}
		}

		/// <summary>
		/// Execute the next queued instruction.
		/// </summary>
		public void Force(CoreInstruction ins)
		{
			foreach (Chip chip in _chips)
			{
				try
				{
					chip.Execute(ref ins);
					if (animated)
					{
						_taskBatch.Add(chip.ExecuteAsync(ins));
					}
				}
				catch (Exception e)
				{
					DebugLogger.LogException(e);
				}
			}
		}

	#endregion

	#region Actions

		private void PrepareAction([NotNull] BattleAnim anim)
		{
			anim.runner  = this;
			anim.battle  = battle;
			anim.fighter = anim.fighter ?? battle.ActiveActer as Fighter;
		}

		private void PrepareAction([NotNull] BattleAnim anim, Fighter fighter)
		{
			anim.runner  = this;
			anim.battle  = battle;
			anim.fighter = fighter ?? anim.fighter;
		}

		public void ExecuteAction([NotNull] BattleAnim anim)
		{
			PrepareAction(anim);
			anim.RunInstant();
		}

		public void ExecuteAction([NotNull] BattleAnim anim, [NotNull] Fighter fighter)
		{
			PrepareAction(anim, fighter);
			anim.RunInstant();
		}

		public async UniTask ExecuteActionAsync([CanBeNull] BattleAnim anim, Fighter me = null)
		{
			if (anim == null) return;

			PrepareAction(anim, me);
			anim.cts = new CancellationTokenSource();

			_activeActions.Add(anim);
			await anim.RunAnimated();
			_activeActions.Remove(anim);
		}

		public UniTask AwaitActions()
		{
			if (_funcAwaitActions()) return UniTask.CompletedTask;
			return UniTask.WaitUntil(_funcAwaitActions);
		}

		/// <summary>
		/// Cancel the current action's execution so it completes immediately.
		/// </summary>
		public void CancelAction(bool graceful = false)
		{
			for (var i = 0; i < _activeActions.Count; i++)
			{
				BattleAnim anim = _activeActions[i];

				anim.cts.Cancel();
				anim.gracefulCancelation = graceful;
			}
		}

		/// <summary>
		/// Stop brains from thinking.
		/// </summary>
		public void CancelBrains()
		{
			foreach (Team team in battle.teams)
				team.brain?.cts?.Cancel();
		}

		/// <summary>
		/// Cancel the current action's execution so it completes immediately.
		/// </summary>
		public void CancelActions(bool graceful = false)
		{
			for (var i = 0; i < _activeActions.Count; i++)
			{
				BattleAnim anim = _activeActions[i];

				anim.cts.Cancel();
				anim.gracefulCancelation = graceful;
			}
		}

		public void CancelActions<T>(bool graceful = false)
			where T : BattleAnim
		{
			for (var i = 0; i < _activeActions.Count; i++)
			{
				BattleAnim anim = _activeActions[i];

				if (anim is T)
				{
					anim.cts.Cancel();
					anim.gracefulCancelation = graceful;
				}
			}
		}

		/// <summary>
		/// Execute until the next StartTurn instruction.
		/// </summary>
		public void WaitForResult()
		{
			for (var i = 0; i < MAX_WAIT_TIME; i++)
			{
				Next();
				if (_lastInstruction.op == CoreOpcode.LoseBattle || _lastInstruction.op == CoreOpcode.WinBattle)
					return;
			}
		}

		/// <summary>
		/// Execute until the next StartTurn instruction.
		/// </summary>
		public void NextTurn()
		{
			for (var i = 0; i < MAX_WAIT_TIME; i++)
			{
				Next();
				if (_lastInstruction.op == CoreOpcode.StartTurn)
					return;
			}
		}

		/// <summary>
		/// Execute until a number of StartTurn have passed.
		/// </summary>
		public void NextTurn(int count)
		{
			for (var i = 0; i < count; i++)
			{
				NextTurn();
			}
		}

		/// <summary>
		/// Execute until StartTurn for a specific fighter.
		/// </summary>
		public void NextTurn([NotNull] Fighter fighter)
		{
			if (battle.ActiveActer == fighter && battle.turns.Index == 0)
				return;

			for (var i = 0; i < MAX_WAIT_TIME; i++)
			{
				Next();
				if (_lastInstruction.op == CoreOpcode.StartTurn
				    && battle.ActiveActer == fighter)
					return;
			}

			throw new NotImplementedException(); // TODO warning no action
		}

		/// <summary>
		/// Execute until StartTurn for a specific team.
		/// </summary>
		public void NextTurn(Team team)
		{
			for (var i = 0; i < MAX_WAIT_TIME; i++)
			{
				Next();
				if (_lastInstruction.op == CoreOpcode.StartTurn
				    && battle.ActiveActer is Fighter fighter && fighter.team == team)
					return;
			}

			this.LogError($"Handler never arrived after {MAX_WAIT_TIME} turns.");
		}

		/// <summary>
		/// Execute until StartTurn for a specific team.
		/// </summary>
		/// <param name="brain"></param>
		public void NextTurn(BattleBrain brain)
		{
			// TODO(C.L): Does this need to be changed for fighters having their own optional brains?
			for (var i = 0; i < MAX_WAIT_TIME; i++)
			{
				Next();
				if (_lastInstruction.op == CoreOpcode.StartTurn
				    && battle.ActiveActer is Fighter fighter && fighter.team.brain == brain)
					return;
			}
		}

		/// <summary>
		/// Make the active fighter use a skill by name.
		/// </summary>
		public void UseSkill(string skillname, params int[] targetPicks)
		{
#if UNITY_EDITOR
			SkillAsset asset = Addressables2.LoadInEditor<SkillAsset>($"Skills/{skillname}");
			Assert.IsNotNull(asset, $"Could not find a skill asset for the name '{skillname}'.");

			UseSkill(asset, targetPicks);
#endif
		}

		/// <summary>
		/// Make the active fighter use a skill by name.
		/// </summary>
		public void UseOverdrive(List<BattleAnim> actions)
		{
			ExecuteAction(new OverdriveAnim(battle.ActiveFighter, actions));
		}

		public void UseSkill(SkillAsset skill, params int[] targetPicks)
		{
			BattleSkill instance = battle.GetSkillOrRegister(battle.ActiveFighter, skill);
			UseSkill(instance, targetPicks);
		}

		/// <summary>
		/// Make the active fighter use a skill.
		/// </summary>
		public void UseSkill([NotNull] BattleSkill skill, params int[] targetPicks)
		{
			var targeting = new Targeting();

			battle.GetSkillTargets(skill, targeting);

			if (targeting.options.Count == 0)
			{
				LogError("--", "Targeting has no groups.");
				return;
			}

			if (targeting.options.Count != targetPicks.Length)
			{
				LogError("--", "Number of target groups doesn't match number of picks.");
				return;
			}

			for (var i = 0; i < targeting.options.Count; i++)
			{
				List<Target> targets = targeting.options[i];
				if (targets.Count == 0)
				{
					LogError("--", $"No targets in group at index={i}");
					return;
				}

				int pick = targetPicks[i];
				if (targets.Count == 0)
				{
					LogError("--", $"Invalid target pick: {pick}");
					return;
				}

				targeting.AddPick(targets[pick]);
			}

			BattleAnim anim = skill.Use();
			if (anim != null)
				ExecuteAction(anim);
		}

		/// <summary>
		/// Make the active fighter move to a slot.
		/// </summary>
		public void MoveTo(int x, int y)
		{
			Slot slot = battle.GetSlot(x, y);
			MoveTo(slot);
		}

		/// <summary>
		/// Make the active fighter move to a slot.
		/// </summary>
		public void MoveTo(Slot slot)
		{
			MoveTo(battle.ActiveFighter, slot);
		}

		/// <summary>
		/// Make the active fighter move to a slot.
		/// </summary>
		public void MoveTo(Fighter fighter, Slot slot)
		{
			ExecuteAction(new MoveAnim(fighter, slot));
		}

		/// <summary>
		/// Make the active fighter move to a slot.
		/// </summary>
		public void MoveBy(int x, int y)
		{
			MoveBy(battle.ActiveFighter, x, y);
		}

		/// <summary>
		/// Make the active fighter move to a slot.
		/// </summary>
		public void MoveBy([NotNull] Fighter fighter, int x, int y)
		{
			if (fighter.home == null)
			{
				DebugLogger.LogError("Fighter has no slot.", LogContext.Combat);
				return;
			}

			MoveTo(fighter, battle.GetSlot(fighter.home.x + x, fighter.home.y + y));
		}

	#endregion

		public void Restart()
		{
			RestartAsync().Forget();
		}

		public async UniTask RestartAsync()
		{
			requestRestart            = true;
			skipNextCopyToFighterInfo = true;
			if (logClearOnRestart)
				ClearLog();

			await Stop();
		}


		/// <summary>
		/// Cleanup the battle's resources and release the state.
		/// </summary>
		public void Cleanup()
		{
			Profiler.BeginSample("BattleCore Cleanup");

			// Copy to info
			if (!skipNextCopyToFighterInfo)
			{
				foreach (Fighter fter in battle.fighters)
				{
					fter.info.SaveStats(fter);

					foreach (BattleSkill skill in fter.skills)
					{
						skill.Save(fter.info);
					}
				}

				skipNextCopyToFighterInfo = false;
			}


			if (animated)
			{
				BattleCoreSystem.activeCores.Remove(this);

				// Save to disk
				SaveManager.SaveCurrent();

				// Destroy all battle objects
				foreach (GameObject o in objects)
				{
					if (o != null)
						Object.Destroy(o);
				}

				objects.Clear();

				foreach (Fighter fighter in battle.fighters)
				{
					if (fighter.actor != null)
						Object.Destroy(fighter.actor.gameObject);
				}

				foreach (Slot slot in battle.slots)
				{
					if (slot.actor != null)
						Object.Destroy(slot.actor.gameObject);
				}

				// Stop music
				AudioManager.RemoveZone(music);
				music = null;

				// Return screen fade
				GameEffects.ReturnScreenFade(ref screenFade);
				screenFade = null;
			}

			battle.Cleanup();

			foreach (Chip chip in _chips)
			{
				Profiler.BeginSample($"Chip uninstall ({chip.GetType().Name})");
				chip.Uninstall();
				chip.runner = null;
				chip.battle = null;
				Profiler.EndSample();
			}


			Profiler.EndSample();

			battle = null;
		}

		private void Reset()
		{
			step           = States.Zero;
			hasRestarted   = false;
			requestRestart = false;

			instancePlugins.Clear();

			battle     = null;
			_battleEnv = null;

			_timer.Reset();
			_chips.Clear();
			_instructions.Clear();
			_insertions.Clear();
			_lastInstruction   = new CoreInstruction();
			_lastInstructionID = 0;

			_playtaskCancelator?.Dispose();
			_playtaskCancelator = new CancellationTokenSource();
		}

		private HashSet<BattleBrain> _alreadyUpdated = new HashSet<BattleBrain>();

		public void Update()
		{
			// Update active action
			var canSkip = true;
			for (var i = 0; i < _activeActions.Count; i++)
			{
				BattleAnim act = _activeActions[i];
				act.Update();

				canSkip &= act.Skippable;
			}

			if (canSkip && (GameInputs.confirm.IsPressed || GameInputs.cancel.IsPressed))
			{
				if (_activeActions.Count > 0)
					CancelAction(true);
			}

			// Update teams
			for (var i = 0; i < battle.teams.Count; i++)
			{
				BattleBrain brain = battle.teams[i].brain;
				if (_alreadyUpdated.Contains(brain))
					continue;
				brain?.Update();
				_alreadyUpdated.Add(brain);
			}

			for (int i = 0; i < battle.fighters.Count; i++)
			{
				BattleBrain b = battle.fighters[i].brain;
				if (_alreadyUpdated.Contains(b))
					continue;
				b?.Update();
				_alreadyUpdated.Add(b);
			}

			_alreadyUpdated.Clear();

			// Update chips
			for (var i = 0; i < _chips.Count; i++)
			{
				Chip chip = _chips[i];
				chip.Update();
			}

			// TODO very bad workaround
			// foreach (CoreInstruction ins in _insertions)
			// {
			// 	if(ins.op == CoreOpcode.LoseBattle || ins.op == CoreOpcode.WinBattle) {ForceNext(); break;}
			// }
		}


		/// <summary>
		/// Use instead of Hook when the battle is already running.
		/// This must be executed async because chips may not install
		/// instantaneously.
		/// </summary>
		public async UniTask Hook([NotNull] params Chip[] chips)
		{
			_chips.AddRange(chips);
			await UniTask.WhenAll(chips.Select(c => c.InstallAsync()));
		}


		public DynValue ExecLua(string code)
		{
			if (_battleEnv == null)
			{
				_battleEnv = Lua.NewEnv("battle-table");
				Lua.LoadFilesInto(LuaUtil.battleRequires, _battleEnv);
			}

			return Lua.LoadCodeInto(code, _battleEnv);
		}

		private void Log(CoreInstruction struc)
		{
			string details = null;

			switch (struc.op)
			{
				case CoreOpcode.Execute:
					details = $"{struc.anim}";
					break;

				case CoreOpcode.Wait:
					details = $"{struc.duration} sec";
					break;

				case CoreOpcode.ActTurn:
					ITurnActer acter = battle.ActiveActer;
					Team       team  = battle.GetTeam(acter);

					BattleBrain brain = team?.brain;
					string      brainString;
					if (brain == null)
						brainString = brain + "(team)";
					else
						brainString = "none";

					if (acter is Fighter ftr && ftr.brain != null)
					{
						brain       = ftr.brain;
						brainString = brain + " (fighter)";
					}

					details = $"event: {acter}, team: {team ?? (object)"none"}, brain: {brainString})";
					break;
			}

			LogTrace("--", $"[{_lastInstructionID}] {struc.op} :: {details ?? ""}");
		}

		public void LogTrace(string op, string msg)
		{
			if (!logInitialization && step < States.Initialized) return;
			AjLog.LogTrace(this, op, $"({_timer.Elapsed.ToString(ELAPSED_FORMAT)}) {msg}");
		}

		public void LogError(string op, string msg)
		{
			AjLog.LogError(this, op, $"({_timer.Elapsed.ToString(ELAPSED_FORMAT)}) {msg}");
		}

		public void DrawImGui()
		{
			ImGui.Text("Actions:");
			foreach (BattleAnim action in _activeActions)
				ImGui.Text(action.ToString());

			ImGui.Spacing();

			ImGui.Text("Instructions:");
			ImGui.Text(_lastInstruction.ToString());
			foreach (CoreInstruction ins in _instructions)
				ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), ins.ToString());
		}
	}
}
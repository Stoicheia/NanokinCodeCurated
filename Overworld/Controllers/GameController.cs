using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Anjin.Actors;
using Anjin.Audio;
using Anjin.Cameras;
using Anjin.Core.Flags;
using Anjin.Minigames;
using Anjin.MP;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using Anjin.Nanokin.SceneLoading;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using Combat.Launch;
using Combat.Scripting;
using Core.Debug;
using Cysharp.Threading.Tasks;
using Data.Overworld;
using Drawing;
using ImGuiNET;
using KinematicCharacterController;
using Overworld.Controllers;
using Overworld.Cutscenes;
using Overworld.Park_Game;
using Overworld.Shopping;
using Overworld.UI;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Util.Odin.Attributes;
using Debug = UnityEngine.Debug;
using g = ImGuiNET.ImGui;

/*using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;*/

namespace Anjin.Nanokin
{
	public delegate void GameEvent();
	public delegate void LevelEvent(Level level);
	public delegate void ActorEvent(Actor actor);
	public delegate void SpawnEvent(Spawn spawn);

	public class GameController : StaticBoy<GameController>, IDebugDrawer
	{
		#region Types

		public enum AppState
		{
			Startup,  // The starting state of the game. Overall initialization happens here, maybe show company logos or something.
			Menu,     // We are in some sort of menu outside of normal gameplay (Main menu, but there could be others as well)
			InGame,   // The player is playing the game in some way. Further state here is delegated to <see cref="GameplayState"/>.
			Shutdown, // The game is shutting down. (We may want to do some sort of overall cleanup here.)
		}

		/// <summary> The state of the level we're in. </summary>
		public enum LevelState
		{
			NothingLoaded, // There is no level loaded. It's less likely that gameplay is going to happen here,
			// but regular gameplay should be able to function regardless.
			Unloading, // We're unloading a level that we were in. Afterwards we need to either load another level, or do something else.
			Loading,   // We are loading a game level or scene, probably showing a loading screen.
			// (Note that this is not the only time the game should be able to load in scene. We may need to stream stuff in as gameplay is happening)
			AfterLoad,
			Loaded // There is a level loaded that gameplay can occur inside.
		}

		/// <summary>
		/// The state concerning the players interaction with the game.
		/// TODO potentially just merge with AppState? Everything between a certain range is game
		/// </summary>
		public enum GameState
		{
			PreSpawn, // The state is in gameplay, but nothing specifically has control.
			// This is the default state until control is given to something else.

			// Warping states (while the game fades out and in)
			WarpOut, WarpIn,

			// Specifically for in-map warps, mainly to insure smoothness with cinemachine
			MidWarp,

			Overworld,		// The player is in control of a spawned actor in the game in some way.

			// Void outs, fail states, ect where the player needs to be teleported back to some checkpoint or safe position.
			// Handled in ActorController
			OverworldDeath,

			Battle,			// The player is in a nanokin battle. Control is given over to the ParkController.
			Cutscene,		// A cutscene has taken control away from the player to play out.
			Minigame		// A minigame has taken control!
		}

		/// <summary> Is the game world paused in some way? </summary>
		public enum WorldPauseState
		{
			Running,   // The game world is running.
			FullPaused // The game world is fully paused.
			// CutscenePaused,
		}

		public enum NextToLoad
		{
			Level,
			Scene
		}

		public enum SpawnMenuState
		{
			SelectSpawnPoint,
			SelectCharacter,
		}

		//We should only be able to control the debug cam or the player at one time for now.
		public enum PlayerControlRoute
		{
			Player,
			DebugCam,
		}

		#endregion

		//	Global State
		//========================================================================

		[Title("Global State")]
		[NonSerialized, ShowInInspector] public AppState			StateApp = AppState.Startup;
		[NonSerialized, ShowInInspector] public LevelState			StateLevel = LevelState.NothingLoaded;
		[ShowInInspector]				 public GameState			StateGame { get; set; } = GameState.PreSpawn;
		[NonSerialized, ShowInInspector] public WorldPauseState		State_WorldPause = WorldPauseState.Running;
		[NonSerialized, ShowInInspector] public SpawnMenuState		StateSpawnMenu   = SpawnMenuState.SelectSpawnPoint;
		[NonSerialized, ShowInInspector] public PlayerControlRoute	ControlRoute     = PlayerControlRoute.Player;

		public static bool IsQuitting;
		public static bool DebugMode;
		public static bool DebugPause;
		public static bool DebugStepForwards;
		public static bool DebugStepForwardsFixedFlag;
		public static bool DebugStepForwardsFixed;

		[NonSerialized, ShowInPlay] public bool Initialized;
		[NonSerialized, ShowInPlay] public bool luaInitialized;

		[ShowInPlay] private float _startingFixedDT;

		//	Base Level Globals
		//-----------------------------------------------------------

		[Title("Global Vars")]
		public int TargetFPS = 60;

		// The game window should be scalable to any resolution, but theses are the ones selectable from the options screen.
		public static List<Vector2Int> SupportedResolutions = new List<Vector2Int>
		{
			// 16x9 resolutions
			new Vector2Int(1024, 576),
			new Vector2Int(1152, 648),
			new Vector2Int(1280, 720),
			new Vector2Int(1366, 768),
			new Vector2Int(1600, 900),
			new Vector2Int(1920, 1080),
			new Vector2Int(2560, 1440),
			new Vector2Int(3840, 2160),
		};

		//[NonSerialized] public bool GameplayPaused; // A way to pause any gameplay happening.

		[NonSerialized, ShowInInspector] public Cutscene ControllingCutscene; // A reference to any overworld cutscene that's taken control of the game. (Not all world cutscenes do this)
		[NonSerialized, ShowInInspector] public Minigame CurrentMinigame;

		//	Level/Scene/World
		//-----------------------------------------------------------
		[Title("Levels")]
		public static Level ActiveLevel;

		// For synchronization.
		// Note(C.L. 11-26-22): Maybe this could be useful, though I don't think we use this anywhere yet.
		public static float GlobalSyncTimer;

		[NonSerialized, ShowInInspector] public  float WarpTimer;
		[NonSerialized, ShowInInspector] private bool  _warpInLock;

		[ShowInPlay] private Checkpoint _lastCheckpointTouched;

		//  Spawn Menu
		//-----------------------------------------------------------
		[Title("Spawn Menu")]
		public Actor SelectedCharacter = null;

		private WarpInstructions? _currentWarp;
		private SceneLoadHandle   _loadHandle;
		private SceneUnloadHandle _unloadHandle;

		private List<SceneLoadHandle> _arenaLoadHandles;

		[ShowInPlay] private int _spawnPoint = -1;

		// Events
		//========================================================================
		public GameEvent OnInitialized;

		public GameEvent OnEnterGameplay;
		public GameEvent OnExitGameplay;

		// Multiple events for dependencies
		// Note(C.L. 11-26-22): The usefulness of this is less so now, this may be refactored out at some point
		public LevelEvent OnFinishLoadLevel_1;
		public LevelEvent OnFinishLoadLevel_2;

		public LevelEvent OnBeforeLeaveLevel;

		public static GameEvent OnMidWarp;
		public static GameEvent OnEndMidWarp;

		public static AsyncLazy levelLoaded;


		//	Helper Properties to better define what is possible based on global
		//	game state
		// TODO Make all of these statics for ease of access
		//========================================================================

		[Title("States")]
		[ShowInInspector] public bool IsMainMenu => StateApp == AppState.Menu;

		[ShowInInspector] 	public        bool IsInLevel            => Live.StateApp == AppState.InGame && Live.StateLevel == LevelState.Loaded;
		[ShowInInspector] 	public        bool IsPlayerControlled   => Live.StateApp == AppState.InGame && Live.StateGame  == GameState.Overworld && ControlRoute == PlayerControlRoute.Player;
		[ShowInInspector] 	public        bool IsCutsceneControlled => Live.StateApp == AppState.InGame && Live.StateGame  == GameState.Cutscene;
		[ShowInInspector] 	public        bool IsInBattle           => Live.StateApp == AppState.InGame && Live.StateGame  == GameState.Battle;
		[ShowInInspector] 	public static bool IsMinigame           => Live.StateApp == AppState.InGame && Live.StateGame == GameState.Minigame;

		[ShowInInspector]
		[HideInEditorMode]  public static bool IsWorldPaused => Exists && (Live.State_WorldPause == WorldPauseState.FullPaused || MinigameQuitPrompt.Live.Active || (DebugPause && !DebugStepForwards));

		[ShowInInspector] 	public        bool IsLevelLoading       => StateLevel == LevelState.Loading || StateLevel == LevelState.Unloading;
		[ShowInInspector] 	public        bool IsWarping            => StateGame == GameState.WarpIn || StateGame == GameState.WarpOut || StateGame == GameState.MidWarp;
		[ShowInInspector] 	public        bool AnyLoading           => IsLevelLoading || !Lua.Ready;

		public static bool OverworldEnemiesActive	=> Live.StateGame == GameState.Overworld && !Live.IsCutsceneControlled && !IsWorldPaused;
		public static bool OverworldHUDShowable		=> (Live.IsPlayerControlled && Live.IsInLevel && Live.StateGame == GameState.Overworld && !SplashScreens.IsActive) || SplicerHub.ShouldShowCredits                                                                                    || ShopMenu.menuActive;

		public static bool CanSave => !BattleController.Live.IsAnyEnemyAggroed;

		//	Helper Functions for awaiting for global state
		//========================================================================
		public static async UniTask TillIntialized()         => await UniTask.WaitUntil(() => Live != null && Live.Initialized);
		public static async UniTask TillLuaIntialized()      => await UniTask.WaitUntil(() => Live != null && Live.Initialized);
		public static async UniTask TillLevelLoaded()        => await UniTask.WaitUntil(() => Live.StateLevel == LevelState.Loaded);
		public static async UniTask TillInitAndLevelLoaded() => await UniTask.WaitUntil(() => Live != null && Live.Initialized && Live.StateLevel == LevelState.Loaded);


		//	Static Initialization
		//========================================================================
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			ActiveLevel  = null;
			OnMidWarp    = null;
			OnEndMidWarp = null;
			DebugPause   = false;
			IsQuitting   = false;
			levelLoaded  = null;

			GlobalSyncTimer = 0;
		}


		//	Basic Operations
		//========================================================================

		protected override void OnAwake()
		{
			Initialized    = false;
			luaInitialized = false;

			Application.quitting += () => IsQuitting = true;

			_startingFixedDT = Time.fixedDeltaTime;

			// Note(C.L. 8-9-22): This should only be done ONCE. Doing this messes up any currently-running UniTask instances
			typeof(PlayerLoopHelper).GetMethod("Init", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
		}

		private void Start() => StartupTask().ForgetWithErrors();

		private async UniTask StartupTask()
		{
			// Wait for some amount of time so all other statics can actually initialize
			await UniTask.DelayFrame(5);

			DebugSystem.Register(this);

			Stopwatch sw = Stopwatch.StartNew();
			GameOptions.InitializeThroughGameController();
			sw.Stop();
			Debug.Log($"[Init] GameOptions {sw.Elapsed}");

			var batch = UniTask2.Batch();
			if (!GameOptions.current.load_on_demand)
			{
				foreach (string s in Addresses.PRELOAD_SCENES)
				{
					SceneLoader.GetOrLoadAsync(s).Batch(batch);
				}
			}

			InitializeAllSubsystems().Batch(batch);

			// Wait for both important scenes to be loaded and subsystem initialization
			await batch;

			Initialized = true;
			OnInitialized?.Invoke();
			Debug.Log("[Init] All subsystems initialized!");

			GameOptions.current.screen_resolution.onUpdate += v =>
			{
				if (SupportedResolutions.TryGet((int)v, out var res))
				{
					Screen.SetResolution(res.x, res.y, false);
				}
			};

			if (StateApp != AppState.Menu)
			{
				if (Level.all.Count > 0)
				{
					ActiveLevel = Level.all[0];
					GameSceneLoader.EnsureLevelRegistered(Level.all[0]);
				}
				else
				{
					this.LogError("No active level found");
				}
			}

#if UNITY_EDITOR
			_currentWarp = null;

			if (BattleLauncherInjection.injections.Count > 0)
				return; // Let the injection start combat

			if (ActiveLevel != null)
				enterLevelAndSpawnAsync(ActiveLevel, true); // Launch into the level

			else if (MenuManager.activeMenuComponents.Count > 0)
			{
				// Nothing else to do, might as well launch a menu if we have it
				IMenuComponent menu = MenuManager.activeMenuComponents.FirstOrDefault();
				menu?.EnableMenu();
			}
			else
				// Inform the user
				DebugLogger.Log("Started without any level scene loaded. You can use the debug menu to load a level if you want.");
#endif
		}


		/// <summary>
		/// Start up all global controllers.
		/// </summary>
		private async UniTask InitializeAllSubsystems()
		{
			// Create a function to wrap a unitask with a stopwatch and log the time it took
			async UniTask timingTask(UniTask task, string name)
			{
				Stopwatch sw2 = new Stopwatch(); // Name conflicts with outer scope

				sw2.Restart();
				await task;
				sw2.Stop();

				Debug.Log($"[Init] {name} took {sw2.Elapsed}");
			}

			await UniTask.WhenAll(
				timingTask(Addressables.InitializeAsync().ToUniTask(), "Addressables"),
				timingTask(GameAssets.Live.InitializeThroughGameController(), "GameAssets"),
				timingTask(UniTask.Create(async () =>
				{
					await Lua.InitalizeThroughGameController();
					luaInitialized = true;
				}), "Lua")
			);

			// sw.Restart();
			// sw.Stop();
			// Debug.Log($"[Init] Lua {sw.Elapsed}");

			Stopwatch sw = new Stopwatch();
			sw.Restart();
			await ActorController.Live.InitializeThroughGameController();
			sw.Stop();
			Debug.Log($"[Init] ActorController {sw.Elapsed}");

			// Save Data
			sw.Restart();
			await SaveManager.InitializeThroughGameController();
			sw.Stop();
			Debug.Log($"[Init] SaveManager {sw.Elapsed}");
		}


	#region New Games

		[ShowInPlay]
		public static void NewGame_PubBuild(bool introCutscene = true, SaveFileID? saveID = null)
		{
			Live.NewGame(new NewGameInfo
			{
				saveFileID     = saveID.GetValueOrDefault(SaveFileID.DefaultIndexed),
				saveSetup      = data => data.SetupPrologueFreeport(),
				targetMenu     = GameAssets.Live.DebugLevelSelectMenuScene,
				targetWarpID   = introCutscene ? 201 : WarpReceiver.SPAWN_MENU,
				targetManifest = Resources.Load<LevelManifest>("Level Manifests/Oceanus/ocean_Freeport") /*await Addressables.LoadAssetAsync<LevelManifest>("Manifests/Freeport")*/,
				halt_music     = introCutscene,
			});
		}

		[ShowInPlay]
		public static void NewGame_DebugBase(SaveFileID? saveID = null)
		{
			Live.NewGame(new NewGameInfo
			{
				saveFileID = saveID.GetValueOrDefault(SaveFileID.DefaultIndexed),
				targetMenu = GameAssets.Live.DebugLevelSelectMenuScene,
			});
		}


		[ShowInPlay]
		public static void NewGame_DebugMaxed(SaveFileID? saveID = null)
		{
			Live.NewGame(new NewGameInfo
			{
				saveFileID = saveID.GetValueOrDefault(SaveFileID.DefaultIndexed),
				saveSetup  = data => data.SetMaxedData(),
				targetMenu = GameAssets.Live.DebugLevelSelectMenuScene,
			});
		}

		public struct NewGameInfo
		{
			public Action<SaveData> saveSetup;
			public SaveFileID       saveFileID;

			public LevelManifest   targetManifest;
			public SceneReference? targetMenu;
			public int?            targetWarpID;
			public bool            halt_music;
		}

		public void NewGame(NewGameInfo info)
		{
			SaveData save = SaveManager.CreateFile(info.saveFileID, true);

			if (info.saveSetup != null)
				info.saveSetup.Invoke(save);
			else
				save.SetBaseData();

			SaveManager.Set(save);

			Quests.Live.Reload();

			if (info.targetManifest)
				ChangeLevel(
					new WarpInstructions
					{
						type               = WarpTargetType.Level,
						spawnType          = WarpSpawnType.Reciever,
						target_level       = info.targetManifest,
						target_reciever_id = info.targetWarpID.GetValueOrDefault(WarpReceiver.SPAWN_MENU),
						halt_music         = info.halt_music,
					});

			else if (info.targetMenu.HasValue)
			{
				StateApp = AppState.Menu;
				GoToOtherMenu(info.targetMenu.Value, null);
			}
		}

	#endregion

	#region Loading

		public bool LoadGameFromSaveData(SaveData data)
		{
			if (data == null)
			{
				this.LogError($"{nameof(LoadGameFromSaveData)}(): Provided data was null.");
				return false;
			}

			if (data.Location_Current == null)
			{
				this.LogError($"{nameof(LoadGameFromSaveData)}(): Player location was null.");
				return false;
			}

			if (SaveDataToWarpInstructions(data, out WarpInstructions ins))
			{
				ChangeLevel(ins);
			}
			else
			{
				this.LogError($"{nameof(LoadGameFromSaveData)}(): Could not get a valid warp instructions from the player location.");
				return false;
			}

			StateGame = GameState.PreSpawn;

			SaveManager.Set(data);

			return true;
		}

		public void OnBattleDeath(float reviveAt = 0.55f)
		{
			string lastSaveLocation = SaveManager.current.Location_LastSavePoint.MostRecentSavePointID;
			var    newPoint         = SavePoint.allLoaded.FirstOrDefault(point => point.ID == lastSaveLocation);
			ActorController.playerActor.Teleport(newPoint == null ? SavePoint.allLoaded.First().SpawnPoint : newPoint.SpawnPoint);

			SaveManager.current.RevivePartyAt(reviveAt);

		}

		public async void OnChangeSavedata(SaveData data, SetSaveAction action)
		{
			if (action != SetSaveAction.CopyCurrent)
			{
				if (Flags.Exists)

					Flags.ResetAll();

				await Quests.Live.ReloadAsync();

				if (Flags.Exists)
				{
					//Flags.ResetAll();

					foreach (FlagStateBase flag in Flags.Live.AllFlags)
					{
						if (data.FlagValues.TryGetValue(flag.DefBase.Name, out object value))
						{
							#if UNITY_EDITOR
							flag.SetValue(value, true, true);
							#else
							flag.SetValue(value, true);
							#endif
						}
					}
				}

				Quests.Live.DoQuestUpdates();

				if (SplicerHub.Exists)
				{
					SplicerHub.Live.ResetData();
				}

				if (QuestNotifyHUD.Live)
					QuestNotifyHUD.Live.Notifications.Clear();
			}
		}

	#endregion


		public void RegainControl()
		{
			StateGame = GameState.Overworld;
		}

		public void UpdateWorldState()
		{
			switch (StateLevel)
			{
				case LevelState.NothingLoaded: break;

				// If we are unloading, we assume that we've placed an unload request and we should wait for it to finish
				case LevelState.Unloading:
				{
					bool done = !_unloadHandle.IsValid || _unloadHandle.IsDone;
					if (!done || _currentWarp == null || !_currentWarp.Value.valid)
					{
						// TODO/WARNING: PANIC
						break;
					}

					if (_currentWarp.Value.type == WarpTargetType.Level)
						LoadLevel(_currentWarp.Value.target_level);
					else
						LoadScene(_currentWarp.Value.target_scene);
				}
					break;

				case LevelState.Loading:
				{
					bool loadingDone = !_loadHandle.IsValid || _loadHandle.IsDone;
					if (!loadingDone) break;

					StateLevel = LevelState.AfterLoad;
					enterLevelAndSpawnAsync(_loadHandle.LoadedGroup?.Level);
					break;
				}

				case LevelState.AfterLoad:
					break;

				case LevelState.Loaded:
					/*if (Flags.GetBool("intro_cutscene_viewed"))
					{
						AudioManager.Play();
					}*/

					//if (CanControlPlayer())
					//{

					//}

					break;
			}
		}

		private async void enterLevelAndSpawnAsync(Level level, bool already_loaded = false)
		{
			ActiveLevel = level;

			levelLoaded = _enterLevelAndSpawnAsync(level, already_loaded).ToAsyncLazy();
			await levelLoaded;
			levelLoaded = null;
			await UniTask.DelayFrame(10);
			CallLuaOnActivate();
		}

		private async UniTask _enterLevelAndSpawnAsync(Level level, bool already_loaded = false)
		{
			await enterLevelAsync(level, already_loaded);
			await UniTask.DelayFrame(5);
			await SaveManager.GetCurrentAsync();

			if (StateGame == GameState.PreSpawn || StateGame == GameState.WarpIn)
			{
				bool deferredFade = false;

				// Note(C.L.): I usually wouldn't use a try/catch, but it's REALLY IMPORTANT that the below fadein gets called. This is in case AutoSpawn throws something.
				try
				{
					if (AutoSpawn(out deferredFade))
					{
						await UniTask.DelayFrame(1);
						ParkAIController.Live.PerformTotalPeepRedistribution();
					}
				}
				catch (Exception e)
				{
					DebugLogger.LogException(e);
					throw;
				}

				if (!deferredFade)
					GameEffects.FadeIn(0);
			}
			else
			{
				GameEffects.FadeIn(0);
			}

			// Hopefully this will insure we don't spawn when peeps are in the middle of their walk cycles

			//_currentWarp = null;
		}

		/// <summary>
		/// Setup all of the level using its manifest and
		/// run functionality related to this level starting up.
		/// </summary>
		/// <param name="level"></param>
		private async UniTask enterLevelAsync(Level level, bool already_loaded = false)
		{
			if (level == null) return;
			if (StateLevel == LevelState.Loaded) return;

			StateApp         = AppState.InGame;
			State_WorldPause = WorldPauseState.Running;

			GlobalSyncTimer = 0;

			// Level setup
			// ----------------------------------------
			LevelManifest manifest = level.Manifest;

			EncounterLayer encounterLayer = level.AddComponent<EncounterLayer>();
			encounterLayer.Settings = manifest.EncounterLayer;

			if (already_loaded && !GameOptions.current.load_on_demand)
			{
				foreach (SceneReference scene in manifest.ArenaScenes)
				{
					Scene existing = SceneManager.GetSceneByName(scene.SceneName);
					if (!existing.isLoaded)
						loadArenaAsync(scene, _loadHandle.LoadedGroup);
				}
			}

			// Register audio profiles
			AudioManager.musicLayer.Setup(manifest.MusicProfile);
			AudioManager.ambienceLayer.Setup(manifest.AmbientProfile);

			if (_currentWarp.HasValue && _currentWarp.Value.halt_music)
				AudioManager.Stop(false);
			else
				AudioManager.Play();

			RegionController.OnLevelLoad(manifest);

			// Register scripts
			// Note(C.L. 8-9-22): already handled in new startup
			//await Lua.initTask;

			Lua.LevelTable["level_name"] = level.Manifest.DisplayName;

			foreach (LuaAsset script in manifest.Scripts)
			{
				if (script == null || script.Path == null)
					DebugLogger.LogError($"Level script for manifest {manifest.name} is null!", LogContext.Core, LogPriority.Critical);
				else
				{
					LuaChangeWatcher.BeginCollecting(); // should probably be a part of AddGlobalScript automatically
					Lua.AddGlobalScript(script);
					LuaChangeWatcher.EndCollecting(this, () => Lua.ScheduleReload(script));
				}
			}

			CallLuaOnActivate(true);

			await UniTask.DelayFrame(3);

			await ParkAIController.Live.GlobalInit();
			ParkAIController.Live.OnLevelLoad(manifest);

			DebugLogger.Log("On Level Start", LogContext.Core, LogPriority.Low);
			Lua.RunGlobal("on_level_start", null, true);

			BattleController.SetEncountersActive();

			try
			{
				// WARNING:
				// This can easily break a lot of things because of one error
				// TODO make this unnecessary, simply use Unity's Start() where appropriate
				// and inline code here when it is related to the level itself
				OnFinishLoadLevel_1?.Invoke(ActiveLevel);
				OnFinishLoadLevel_2?.Invoke(ActiveLevel);
			}
			catch (Exception e)
			{
				DebugLogger.LogException(e);
			}


			SaveData save = await SaveManager.GetCurrentAsync();
			//save.Location = new

			// NOTE(C.L) we don't actually need this right atm, but I think we will, so I'll go ahead and do it.
			if (manifest.Area != Areas.None && !save.AreasVisited.Contains(manifest.Area))
				save.AreasVisited.Add(manifest.Area);

			if (manifest.Level != LevelID.None && !save.LevelsVisited.Contains(manifest.Level))
				save.LevelsVisited.Add(manifest.Level);


			MotionPlanning.OnGraphChange();

			// After level init
			// ----------------------------------------
			StateLevel = LevelState.Loaded;
		}


		//===========================================================================================================//
		//	SPAWNING																								 //
		//===========================================================================================================//

		private bool AutoSpawn(out bool deferredFade)
		{
			deferredFade = false;

			_spawnPoint = 0;

			// if (_currentWarp == null)
			// {
			// 	StateGame = GameState.PreSpawn;
			// 	GameEffects.FadeIn(0);
			// 	return;
			// }

			// Attempt to spawn using the current warp instructions

			if (_currentWarp != null)
			{
				WarpInstructions ins = _currentWarp.Value;
				switch (ins.spawnType)
				{
					case WarpSpawnType.Reciever:
					{
						int? target = ins.target_reciever_id;
						if (target.HasValue && target.Value > -1)
						{
							// We have a spawn receiver to use
							// cannot be bypeassed by debug tools
							// (can be used for cutscenes, warps, etc.)
							SpawnFromWarp(target.Value);
							deferredFade = true;
							return true;

							/*if (_currentWarp.Value.target_reciever_id == 999999)
							{
								//GameEffects.FadeIn(0);
								SpawnParty(SaveManager.current.Position, new Vector3(-0.8f, 0.0f, -0.7f));
							}
							else
							{
							}

							// =====================================
							// this is a charm designed to keep special-grade typing errors away
							// accidentally removing the following line of code will
							// explode the entire universe and summon sephiroth
							//_spawnReceiver = -1;
							// =====================================
							return;*/
						}
					}
						break;

					case WarpSpawnType.Savepoint:
					{
						SpawnPoint sp = SavePoint.FindByID(ins.save_point_id)?.SpawnPoint;
						if (sp != null)
						{
							SpawnParty(sp);
							return true;
						}
					}
						break;

					case WarpSpawnType.Position:
					{
						SpawnParty(ins.position, ins.facing);
						return true;
					}
						break;

					case WarpSpawnType.None:
					{
						StateGame = GameState.PreSpawn;
						return true;
					}
						break;
				}
			}


			/*if ((_currentWarp?.target_reciever_id != null) && ())
			{

			}*/

			// NOTE (CL): If we aren't spawning into a reciever, we're just going to do this as a safety measure.
			// I've had multiple issues with the screen not fading back in after spawning in certain edge cases.
			//GameEffects.FadeIn(0);

			void SpawnAtHighestPrioritySpawnPoint()
			{
				//GameEffects.FadeIn(0);
				SpawnPoint spawn = SpawnPoint.allActive[0];
				SpawnParty(spawn);
				DebugLogger.Log($"Spawning at highest priority SpawnPoint: {spawn.gameObject.name}. (option.ini: spawn_with_priority)", LogContext.Overworld, LogPriority.Low);
			}

			// Development Spawn Methods
			// ----------------------------------------
			bool imguiSpawn    = GameOptions.current.spawn_with_imgui || _currentWarp != null && _currentWarp.Value.target_reciever_id == WarpReceiver.SPAWN_MENU;
			bool prioritySpawn = GameOptions.current.spawn_with_priority;

			bool devSpawns = imguiSpawn || prioritySpawn;

			if (devSpawns && SpawnPoint.allActive.Count == 0)
			{
				DebugLogger.Log("No spawn points to use for dev auto-spawn.", LogContext.Overworld, LogPriority.High);
			}
			else if (imguiSpawn)
			{
				// IMGUI spawn menu
				//GameEffects.FadeIn(0);
				StateGame = GameState.PreSpawn;

				// N.B.: auto-spawn in the following sentence doesn't hold the same meaning as this function's name!
				DebugLogger.Log("Spawning using imgui menu. (option.ini: spawn_with_imgui)", LogContext.Overworld, LogPriority.Low);
				return false;
			}
			else if (prioritySpawn)
			{
				// Auto-spawn to first spawn-point
				SpawnAtHighestPrioritySpawnPoint();
				return true;
			}

			// If we couldn't spawn at a crystal, we should still try to spawn
			SpawnAtHighestPrioritySpawnPoint();

			return true;
		}

		/// <summary>
		/// Spawn at a position.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="prefab"></param>
		public void SpawnParty(Vector3 pos, Vector3 rot, Actor prefab = null)
		{
			ActorController.SpawnParty(new Spawn { position = pos, facing = rot, prefab = prefab });
			/*Actor player = ActorController.SpawnPlayer(spawn);
			if (player == null)
				return;

			ActorController.SetPlayer(player);
			ActorController.playerCamera.ReorientInstant(spawn.facing);

			if (ActorController.Live.SpawnParty && !GameOptions.current.ow_no_party_spawning)
			{
				ActorController.SpawnPartyMembers(player);
			}*/

			if (!IsWarping)
				StateGame = GameState.Overworld;

			OnEnterGameplay?.Invoke();
		}

		/// <summary>
		/// Spawn at a spawn point
		/// </summary>
		/// <param name="spawnPoint"></param>
		/// <param name="prefab"></param>
		public void SpawnParty(SpawnPoint spawnPoint, Actor prefab = null)
		{
			SpawnParty(spawnPoint.GetSpawnPointPosition(0), spawnPoint.GetSpawnPointFacing(0), prefab);
			spawnPoint.OnSpawn();
		}

		/// <summary>
		/// Spawn at a region object.
		/// </summary>
		public void SpawnParty(RegionObject regobj, Actor prefab = null)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Spawn at a warp id.
		/// </summary>
		/// <param name="warp_id"></param>
		/// <param name="prefab"></param>
		public void SpawnFromWarp(int warp_id, Actor prefab = null)
		{
			var receiver = WarpReceiver.FindReceiver(warp_id);
			if (receiver == null)
			{
				DebugLogger.LogError("Could not find target warp id that was requested for the spawn.", LogContext.Overworld, LogPriority.High);
				return;
			}

			SpawnParty(receiver.GetStartingPosition(), Vector3.forward, prefab);

			if (receiver.Cutscene)
			{
				receiver.Cutscene.StartDelayFrames = 60;
				receiver.Cutscene.Play();
			}
			else
			{
				TransitionVolume trans = receiver.GetComponent<TransitionVolume>();
				if (ActorController.playerActor && trans)
				{
					//Debug.Log("Manual registration of transition brain.");

					ActorController.Live.TransitionBrain.isWarpOut = false;
					ActorController.Live.TransitionBrain.StartUsing(ActorController.playerActor as PlayerActor, trans);

					ActorController.playerBrain.transitionToggle = trans;
					ActorController.playerBrain.triggerToggles.AddRange(trans.GetComponents<Trigger>());

					GameCams.ReleaseBlendOverride();
				}
			}
		}

		public bool SaveDataToWarpInstructions(SaveData data, out WarpInstructions instructions)
		{
			instructions = new WarpInstructions();

			SaveData.PlayerLocation location            = data.Location_Current;
			SaveData.PlayerLocation saveCrystalLocation = data.Location_LastSavePoint;
			SaveData.SaveOrigin     origin              = data.Origin;

			//if (location != null) {

			LevelID id = location.Level;

			if (origin == SaveData.SaveOrigin.Savepoint)
				id = saveCrystalLocation.Level;

			if (id != LevelID.None)
			{
				if (LevelManifestDatabase.LoadedDB.IDsToManifests.TryGetValue(id, out var manifest))
				{
					instructions.target_level = manifest;
				}
				else
				{
					this.LogError($"LoadFromSave(): Player location had level that the manifest database does not have cataloged: {location.Level}.");
					return false;
				}
			}
			else
			{
				this.LogError($"LoadFromSave(): Player location did not have a target level specified.");
				return false;
			}

			if (origin == SaveData.SaveOrigin.Savepoint && saveCrystalLocation.MostRecentSavePointID != null)
			{
				instructions.spawnType     = WarpSpawnType.Savepoint;
				instructions.save_point_id = saveCrystalLocation.MostRecentSavePointID;
			}
			else if (location.LastStableStandingPosition.HasValue)
			{
				instructions.spawnType = WarpSpawnType.Position;
				instructions.position  = location.LastStableStandingPosition.Value;
				instructions.facing    = location.FacingDirection.GetValueOrDefault(Vector3.forward);
			}
			else
			{
				instructions.spawnType = WarpSpawnType.None;
				//instructions.target_reciever_id = null;
			}

			/*} else {
				// TODO(C.L.): Maybe we should have either a toggle or make this editor only, and have the game have a default level to spawn at.
				this.LogError($"LoadFromSave(): Player location was null.");
				return false;
			}*/

			return true;
		}


		/// <summary>
		/// Unload the level.
		/// </summary>
		/// <param name="activeLevel"></param>
		private void leaveLevel(Level activeLevel)
		{
			OnBeforeLeaveLevel?.Invoke(ActiveLevel);

			ActorController.EnsureDespawned();
			ActorController.LastStableStandingPosition      = null;
			ActorController.LastStablePlayerFacingDirection = null;
			ActorController.PlayerPosition                  = null;
			ActorController.PlayerFacing                    = null;

			LevelManifest mfest = activeLevel.Manifest;

			LuaChangeWatcher.ClearWatches(this);
			foreach (LuaAsset script in mfest.Scripts)
			{
				Lua.RemoveGlobalScript(script);
			}

			Lua.ResetLeveltable();

			AudioManager.Stop();
			ParkAIController.Live.OnLevelExit();
			RegionController.OnLevelExit();
		}

		private void Update()
		{
			QualitySettings.vSyncCount  = 0;
			Application.targetFrameRate = TargetFPS;

			UpdateWorldState();

			KinematicCharacterSystem.AutoSimulation = !IsWorldPaused;

			// WARPING
			// ----------------------------------------
			if (ActorController.playerActor != null)
			{
				if (StateApp == AppState.InGame && StateLevel == LevelState.Loaded)
				{
					switch (StateGame)
					{
						case GameState.WarpOut:
						{
							WarpTimer -= Time.deltaTime;

							if (WarpTimer <= 0)
							{
								bool panic_to_spawn = true;

								if (_currentWarp.HasValue)
								{
									WarpInstructions ins = _currentWarp.Value;

									if (ins.type == WarpTargetType.Level)
									{
										if (_currentWarp.Value.target_level == null)
										{
											if (ins.target_reciever_id.HasValue)
											{
												StateGame = GameState.MidWarp;
												WarpTimer = 10;
												OnMidWarp?.Invoke();

												if (ActorController.Live.WarpTo(ins.target_reciever_id.Value))
													panic_to_spawn = false;
											}
										}
										else
										{
											StateGame  = GameState.WarpIn;                                 // Should stay this way through level changes
											WarpTimer  = ins.fade_time.GetValueOrDefault((0, 0)).out_time; // Reset timer for warp in use
											_warpInLock = false;
											ChangeLevel(ins.target_level, ins.target_reciever_id.GetValueOrDefault(-1));
											panic_to_spawn = false;
										}
									}
									else if (ins.type == WarpTargetType.Scene && ins.target_scene.IsValid)
									{
										LevelToMenu(ins.target_scene);
										panic_to_spawn = false;
									}
								}

								if (panic_to_spawn)
								{
									DespawnToSpawnMenu(true);
									GameEffects.FadeIn(0);
									WarpTimer = 0;
								}
							}
						}
							break;

						case GameState.WarpIn:
						{
							if (!_warpInLock)
							{
								GameEffects.FadeIn(_currentWarp.Value.fade_time.GetValueOrDefault((0, 0)).in_time);

								_warpInLock = true;
								var brain = ActorController.Live.TransitionBrain;

								if (brain.actor)
									brain.actor.LeaveArea(1, 0);
							}

							WarpTimer -= Time.deltaTime;
							DebugLogger.Log(WarpTimer.ToString(), LogContext.Overworld, LogPriority.Temp);

							if (WarpTimer <= 0 && ActorController.Live.TransitionBrain.controlling == null)
							{
								StateGame    = GameState.Overworld;
								_currentWarp = null;
							}
						}
							break;

						// NOTE: WarpTimer counts by frames here.
						case GameState.MidWarp:
						{
							WarpTimer--;
							if (WarpTimer <= 0)
							{
								StateGame  = GameState.WarpIn;                                                   // Should stay this way through level changes
								WarpTimer  = _currentWarp.Value.fade_time.GetValueOrDefault((0, 0)).in_time / 3; // Reset timer for warp in use
								_warpInLock = false;
								OnEndMidWarp?.Invoke();
							}
						}
							break;
					}
				}
			}

			if (StateGame == GameState.Minigame && CurrentMinigame == null)
			{
				EndMinigame();
			}

			if (!IsWorldPaused && StateLevel == LevelState.Loaded)
			{
				GlobalSyncTimer += Time.deltaTime;
			}

			// Until we implement the system menu
			if (GameInputs.IsPressed(Key.F3))
				Application.Quit();

			//#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// DEBUG INPUTS
			// ----------------------------------------

			// we should add a hidden option to option.ini to re-enable these features, could come in handy one of these days!
			// players also like to discover these dev features and toy with them
			if (GameInputs.IsPressed(Key.Backquote) || GameInputs.DebugPressed && Gamepad.current?.buttonNorth?.wasPressedThisFrame == true)
			{
				DebugMode = !DebugMode;
				// GameInputs.forceUnlocks.Set("debug_mode", DebugMode);
				//GameInputs.inputDisables.Set("debug_mode", DebugMode);
			}

			//Time.fixedDeltaTime = _startedFixedDT * Time.timeScale;


			if (StateGame == GameState.Battle) { }

			if (!LuaConsole.Open && !DebugConsole.Live.Opened)
			{
				if (GameInputs.IsShortcutPressed(Key.L, Key.LeftCtrl)) ExitGameplayToMenu(GameAssets.Live.DebugLevelSelectMenuScene);
				if (GameInputs.IsPressed(Key.F1)) DespawnToSpawnMenu();
				if (GameInputs.IsPressed(Key.F2)) GameOptions.current.debug_ui_on_startup.Toggle();
				if (GameInputs.InputsEnabled && GameInputs.IsPressed(Key.Digit9) || GameInputs.DebugDown && GameInputs.menuLeft.AbsorbPress()) cycleSpawn(-1);
				if (GameInputs.InputsEnabled && GameInputs.IsPressed(Key.Digit0) || GameInputs.DebugDown && GameInputs.menuRight.AbsorbPress()) cycleSpawn(1);
			}

			if (DebugMode)
			{
				if (GameInputs.IsShortcutPressed(Key.Digit1)) DebugSystem.ToggleMenu(GameOptions.DBG_NAME);
				if (GameInputs.IsShortcutPressed(Key.Digit2)) DebugSystem.ToggleMenu(SaveManager.DBG_NAME);
				if (GameInputs.IsShortcutPressed(Key.Digit3)) DebugSystem.ToggleMenu(Flags.DBG_NAME);
				if (GameInputs.IsShortcutPressed(Key.Digit4)) DebugSystem.ToggleMenu(BattleController.DBG_NAME);

				if(_lastCheckpointTouched) {
					Vector3 pos = _lastCheckpointTouched.transform.position;
					Draw.ingame.Arrow(pos   + Vector3.up * 2, pos, Color.green);
					Draw.ingame.Label2D(pos + Vector3.up * 2.15f, "Last Checkpoint", 20, LabelAlignment.BottomCenter, Color.green);
				}

			}
			//#endif

			// Note(C.L. 12-9-22): IDK if this should be here or somewhere else
			if (_lastCheckpointTouched && _lastCheckpointTouched.BoundingCollider && ActorController.playerActor is PlayerActor plr) {

				// TODO(C.L.): AHHHHHHHHHHHH
				var overlaps = Physics.ComputePenetration(plr.Motor.Capsule, plr.Motor.transform.position, plr.Motor.transform.rotation, _lastCheckpointTouched.BoundingCollider, _lastCheckpointTouched.BoundingCollider.transform.position,
														  _lastCheckpointTouched.BoundingCollider.transform.rotation, out Vector3 dir, out float dist);

				if (!overlaps) {
					_lastCheckpointTouched = null;
				}
			}

			// Activate debug camera
			if (GameInputs.InputsEnabled && GameInputs.IsPressed(Key.F7))
			{
				switch (ControlRoute)
				{
					case PlayerControlRoute.Player:
						ControlRoute = PlayerControlRoute.DebugCam;
						DebugCamera.Live.Activate();
						break;

					case PlayerControlRoute.DebugCam:
						ControlRoute = PlayerControlRoute.Player;
						DebugCamera.Live.Deactivate();
						break;
				}
			}
		}

		public static void CallLuaOnActivate(bool during_load = false)
		{
			Lua.InvokeGlobal("on_activate", new[] { (object)(Live.StateLevel != LevelState.Loaded) }, true);
		}

		private void LateUpdate()
		{
			if (DebugStepForwards) {
				DebugStepForwards = false;
				Time.timeScale    = (DebugPause && !DebugStepForwards) ? 0 : 1;
				//DebugStepForwardsFixed = false;
			}


			if (GameInputs.IsPressed(Key.F6)) {
				DebugPause     = !DebugPause;
				Time.timeScale = (DebugPause && !DebugStepForwards) ? 0 : 1;
			}

			if (DebugPause) {
				if(!DebugStepForwards) {
					if (GameInputs.IsPressed(Key.Digit6)) {
						DebugStepForwards = true;
						Time.timeScale    = (DebugPause && !DebugStepForwards) ? 0 : 1;
						//DebugStepForwardsFixed = true;
					}
				}
			}

			//Time.fixedDeltaTime = _startingFixedDT * (DebugPause && !DebugStepForwardsFixed ? 0 : 1);
		}

		public static void ResetCheckpoint() => Live._lastCheckpointTouched = null;
		public static void PlayerEnterCheckpoint(Checkpoint point)
		{
			Live._lastCheckpointTouched = point;
		}


		// Note(C.L. 11-19-22): "Death" in this context means any time we need to
		public static void TriggerOverworldDeath(OverworldDeathConfig config, VoidOutZone zone = null) => Live._triggerOverworldDeath(config, zone);
		private void _triggerOverworldDeath(OverworldDeathConfig config, VoidOutZone zone = null)
		{
			if (StateGame == GameState.OverworldDeath) return;

			if(IsMinigame && CurrentMinigame.OverworldDeathEnabled)
				CurrentMinigame.ModifyDeathConfig(ref config);
			else if (!IsPlayerControlled)
				return;

			StateGame = GameState.OverworldDeath;
			_overworldDeathCoroutine(config, IsMinigame ? GameState.Minigame : GameState.Overworld, zone).ForgetWithErrors();
		}

		private async UniTask _overworldDeathCoroutine(OverworldDeathConfig config, GameState next, VoidOutZone zone = null)
		{
			ActorController.LockStablePosition = true;

			if(ActorController.playerActor is PlayerActor plr) {
				switch (config.playerBehaviour) {

					case OverworldDeathConfig.PlayerBehaviour.Knockback:
						if(zone && zone.TryGetComponent(out Collider collider)) {
							plr.OnStun((collider.bounds.center - plr.transform.position) * config.KnockbackForce);
						}
						break;

					default:
						plr.ChangeState(plr.GetDefaultState());
						break;
				}
			}

			await UniTask2.Seconds(config.time.ValueOrDefault(0.25f));
			await GameEffects.FadeOut(config.transitionTime.ValueOrDefault(0.75f)).ToUniTask();
			ActorController.DespawnParty();
			await UniTask2.Seconds(0.35f);

			switch (config.mode) {
				case OverworldDeathConfig.Mode.LastValidGrounding:
					if	 (LastStableGroundPos())	{}
					else DefaultSpawnpointOrMenu();
					break;

				case OverworldDeathConfig.Mode.LastCheckpointOrValidGrounding:
					if		(LastCheckpoint())		{}
					else if (LastStableGroundPos()) {}
					else	DefaultSpawnpointOrMenu();

					break;
				case OverworldDeathConfig.Mode.LastCheckpoint:
					if		(LastCheckpoint())		{}
					else	DefaultSpawnpointOrMenu();

					break;

				case OverworldDeathConfig.Mode.SpecificSpawnPoint:
					if (config.spawn)
						SpawnParty(config.spawn);
					else
						DefaultSpawnpointOrMenu();

					break;
			}

			ActorController.LockStablePosition = false;

			await GameEffects.FadeIn(0.5f).ToUniTask();


			StateGame = next;

			//==============================================================================

			bool LastCheckpoint() {
				if(_lastCheckpointTouched && _lastCheckpointTouched.SpawnPoint) {
					SpawnParty(_lastCheckpointTouched.SpawnPoint);
					return true;
				}
				return false;
			}

			bool LastStableGroundPos() {
				if(ActorController.LastStableStandingPosition.HasValue) {
					SpawnParty(ActorController.LastStableStandingPosition.Value, ActorController.LastStablePlayerFacingDirection ?? Vector3.forward);
					return true;
				}
				return false;
			}

			void DefaultSpawnpointOrMenu() {
				if (ActiveLevel && ActiveLevel.DefaultSpawnpoint) {
					SpawnParty(ActiveLevel.DefaultSpawnpoint);
				} else {
					// TODO(C.L.): Even if the player is never expected to see this happen, we still need to handle this so we don't go to the spawn menu.
					Debug.LogError("Overworld Death: No default spawn point set for level. Cannot fall back to spawning the player there.");
					DespawnToSpawnMenu(true);
				}
			}
		}


		private void FixedUpdate()
		{
			//Debug.Log("FIXED UPDATE " + Time.fixedDeltaTime);

			/*if (DebugStepForwardsFixedFlag) {
				DebugStepForwardsFixed     = true;
				DebugStepForwardsFixedFlag = false;
				Time.fixedDeltaTime        = _startingFixedDT * (DebugPause && !DebugStepForwardsFixed ? 0 : 1);
				Debug.Log("STEP FORWARDS FIXED " + Time.fixedDeltaTime);
			} else {
				DebugStepForwardsFixed = false;
			}*/

			//Time.fixedDeltaTime = _startingFixedDT * (DebugPause && !DebugStepForwardsFixed ? 0 : 1);
		}

		//===========================================================================================================//
		//	STARTUP																								 //
		//===========================================================================================================//

		/// <summary>
		/// It's assumed that you're either loading into a menu scene, or a scene that will eventually load into a menu scene (company logos).
		/// </summary>
		/// <param name="firstSceneToLoad"></param>
		public void StartupNormal(SceneReference firstSceneToLoad)
		{
			DebugLogger.Log("GameStartup_Normal", LogContext.Core, LogPriority.High);
			StateApp = AppState.Startup;

			StateGame        = GameState.PreSpawn;
			StateLevel       = LevelState.NothingLoaded;
			State_WorldPause = WorldPauseState.Running;

			GameSceneLoader.LoadScene(firstSceneToLoad);
		}

		/// <summary>
		/// Call this to load right into a menu scene (main menu, debug level select ect)
		/// </summary>
		/// <param name="menuScene"></param>
		public void StartupMenu(SceneReference menuScene)
		{
			DebugLogger.Log("GameStartup_Menu", LogContext.Core, LogPriority.High);
			StateApp = AppState.Menu;

			StateGame        = GameState.PreSpawn;
			StateLevel       = LevelState.NothingLoaded;
			State_WorldPause = WorldPauseState.Running;

			GameSceneLoader.LoadScene(menuScene);
		}

		/// <summary>
		/// Start playing the game in a specified level.
		/// </summary>
		public void StartGameplay(LevelManifest firstLevel)
		{
			if (StateApp == AppState.InGame) return;

			StateApp = AppState.InGame;
			ChangeLevel(firstLevel);
		}

		public void DespawnToSpawnMenu(bool force = false)
		{
			if (!force && (StateGame == GameState.PreSpawn || StateApp != AppState.InGame))
				return;

			ActorController.EnsureDespawned();
			StateGame      = GameState.PreSpawn;
			StateSpawnMenu = SpawnMenuState.SelectSpawnPoint;

			GameEffects.FadeIn(0);

		}

		public void UnloadLevel()
		{
			leaveLevel(ActiveLevel);
			_unloadHandle = GameSceneLoader.UnloadLevel(ActiveLevel);
			StateLevel   = LevelState.Unloading;
		}

		// Call this for a normal game start. A first scene to load is of course required.
		private void LoadLevel(LevelManifest manifest)
		{
			Resources.UnloadUnusedAssets();
			GameEffects.FadeOut(1);
			_loadHandle = GameSceneLoader.LoadFromManifest(manifest);
			StateLevel = LevelState.Loading;
		}

		private void LoadScene(SceneReference scene)
		{
			Resources.UnloadUnusedAssets();
			GameEffects.FadeIn(0);
			_loadHandle = GameSceneLoader.LoadScene(scene);
			StateLevel = LevelState.NothingLoaded;
		}

		[LuaGlobalFunc("game_go_to_menu")]
		public static void GoToMenu(SceneReference menuScene) => Live.goToMenu(menuScene);

		private void goToMenu(SceneReference menuScene)
		{
			// We are going to a menu
			StateApp  = AppState.Menu;
			StateGame = GameState.PreSpawn;
			OnBeforeLeaveLevel?.Invoke(ActiveLevel);

			StateLevel = LevelState.Unloading;

			// We are loading a menu scene of some type
			_currentWarp = new WarpInstructions
			{
				type         = WarpTargetType.Scene,
				target_scene = menuScene,
			};

			/*_nextSceneToLoad = menuScene;
			_nextToLoadDir   = NextToLoad.Scene;*/
		}

		public void ExitGameplayToMenu(SceneReference menuScene)
		{
			if (StateApp != AppState.InGame ||
			    StateLevel != LevelState.Loaded ||
			    StateGame != GameState.Overworld && StateApp != AppState.InGame && StateGame != GameState.PreSpawn) return;

			// We are going to a menu
			StateApp  = AppState.Menu;
			StateGame = GameState.PreSpawn;

			UnloadLevel();            // We're leaving the current level
			OnExitGameplay?.Invoke(); // We're also exiting gameplay

			// We are loading a menu scene of some type
			_currentWarp = new WarpInstructions
			{
				type         = WarpTargetType.Scene,
				target_scene = menuScene,
			};
		}

		public void GoToOtherMenu(SceneReference menuScene, Scene? currentScene)
		{
			if (StateApp != AppState.Menu) return;

			if (currentScene.HasValue && currentScene.Value.IsValid())
				SceneManager.UnloadSceneAsync(currentScene.Value);

			SceneManager.LoadSceneAsync(menuScene, LoadSceneMode.Additive);
		}

		// A warpID of -1 will just go to the spawn menu.
		public void ChangeLevel(LevelManifest targetLevel, int warpID = -1)
		{
			ChangeLevel(new WarpInstructions
			{
				type               = WarpTargetType.Level,
				target_level       = targetLevel,
				target_reciever_id = warpID,
			});
		}

		public void ChangeLevel(WarpInstructions warp)
		{
			if (warp.target_level == null || IsLevelLoading) return;

			_currentWarp = warp;

			// Start unloading previous level if applicable

			if (IsInLevel)
			{
				OnExitGameplay?.Invoke();
				leaveLevel(ActiveLevel);

				_unloadHandle = GameSceneLoader.UnloadLevel(ActiveLevel);
				StateLevel   = LevelState.Unloading;
			}
			else
			{
				LoadLevel(warp.target_level);
			}
		}

		public void LevelToMenu(SceneReference menuScene)
		{
			if (IsInLevel)
			{
				OnExitGameplay?.Invoke();
				leaveLevel(ActiveLevel);

				_unloadHandle = GameSceneLoader.UnloadLevel(ActiveLevel);
				StateLevel   = LevelState.Unloading;
			}
			else
			{
				LoadScene(menuScene);
			}
		}

		//public bool DoWarp(LevelManifest targetLevel, int warpID = -1, (float outTime, float inTime)? fadeTime = null)
		public bool DoWarp(WarpInstructions warp)
		{
			if (StateGame != GameState.Overworld || !warp.valid)
				return false;

			_currentWarp = warp;

			// Instant
			if (warp.fade_time == null)
			{
				if (warp.target_level == null)
				{
					// Do an in-level warp
					ActorController.Live.WarpTo(warp.target_reciever_id.GetValueOrDefault(WarpReceiver.NULL_WARP));
				}
				else
				{
					// Change the level, warping to the selected warp point in said level
					ChangeLevel(warp.target_level, warp.target_reciever_id.GetValueOrDefault(WarpReceiver.NULL_WARP));
				}
			}
			else
			{
				(float outTime, float inTime) time = warp.fade_time.Value;
				Assert.IsTrue(time.inTime >= 0 && time.outTime >= 0);
				WarpTimer = time.outTime;
				//FadeTime         = time;
				StateGame = GameState.WarpOut;
				//_spawnReceiver   = warpID;
				//_targetWarpLevel = targetLevel;
				GameEffects.FadeOut(time.outTime);
			}

			return true;
		}

		async void loadArenaAsync(SceneReference scene, SceneGroup parent)
		{
			SceneLoadHandle handle = GameSceneLoader.LoadScene(scene, parent: parent);

			await UniTask.WaitUntil(() => handle.Status.IsDoneOrError);
			if (handle.Status.state == InstructionStatus.State.Error)
			{
				DebugLogger.LogError($"Arena {scene.SceneName} failed to load.", LogContext.Combat | LogContext.Core, LogPriority.Critical);
				return;
			}

			handle.LoadedGroup.SetRootObjectsActive(false);
		}

	#region State Transitions

		public bool BeginCutscene(Cutscene cutscene)
		{
			if ((StateGame != GameState.Overworld && StateGame != GameState.Cutscene) || StateLevel != LevelState.Loaded) return false;

			//OverworldHUD.HideAll(false);

			StateGame           = GameState.Cutscene;
			ControllingCutscene = cutscene;

			//AudioManager.Stop(false);
			return true;
		}

		public void EndCutscene()
		{
			if (StateGame != GameState.Cutscene) return;
			StateGame           = GameState.Overworld;
			ControllingCutscene = null;

			//OverworldHUD.Live.ReturnUIState();

			if (ActorController.playerActor.TryGetComponent(out EncounterPlayer eplayer))
			{
				eplayer.AddImmunityWithoutFlash(3);
			}

			//AudioManager.Play();
		}

		public bool BeginMinigame(Minigame minigame)
		{
			if (minigame == null || CurrentMinigame != null ||
			    StateGame != GameState.Overworld || StateLevel != LevelState.Loaded) return false;

			//OverworldHUD.HideAll(false);

			StateGame       = GameState.Minigame;
			CurrentMinigame = minigame;
			return true;
		}

		public void EndMinigame()
		{
			if (StateGame != GameState.Minigame) return;

			StateGame       = GameState.Overworld;
			CurrentMinigame = null;

			//OverworldHUD.Live.ReturnUIState();

			if (ActorController.playerActor.TryGetComponent(out EncounterPlayer eplayer))
			{
				eplayer.AddImmunityWithoutFlash(3);
			}

			//AudioManager.Play();
		}

		public bool OnEnterBattle()
		{
			if (StateApp != AppState.InGame && StateGame == GameState.Overworld) return false;
			StateGame = GameState.Battle;

			return true;
		}

		public void OnBattleEnded()
		{
			if (StateApp != AppState.InGame || StateGame != GameState.Battle) return;
			StateGame = GameState.Overworld;
		}

		public bool CanControlPlayer()
		{
			return (IsPlayerControlled || (IsMinigame && CurrentMinigame.PlayerHasControl))
			       && !LuaConsole.Open
			       && MenuManager.activeMenus.Count == 0
			       && GameInputs.ActiveDevice != InputDevices.None;
		}


	#endregion

		/// <summary>
		/// Cycle the controlled actor to the nxet spawn point.
		/// </summary>
		/// <param name="offset"></param>
		private void cycleSpawn(int offset)
		{
			if (StateGame == GameState.PreSpawn || StateApp != AppState.InGame) return;

			var spawns = SpawnPoint.allActive
				.Where(sp => string.IsNullOrEmpty(sp.ScriptFunction) && !(sp is CutsceneSpawnPoint))
				.ToList();

			_spawnPoint = (_spawnPoint + offset) % spawns.Count;
			ActorController.TeleportPartyToSpawnpoint(spawns[_spawnPoint]);
		}

		// Debug
		//================================================================

		public void OnLayout(ref DebugSystem.State state)
		{
			if (StateApp == AppState.InGame && StateGame == GameState.PreSpawn && StateLevel == LevelState.Loaded)
			{
				GameInputs.mouseUnlocks.Add("spawn_menu");

				//TODO: figure out how to get music to play when the debug menu is visible and not trying to load into the intro cutscene
				//AudioManager.Play();

				int sw = Screen.width;
				int sh = Screen.height;

				int w = Mathf.Max(sw / 2, 500);
				int h = Mathf.Max(sh / 2, 300);

				int x = sw / 2 - w / 2;
				int y = sh / 2 - h / 2;

				/*ImGui.SetNextWindowPos(new Vector2(128, 280), ImGuiCond.Always);
				ImGui.SetNextWindowSize(new Vector2(600, 312), ImGuiCond.Always);*/

				g.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
				g.SetNextWindowSize(new Vector2(w, h), ImGuiCond.Always);

				if (g.Begin("Spawn Menu", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
				{
					if (g.BeginChild("controls", new Vector2(0, -g.GetFrameHeightWithSpacing())))
					{
						g.Columns(2);

						g.TextColored(ColorsXNA.Goldenrod.ToV4(), "Spawn Point:");
						for (var i = 0; i < SpawnPoint.allActive.Count; i++)
						{
							g.PushID(i);
							if (g.Selectable(SpawnPoint.allActive[i].SpawnPointName + ": " + SpawnPoint.allActive[i].transform.position + "###ID", i == _spawnPoint))
							{
								_spawnPoint = i;
							}

							g.PopID();
						}

						g.NextColumn();

						g.SetColumnWidth(-1, 200);

						g.TextColored(ColorsXNA.Goldenrod.ToV4(), "Character:");

						if (g.Selectable(GameAssets.Live.BaseSpawnableCharacter.name, SelectedCharacter == null) && SelectedCharacter != null)
						{
							SelectedCharacter = null;
						}

						foreach (Actor actor in GameAssets.Live.OtherSpawnableCharacters)
						{
							if (g.Selectable(actor.name, SelectedCharacter == actor) && SelectedCharacter != actor)
								SelectedCharacter = actor;
						}

						g.Columns(1);
					}

					g.EndChild();

					if (g.Button("Spawn"))
					{
						SpawnParty(SpawnPoint.allActive[_spawnPoint], SelectedCharacter);
						ParkAIController.Live.PerformTotalPeepRedistribution();
					}

					g.SameLine();

					if (g.Button("Go To Level Select"))
						ExitGameplayToMenu(GameAssets.Live.DebugLevelSelectMenuScene);
				}

				g.End();
			}
			else
			{
				GameInputs.mouseUnlocks.Remove("spawn_menu");
			}

			if (state.Begin("Coplayers"))
			{
				for (int i = 0; i < Coplayer.All.Count; i++)
				{
					Coplayer player = Coplayer.All[i];
					g.PushID(i);
					AImgui.Text($"{player.gameObject.GetNameWithPath()}: + {player.state.ToString()}");
					g.PopID();
				}
			}
		}

		[DebugRegisterGlobals]
		public static void RegisterMenu() { }


		// TODO: Add option for teleporting without party members/teleporting specific members.
	}
}
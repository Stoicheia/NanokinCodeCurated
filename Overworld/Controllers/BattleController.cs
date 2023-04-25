using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Audio;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Anjin.Utils;
using Cinemachine;
using Combat;
using Combat.Chips;
using Combat.Components;
using Combat.Components.WinLoseHandling;
using Combat.StandardResources;
using Combat.Startup;
using Combat.Toolkit;
using Combat.UI;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Park_Game;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Util.Components.Cinemachine;
using Util.Components.Timers;
using g = ImGuiNET.ImGui;

namespace Overworld.Controllers
{
	public class BattleController : StaticBoy<BattleController>, IDebugDrawer
	{
		public const string DBG_NAME = "Battle Controller";

		public BattleConfig           BattleConfig;
		public BattleTransitionConfig TransitionConfig;

		public bool IsAnyEnemyAggroed;

		private ValTimer _introTimer;

		private BattleRunner _currentRunner;

		private CinemachineZoom _currentZoom;

		/*[Title("Design")]
		[SerializeField] public AudioDef EncounterStartupSound;
		[SerializeField] private AnimationCurve EncounterEntranceZoom;
		[SerializeField] private AnimationCurve EncounterEntranceLook;

		[SerializeField] public AnimationCurve EncounterEntranceSlowdown;
		[SerializeField] public AnimationCurve EncounterExitSlowdownEnemies;
		[SerializeField] public float          EncounterExitImmunity;*/

		protected override async void OnAwake()
		{
			base.OnAwake();

			DebugSystem.RegisterMenu(DBG_NAME);
			DebugSystem.Register(this);

			await Addressables.InitializeAsync();
		}


		public void Start()
		{
			GameController.Live.OnFinishLoadLevel_2 += OnFinishLoadLevel_2;
		}

		[Button]
		public static void SetEncountersActive(bool b = true)
		{
			foreach (EncounterSpawner spawner in EncounterSpawner.All)
			{
				if (b) spawner.Enable();
				else spawner.Disable();
			}
		}

		private void OnFinishLoadLevel_2(Level level)
		{
			// Try to load the arena
			// if (level && !_areaLoaded && level.Manifest)
			// {
			// 	if (!level.Manifest.DefaultBattleArenaScene.IsInvalid)
			// 		_arenaHandle = GameSceneLoader.LoadScene(level.Manifest.DefaultBattleArenaScene);
			//
			// 	_loadingArena = true;
			// }
		}

		private void Update()
		{
			IsAnyEnemyAggroed = false;
			if (GameController.Live.StateGame == GameController.GameState.Overworld && ActorController.playerActor != null)
			{
				foreach (EncounterSpawner spawner in EncounterSpawner.All)
				{
					foreach (EncounterSpawner.SpawnedEncounter encounter in spawner.spawnedMonsters)
					{
						if (encounter.actor != null && encounter.actor is IEncounterActor enc && encounter.encounter.spawned)
							if (enc.IsAggroed)
								IsAnyEnemyAggroed = true;
					}
				}
			}
		}

		/// <summary>
		/// Create the base battle core with the game's logic.
		/// </summary>
		/// <param name="io"></param>
		public static BattleRunner CreateBaseBattle(BattleIO io)
		{
			var core = new BattleRunner
			{
				logInitialization = true,
				logVisuals        = GameOptions.current.log_combat_visuals,
				logInstructions   = GameOptions.current.log_combat_instructions,
				logTurns          = GameOptions.current.log_combat_turns,
				logState          = GameOptions.current.log_combat_state,
				logEmits          = GameOptions.current.log_combat_emit,
				animConfig            = Live.BattleConfig
			};

			core.io = io ?? core.io;

			core.initChips.Add(new FlowChip());
			core.initChips.Add(new WinLoseConditionRegular { transitionOnFinalDeathMarked = true });
			core.initChips.Add(new DeathAnimatorChip());
			core.initChips.Add(new ProcAnimator());
			core.initChips.Add(new CardIntroChip());
			core.initPlugins.Add(new BaseMechanicPlugin());

			return core;
		}

		/// <summary>
		/// Create a battle core in which the player participates, with features specific to the
		/// actual story-mode played in single-player.
		/// </summary>
		public static BattleRunner CreateGameBattle(BattleIO io = null)
		{
			var core = CreatePlayableBattle(io);
			core.initChips.Add(new TutorialChip());
			return core;
		}

		/// <summary>
		/// Create a battle core in which the player participates.
		/// </summary>
		/// <param name="battleIO"></param>
		/// <returns></returns>
		public static BattleRunner CreatePlayableBattle(BattleIO io = null)
		{
			BattleRunner runner = CreateBaseBattle(io);

			runner.initChips.Add(new ArenaIntroChip());

			// UI
			// ----------------------------------------
			runner.initChips.Add(new CameraChip());
			runner.initChips.Add(new DebugVisChip());

			// Gameplay
			// ----------------------------------------
			// core.initChips.Add(new LoseMenu());
			runner.initChips.Add(new VictoryUIChip());

			// Debugging tools
			// ----------------------------------------

			runner.initChips.Add(new UIChip());
			runner.initChips.Add(new ImGuiChip());
			runner.initChips.Add(new DebugKeysChip());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// core.initChips.Add(new InteropChip());
#endif

			return runner;
		}

		// Directly after any intro animation starts, but before an actual screen transition
		public static void OnEnterCombat1()
		{
			ActorController.LockPlayers(true);
			//OverworldHUD.HideAll(anim: false);
		}

		// During/directly after the screen transition
		public static void OnEnterCombat2()
		{
			ActorController.SetPartyActive(false);
			ParkAIController.Suspend();
			SetEncountersActive(false);
		}

		public static void OnExitCombat()
		{
			SetEncountersActive();
			ParkAIController.Resume();
			ActorController.SetPartyActive(true);
			ActorController.LockPlayers(false);
			//OverworldHUD.Live.ReturnUIState();
		}

		[Button, Title("Debug")]
		public static void RespawnEncounters()
		{
			foreach (EncounterSpawner spawner in EncounterSpawner.All)
			{
				spawner.Respawn();
			}
		}

		public static void DespawnEncounters()
		{
			foreach (EncounterSpawner spawner in EncounterSpawner.All)
			{
				spawner.Despawn();
			}
		}

		public static async UniTask LoadCombatScenes()
		{
			await UniTask.WhenAll(
				SceneLoader.GetOrLoadAsync(Addresses.Scene_UI_Combat),
				SceneLoader.GetOrLoadAsync(Addresses.Scene_UI_Triangle)
			);
		}

		public static async UniTask UnloadCombatScenes()
		{
			await SceneLoader.UnloadAsync(Addresses.Scene_UI_Combat);

			// Unloading the UI_Triangle causes major issues, see NANO-216
			// We have to keep it loaded for now.
			SceneActivator.Set(Addresses.Scene_UI_Triangle, false);
		}

		public struct EncounterLaunchConfig
		{
			// TODO
		}

		public static UniTask LaunchEncounter(Transform target, EncounterSettings settings, EncounterAdvantages advantage)
		{
			CombatTransitionSettings transition = CombatTransitionSettings.Default;
			transition.Target = target;

			if (settings.ArenaAddress.IsValid)
				return LaunchEncounter(settings.ArenaAddress, settings.Recipe.Value, transition, advantage);

			if (settings.Arena != null)
				return LaunchEncounter(settings.Arena, settings.Recipe.Value, transition, advantage);

			return UniTask.CompletedTask;
		}

		public static UniTask LaunchEncounter(EncounterMonster monster, EncounterSettings settings, EncounterAdvantages advantage)
		{
			CombatTransitionSettings transition = CombatTransitionSettings.Default;
			transition.Target = new WorldPoint(monster.transform, new Vector3(0, 0.75f, 0));
			transition.Enemy  = monster;

			if (settings.ArenaAddress.IsValid)
				return LaunchEncounter(settings.ArenaAddress, settings.Recipe.Value, transition, advantage);

			if (settings.Arena != null)
				return LaunchEncounter(settings.Arena, settings.Recipe.Value, transition, advantage);

			return UniTask.CompletedTask;
		}

		public static async UniTask<BattleOutcome> LaunchEncounter(ArenaReference arena,
			RecipeReference                                                       recipe,
			CombatTransitionSettings                                              transition,
			EncounterAdvantages                                                   advantage     = EncounterAdvantages.Neutral,
			AudioClip                                                             music         = null,
			bool                                                                  retryDisabled = false,
			bool                                                                  fleeDisabled  = false)
		{
			BattleTransitionConfig tConfig = Live.TransitionConfig;

			if (GameController.Live.StateGame == GameController.GameState.Battle)
				return BattleOutcome.None;

			Scene previousActiveScene = SceneManager.GetActiveScene();

			GameController.Live.OnEnterBattle();


			WorldPoint targetPoint = ActorController.playerActor.transform;

			if (transition.Target.HasValue)
			{
				targetPoint = transition.Target.Value;
			}

			Live.Log($"Start encounter battle from {transition.Enemy?.name ?? "none"} with '{recipe.Name}'", "TRACE", nameof(LaunchEncounter));

			// CREATE THE BATTLE
			// ----------------------------------------

			BattleRecipe _r = recipe.Get();

			BattleIO io = new BattleIO
			{
				recipe    = _r,
				music     = music,
				advantage = advantage,
				canRetry  = retryDisabled,
				canFlee   = !fleeDisabled,
			};

			BattleRunner runner = CreateGameBattle(io);

			Live._currentRunner = runner;

			AdvantagePlugin advantagePlugin = GetAdvantagePlugin(advantage);
			if (advantagePlugin != null)
				runner.initPlugins.Add(advantagePlugin);

			// TRANSITION INTO BATTLE
			// ----------------------------------------

			// Mute everything
			AudioZone muteZone = AudioManager.AddMute(AudioLayer.All, 0.30f, 50);

			// Slow down time
			TimeScaleVolume entranceSlowdown = TimeScaleVolume.Spawn("Encounter Slowdown", targetPoint.Get());

			// 1st part: Smash into screen
			GameSFX.PlayGlobal(tConfig.IntroSound, Live);
			entranceSlowdown.Tween(tConfig.EntranceSlowdown);

			GameEffects.PlayEncounterAdvantage(advantage);

			CombatIntroAnimationSettings animation = transition.Animation.GetValueOrDefault(tConfig.DefaultAnimation);

			switch (animation.Type)
			{
				case CombatIntroAnimation.ZoomToWorldPoint:

					AnimationCurve zoom = animation.ZoomCurve ?? tConfig.DefaultAnimation.ZoomCurve;
					AnimationCurve look = animation.ZoomLookCurve ?? tConfig.DefaultAnimation.ZoomLookCurve;

					/*float duration = Mathf.Max(
						zoom[zoom.length - 1].time,
						look[look.length - 1].time
					);*/

					if (animation.AdvanceTimeNorm > Mathf.Epsilon)
					{
						EncounterZoomAnimation(targetPoint, zoom, look, animation.Duration).ForgetWithErrors();
						await IntroTimer(animation.Duration * Mathf.Clamp01(animation.AdvanceTimeNorm));
					}
					else
					{
						await EncounterZoomAnimation(targetPoint, zoom, look, animation.Duration);
					}

					break;

				case CombatIntroAnimation.None: break;
			}

			OnEnterCombat1();


			// 2nd part: Launch with cutscene -------------

			// If we're missing the arena, set default for the enemy
			if (arena.IsNull)
			{
				EncounterSettings settings = Get(targetPoint.Get());
				arena = new ArenaReference(settings.Arena, settings.ArenaAddress);
			}

			await InitCombat(arena, runner, GameEffects.PlayHelixCutscene(), transition);
			muteZone.Layer = AudioLayer.Music;

			Destroy(entranceSlowdown);
			SplicerHub.DisableMenu();

			// Play out the battle
			runner.Play().ForgetWithErrors();
			await UniTask.WaitUntil(() => runner.step == BattleRunner.States.Zero);

			Live._currentRunner = null;

			// AFTER BATTLE
			// ----------------------------------------
			await GameEffects.FadeOut(0.5f);
			await UnloadCombatScenes();

			if (Live._currentZoom)
			{
				Live._currentZoom.ZoomValue      = 0;
				Live._currentZoom.OrientationFix = 0;
			}

			Live._currentZoom = null;

			// If the enemy was an encounter monster, despawn it
			if (transition.Enemy != null)
			{
				transition.Enemy.Despawn();
			}

			OnExitCombat();

			// Restore the scene that was active before this fight.
			// We do this instead of restoring the current level, because this function is designed to work without a loaded level.
			// MOST PROBABLY EXTREMELY LIKELY not needed, but hey it's not hard to do it that way so might as well have
			// make it a possibility, perhaps it could come in handy for testing, or re-using for netplay, etc.
			SceneManager.SetActiveScene(previousActiveScene);

			runner.arena.OnEnd();

			if (runner.arena.IsLoadedAtRuntime)
				runner.arena.gameObject.scene.SetRootActive(false);

			switch (runner.io.outcome)
			{
				case BattleOutcome.None:
				case BattleOutcome.Win:
				case BattleOutcome.Flee:
					if (!transition.NoPostImmunity && ActorController.playerActor.TryGetComponent(out EncounterPlayer eplayer))
					{
						eplayer.AddImmunity(tConfig.ExitImmunity);
					}

					break;

				case BattleOutcome.Lose:
					break;

				case BattleOutcome.LoseExit:
					GameController.Live.OnBattleDeath();
					break;

				case BattleOutcome.LoseRetry:
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(runner.io.outcome), "Unhandled battle outcome");
			}

			// Freeze enemies with tweening back to normal
			var timeScale = TimeScaleVolume.Spawn("Encounter Grace Frames", ActorController.playerActor.Position, 50, Layers.Enemy.mask);
			timeScale.TweenAndDestroy(tConfig.ExitEnemySlowdown);

			if (transition.Lua_BeforeReturn != null)
			{
				transition.Lua_BeforeReturn.Call();
				UniTask.DelayFrame(2);
			}

			AudioManager.RemoveZone(muteZone);
			await GameEffects.FadeIn(0.5f);

			GameController.Live.OnBattleEnded();

			return runner.io.outcome;
		}

		public static async void OnExternalBattleLaunched(BattleRunner runner)
		{
			if (runner == null || runner.step == BattleRunner.States.Stopping)
				return;

			Live._currentRunner = runner;
			GameController.Live.OnEnterBattle();
			await UniTask.WaitUntil(() => runner.step == BattleRunner.States.Zero);
			GameController.Live.OnBattleEnded();
			Live._currentRunner = null;
		}

		[CanBeNull]
		private static AdvantagePlugin GetAdvantagePlugin(EncounterAdvantages advantage)
		{
			PlayerAlignments alignment = advantage.ToAlignment();
			return new TurnAdvantage(alignment);

			//if (alignment == PlayerAlignments.Ally)
			//{
			//	switch (RNG.Range(1, 2))
			//	{
			//		case 1: //return new HealthAdvantage(alignment);
			//		case 2: return new TurnAdvantage(alignment);
			//	}
			//}
			//else if (alignment == PlayerAlignments.Enemy)
			//{
			//	switch (RNG.Range(1, 2))
			//	{
			//		case 1:
			//		case 2: return new TurnAdvantage(alignment);
			//	}
			//}

			//return null;
		}

		/// <summary>
		/// Initialize combat, but does not launch it.
		/// - Loads combat scenes
		/// - Loads the arena
		/// - Init the battle
		/// </summary>
		/// <param name="arenaAddress"></param>
		/// <param name="battle"></param>
		/// <param name="cutsceneTask"></param>
		/// <returns></returns>
		private static async UniTask InitCombat(ArenaReference arenaref, BattleRunner battle, UniTask cutsceneTask, CombatTransitionSettings transition)
		{
			cutsceneTask.Preserve();

			// TODO support arenaref direct arena reference

			Application.backgroundLoadingPriority = ThreadPriority.High;
			await LoadCombatScenes(); // Combat UI needs to be loaded to handle certain things on battle init.
			await LoadArenaAndInitBattle(arenaref, battle);
			Application.backgroundLoadingPriority = ThreadPriority.Normal;


			// Wait for the helix if it's still going (all loading finished first!)
			await cutsceneTask;

			Arena arena = battle.io.arena;

			arena.OnInit();

			if (arena.IntroParams != null)
				arena.IntroParams.SetInitialState(); // Without this we'll see the scene camera in its regular position briefly

			arena.gameObject.scene.SetRootActive(true);
			SceneManager.SetActiveScene(arena.gameObject.scene);

			arena.OnBegin();

			OnEnterCombat2();

			if (transition.Lua_AfterIntro != null)
			{
				transition.Lua_AfterIntro.Call();
				UniTask.DelayFrame(2);
			}

			// Fade in while the combat starts playing. (the helix leaves the screen faded out)
			GameEffects.FadeIn(0.65f);
		}

		public static async UniTask IntroTimer(float duration)
		{
			Live._introTimer.Set(duration);
			while (!Live._introTimer.Tick())
			{
				await UniTask.NextFrame();
			}
		}

		public static async UniTask EncounterZoomAnimation(WorldPoint? towards, AnimationCurve zoomCurve, AnimationCurve lookCurve, float duration)
		{
			CinemachineVirtualCamera vcam = (CinemachineVirtualCamera)GameCams.Live.Brain.ActiveVirtualCamera;
			Live._currentZoom = vcam.AddComponent<CinemachineZoom>();
			var zoom = Live._currentZoom;

			zoom.NormalizeDistance = true;
			zoom.StartPosition     = vcam.State.FinalPosition;

			float progress   = 0;
			bool  hasTowards = towards != null;

			while (progress < duration)
			{
				progress += Time.deltaTime;

				zoom.towardsPoint = hasTowards ? towards.Value.Get(ActorController.playerActor.Position) : ActorController.playerActor.Position;

				// Update the animation
				zoom.ZoomValue      = zoomCurve.Evaluate(progress / duration);
				zoom.OrientationFix = lookCurve.Evaluate(progress / duration);

				//Debug.Log($"{progress}/{duration} = {progress/duration}");

				await UniTask.NextFrame(); // Sick!!
			}

			Destroy(zoom);
		}


		public static async UniTask LoadArenaAndInitBattle(ArenaReference arenaref, BattleRunner battle)
		{
			Arena arena = arenaref.direct;

			if (arenaref.IsNull)
			{
				Debug.LogError("No arena for combat. The game will explode momentarily.");
				return;
			}

			if (arenaref.IsScene)
			{
				Scene scene = SceneManager.GetSceneByName(arenaref.address);

				if (!scene.isLoaded)
					scene = await SceneLoader.GetOrLoadAsync(arenaref.address);

				arena = scene.FindRootComponent<Arena>();

				arena.IsLoadedAtRuntime = true;
			}

			arena.VCam.Priority = 100;
			battle.io.arena     = arena;

			await battle.InitTask();
		}

		public static void OnBattleOutcome(BattleOutcome outcome) { }

		public static void ArenaConfig(string name, Table config) { }

		// private static void OnCoreStopping(BattleCore core)
		// {
		// 	// TODO
		// 	core.Stopping -= OnCoreStopping;
		// 	PrefabPool.Return(core.view.gameObject);
		// }

		// /// <summary>
		// /// Unload the arena that was loaded. (nothing if it was already loaded)
		// /// </summary>
		// /// <returns></returns>
		// public async UniTask UnloadArena(string address)
		// {
		// 	if (transitioner.arenaAddress.IsValid)
		// 	{
		// 		if (!_alreadyLoaded)
		// 		{
		// 			await SceneLoader.UnloadAsync(transitioner.arenaAddress);
		// 		}
		// 		else
		// 		{
		// 			SceneManager.GetSceneByName(transitioner.arenaAddress).SetRootActive(false);
		// 		}
		//
		// 		SceneManager.SetActiveScene(SceneManager.GetSceneByName(_activeSceneToRestore));
		// 	}
		// }


		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.Begin(DBG_NAME))
			{
				g.TextColored(ColorsXNA.Goldenrod.ToV4(), $"GameController.Live.IsInBattle: {GameController.Live.IsInBattle}");
				g.TextColored(ColorsXNA.Goldenrod.ToV4(), $"Current BattleCore:			 {_currentRunner ?? (object)"none"}");

				if (!GameController.Live.IsInBattle)
				{
					g.Text("Encounters: ");
					g.SameLine();
					if (g.Button("Activate")) SetEncountersActive();
					g.SameLine();
					if (g.Button("Deactivate")) SetEncountersActive(false);
					g.SameLine();
					if (g.Button("Despawn")) DespawnEncounters();
					g.SameLine();
					if (g.Button("Respawn")) RespawnEncounters();
				}

				g.Separator();
				if (g.BeginTabBar("tabs"))
				{
					if (g.BeginTabItem("Main"))
					{
						g.EndTabItem();
					}

					if (g.BeginTabItem("Brains"))
					{
						g.EndTabItem();
					}

					if (g.BeginTabItem("Tests"))
					{
						AITest();
						g.EndTabItem();
					}

					g.EndTabBar();
				}


				g.End();
			}
		}

		void AITest()
		{
			decisions.Clear();
			ImDrawListPtr draw = g.GetWindowDrawList();

			g.DragFloatRange2("Horizontal", ref grid_x1, ref grid_x2, 0.1f, -50, 50);
			g.DragFloatRange2("Vertical", ref grid_y1, ref grid_y2, 0.1f, -50, 50);

			// From the perspective of A1:
			Vector2 pos = g.GetCursorScreenPos();

			Vector2     gpos       = pos + new Vector2(32, 0);
			const float gwidth     = 400;
			const float gheight    = 360;
			const int   hDivisions = 15;
			const int   vDivisions = 15;

			float h_healing(float hp_percent)
			{
				if (hp_percent <= Mathf.Epsilon || hp_percent > 0.9f) return 0;
				return 1 - hp_percent;
			}

			float h_dmg(float hp_percent) => hp_percent;

			float x1 = grid_x1,
				x2   = grid_x2,
				y1   = grid_y1,
				y2   = grid_y2;

			DrawGrid(gwidth, gheight);


			DrawCurve(h_dmg, Color.red);
			DrawCurve(h_healing, Color.green);
			/*DrawCurve(Mathf.Exp, Color.green);
			DrawCurve(Mathf.Log, Color.blue);*/

			void DrawCurve(Func<float, float> function, Color col)
			{
				const int div = 300;

				Vector2 get_pos(float val)
				{
					float xval = Mathf.Lerp(x1, x2, val);
					float yval = Mathf.Clamp(function(Mathf.Lerp(y1, y2, val)), y1, y2);
					return gpos + new Vector2((xval / (x2 - x1)) * gwidth /*+ gwidth / 2*/, gheight - (yval / (y2 - y1)) * gheight /*- gheight/2*/);
				}

				for (int i = 0; i < div - 1; i++)
				{
					Vector2 p1 = get_pos(Mathf.Clamp01(i / (float)div));
					Vector2 p2 = get_pos(Mathf.Clamp01((i + 1) / (float)div));

					draw.AddLine(p1, p2, col.ToUint());
				}
			}

			void DrawGrid(float width, float height)
			{
				Color col = ColorsXNA.Goldenrod;
				for (int y = 1; y < hDivisions; y++)
				{
					Vector2 bp = gpos + new Vector2((width / hDivisions) * y, 0);
					draw.AddText(bp + new Vector2(0, height), Color.white.ToUint(), Mathf.Lerp(x1, x2, (y / (float)hDivisions)).ToString("#.##"));

					draw.AddLine(bp, bp + new Vector2(0, height), col.ScaleAlpha(0.4f).ToUint());
				}

				for (int y = 1; y < vDivisions; y++)
				{
					Vector2 bp = gpos + new Vector2(0, (height / vDivisions) * y);

					draw.AddText(bp - new Vector2(24, 0), Color.white.ToUint(), Mathf.Lerp(y1, y2, (1 - (float)y / hDivisions)).ToString("#.##"));
					draw.AddLine(bp, bp + new Vector2(width, 0), col.ScaleAlpha(0.4f).ToUint());
				}


				/*Vector2 p = gpos  + new Vector2((width / hDivisions) * 0.5f, 0);
				draw.AddLine(p, p + new Vector2(0, (height / vDivisions) * 0.5f), ColorsXNA.OrangeRed.ToUint());*/

				draw.AddRect(gpos, gpos + new Vector2(width, height), col.ToUint());
			}
		}


		// Test Code
		//------------------------------
		private float grid_x1 = 0, grid_x2 = 1;
		private float grid_y1 = 0, grid_y2 = 1;

		class aifighter
		{
			public float hp;
		}

		enum action { heal, attack }

		struct decision
		{
			public aifighter target;
			public action    action;
		}

		private List<aifighter> team_a = new List<aifighter>
		{
			new aifighter { hp = 1f },
			new aifighter { hp = .9f },
			new aifighter { hp = .2f },
		};

		private List<aifighter> team_b = new List<aifighter>
		{
			new aifighter { hp = .53f },
			new aifighter { hp = .4f },
			new aifighter { hp = 1f },
		};

		private List<decision> decisions = new List<decision>();

		//------------------------------

		public static EncounterSettings Get(Vector3 pos)
		{
			return new EncounterSettings
			{
				Recipe        = EncounterLayer.GetRecipe(pos),
				MonsterPrefab = EncounterLayer.GetMonster(pos),
				ArenaAddress  = EncounterLayer.GetArena(pos),
			};
		}
	}
}
using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using Cinemachine;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Util.Odin.Attributes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ShortcutManagement;

#endif

namespace Overworld.Cutscenes
{
	/// <summary>
	/// A wrapper around a coroutine player
	/// with extended functionalities.
	/// </summary>
	[DefaultExecutionOrder(1)] // This is required for the cutscene to play after CutsceneBrain
	public class Cutscene : SerializedMonoBehaviour, ILuaObject
	{
		public bool                       ControlsGame     = true;
		public bool                       ControlsCamera   = true;
		public CinemachineBlendDefinition DefaultVCamBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
		public int                        BrainPriorityMod = 0;
		public int                        StartDelayFrames = 0;

		[Title("Lua")]
		[LabelText("Script")]
		[FormerlySerializedAs("CutsceneScript")]
		[OnValueChanged("OnScriptChanged")]
		public LuaAsset Script;

		[Optional]
		public string GlobalFunctionName;

		[HideInInspector]
		[NonSerialized]
		[OdinSerialize]
		// ReSharper disable once InconsistentNaming
		public ScriptStore ScriptStore = new ScriptStore();

		// Runtime
		// ----------------------------------------
		[NonSerialized] public Coplayer  coplayer;
		[NonSerialized] public Table     runningScript;
		[NonSerialized] public Cutscene  parent;
		[NonSerialized] public bool      playing;
		[NonSerialized] public bool      loading;
		[NonSerialized] public AsyncLazy initTask;
		[NonSerialized] public Closure   mainClosure;


		private       CoroutineInstance _initInstance;
		private       bool              _scheduledReload;
		private       VCamTarget        _vcamTarget;

		LuaAsset ILuaObject.Script => Script;

		public ScriptStore LuaStore
		{
			get => ScriptStore;
			set => ScriptStore = value;
		}

		public string[] Requires => null;

		private void Awake()
		{
			initTask = null;
		}

		private async void Start()
		{
			// NOTE: we should probably just AddComponent one on ourself instead
			if (coplayer == null)
				coplayer = Lua.RentPlayer();
			else
				coplayer.RestoreCleared(); // wait what? that shouldn't be possible

			parent = null;

			await GameController.TillInitAndLevelLoaded();
			await Init();
		}

		private void OnDisable()
		{
			LuaChangeWatcher.ClearWatches(this);
		}

		private void OnDestroy()
		{
			Lua.ReturnPlayer(ref coplayer);
		}

		private async UniTask Init()
		{
			coplayer.RestoreCleared();

			coplayer.transform.parent     = transform;

			mainClosure = null;

			if(_vcamTarget == null)
				_vcamTarget = new GameObject("Cam Target").AddComponent<VCamTarget>();

			coplayer.baseState.vcamTarget = _vcamTarget;
			coplayer.baseState.vcamTarget.transform.SetParent(transform);

			coplayer.sourceObject = gameObject;
			coplayer.DiscoverResources();

			coplayer.afterStopped += Stop;

			LuaChangeWatcher.BeginCollecting();
			{
				_initInstance = null;

				if (Script == null && !GlobalFunctionName.IsNullOrWhitespace()) {

					if (Lua.FindFirstGlobal(GlobalFunctionName, out Closure val, out Table container)) {

						// Create our table containing the cutscene, then make it a child of the global script our cutscene is contained in?

						runningScript = Lua.NewEnv($"cutscene-{GlobalFunctionName}");
						Lua.envScript.DoFileSafe("cutscene-api", runningScript);
						runningScript["this"]     = this;
						runningScript["coplayer"] = coplayer;
						runningScript["play"]     = (Func<Closure, WaitableCutscene>) Play;
						if (gameObject.TryGetComponent(out Actor actor))
							runningScript["self_actor"] = coplayer.UseMember(actor, Lua.NewTable("self_actor"));

						ScriptStore.WriteToTable(runningScript);
						coplayer.Setup(runningScript);

						Lua.glb_import_index(runningScript, container);

						mainClosure = val;
					}

				} else if(Script != null) {

					runningScript = Lua.NewEnv($"cutscene-{Script.name}");
					Lua.envScript.DoFileSafe("cutscene-api", runningScript);
					runningScript["this"]     = this;
					runningScript["coplayer"] = coplayer;
					runningScript["play"]     = (Func<Closure, WaitableCutscene>) Play;
					if (gameObject.TryGetComponent(out Actor actor))
						runningScript["self_actor"] = coplayer.UseMember(actor, Lua.NewTable("self_actor"));

					ScriptStore.WriteToTable(runningScript);
					coplayer.Setup(runningScript);

					Lua.LoadAssetInto(Script, runningScript);
				}
			}
			LuaChangeWatcher.EndCollecting(this, OnScriptChange);

			// Init invocation
			if(runningScript != null) {
				_initInstance = Lua.RunCoroutine(runningScript, "init", optional: true);
				if (_initInstance != null)
					await UniTask.WaitUntil(() => _initInstance == null || _initInstance.Ended);
			}

			// Keep the configuration options we set right of the bat in global and init
			coplayer.baseState = coplayer.state;

			coplayer.actorBrain.PriorityMod = BrainPriorityMod;
		}

		protected void OnScriptChange()
		{
			_scheduledReload = true;
		}


		public async UniTask InitAndPlay()
		{
			await Init();
			Play();
		}


		private void Update()
		{
			if (_scheduledReload)
			{
#if UNITY_EDITOR
				switch (InternalEditorConfig.Instance.cutsceneHotReloadBehavior)
				{
					case LuaChangeWatcher.HotReloadBehaviors.ContinueExisting:
						break;

					case LuaChangeWatcher.HotReloadBehaviors.Stop:
						_scheduledReload = false;
						Stop();
						Init().Forget();
						break;

					case LuaChangeWatcher.HotReloadBehaviors.Replay:
						bool wasPlaying = playing;
						_scheduledReload = false;
						Stop();

						if (wasPlaying)
						{
							InitAndPlay().Forget();
						}

						break;

					case LuaChangeWatcher.HotReloadBehaviors.WaitForEnd:
						if (!playing)
						{
							_scheduledReload = false;
							Stop();
							Init().Forget();
						}

						break;
				}
#else
				_scheduledReload = false;
#endif
			}

			if (!playing && coplayer != null)
			{
				coplayer.ControlCamera(false);
			}

			// Debug skip
			if (playing && coplayer != null && GameController.DebugMode)
			{
				if (Keyboard.current.f11Key.isPressed && !coplayer.Skipping)
				{
					coplayer.StartSkipping();
				}
			}
		}

		/// <summary>
		/// Play the cutscene.
		/// </summary>
		[CanBeNull]
		public WaitableCutscene Play([CanBeNull] Closure alternateMain = null)
		{
			if (playing)
				return null;

			playing = true;
			BeginScript(alternateMain).ForgetWithErrors();

			return new WaitableCutscene(this);
		}

		[CanBeNull]
		public WaitableCutscene PlayAsChild(Cutscene parent)
		{
			this.parent = parent;
			return Play();
		}

		private async UniTask BeginScript([CanBeNull] Closure alternateMain = null)
		{
			if (ControlsGame && !GameController.Live.BeginCutscene(this))
				return;

			LayerController.ManuallyUpdateActivation(true);

			// Get Coroutine
			// ----------------------------------------
			CoroutineInstance instance = alternateMain != null
				? Lua.CreateCoroutine(mainClosure ?? alternateMain)
				: Lua.CreateCoroutine(runningScript, "main");

			if (instance == null)
			{
				DebugLogger.LogError("Cutscene: Could not play because there was a problem while getting a coroutine of the function.", LogContext.Core, LogPriority.Critical);
				Stop();
				return;
			}

			// Player Coroutine
			// ----------------------------------------
			coplayer.sourceObject = gameObject;
			coplayer.vcamBlend    = DefaultVCamBlend;

			coplayer.Prepare(runningScript, instance);

			if (ControlsCamera)
				coplayer.ControlCamera(true);

			if(StartDelayFrames > 0)
				await UniTask2.Frames(StartDelayFrames);

			await coplayer.Play();
		}

		/// <summary>
		/// Stop the cutscene.
		/// </summary>
		public void Stop()
		{
			if (!playing)
				return;

			if (runningScript.TryGet("on_stop", out DynValue val) && val.Type == DataType.Function)
			{
				val.Function.Call();
			}


			if (parent != null)
			{
				if (parent.ControlsGame) GameController.Live.BeginCutscene(parent);
				if (parent.ControlsCamera) GameCams.SetController(parent.coplayer);
			}
			else if (ControlsGame && GameController.Live.ControllingCutscene == this)
			{
				GameController.Live.EndCutscene();
			}

			playing = false;
			coplayer.Stop();

			GameCams.Live.MessyHackToResetCinemachineBrainStack();

			LayerController.ManuallyUpdateActivation(true);
			LayerController.ProcessWaitingForCutscene();

			parent = null;
		}

		public void EndWith(Closure func)
		{
			if (!playing)
				return;

			if (runningScript.TryGet("on_stop", out DynValue val) && val.Type == DataType.Function)
			{
				val.Function.Call();
			}

			playing = false;

			if (parent != null)
			{
				if (parent.ControlsGame) GameController.Live.BeginCutscene(parent);
				if (parent.ControlsCamera) GameCams.SetController(parent.coplayer);
			}
			else if (ControlsGame && GameController.Live.ControllingCutscene == this)
				GameController.Live.EndCutscene();

			coplayer.StopWith(func);
			//Stop();
		}

#if UNITY_EDITOR

		private static Cutscene _lastCutscene;

		[LabelText("Play")]
		[TitleGroup("Workflow")]
		[DisableInEditorMode]
		[PropertyOrder(1)]
		[Button]
		public void PlayButton()
		{
			Play();
			_lastCutscene = this;
		}

		[LabelText("Stop")]
		[TitleGroup("Workflow")]
		[DisableInEditorMode]
		[DisableIf("@!playing")]
		[PropertyOrder(1)]
		[Button]
		public void StopButton()
		{
			Stop();
			Init();
			_lastCutscene = this;
		}

		[LabelText("Restart")]
		[TitleGroup("Workflow")]
		[DisableInEditorMode]
		[DisableIf("@!playing")]
		[PropertyOrder(1)]
		[Button]
		public void RestartButton()
		{
			if (playing)
				Stop();

			if (_scheduledReload)
				InitAndPlay().Forget();
			else
				Play();

			_lastCutscene = this;
		}

		[LabelText("Schedule Reload")]
		[TitleGroup("Workflow")]
		[DisableInEditorMode]
		[PropertyOrder(1)]
		[Button]
		public void ScheduleReloadButton()
		{
			_scheduledReload = true;
		}

		[PropertySpace]
		[LabelText("Add VCam")]
		[TitleGroup("Workflow")]
		[DisableInPlayMode]
		[PropertyOrder(1)]
		[Button]
		public void AddVCamButton()
		{
			throw new NotImplementedException();
		}

		[LabelText("Add Timeline")]
		[TitleGroup("Workflow")]
		[DisableInPlayMode]
		[PropertyOrder(1)]
		[Button]
		public void AddTimelineButton()
		{
			EditorUtility.DisplayDialog("Oops!", "This functionality is not implemented yet.", "OK");
		}

		[Shortcut("Anjin/Cutscene/Stop")]
		public static void StopShortcut()
		{
			if (!Application.isPlaying) return;

			Cutscene cutscene = GameController.Live.ControllingCutscene;
			if (cutscene != null && cutscene.playing)
			{
				cutscene.Stop();
				_lastCutscene = cutscene;
			}
		}


		[Shortcut("Anjin/Cutscene/Replay")]
		public static void ReplayShortcut()
		{
			if (!Application.isPlaying) return;

			Cutscene cutscene = GameController.Live.ControllingCutscene;

			if (cutscene == null)
				cutscene = _lastCutscene;

			if (cutscene == null && Selection.activeGameObject)
				cutscene = Selection.activeGameObject.GetComponentInParent<Cutscene>();

			if (cutscene != null)
			{
				if (cutscene.playing)
					cutscene.Stop();

				cutscene.Play();
				_lastCutscene = cutscene;
			}
		}

		private async void OnScriptChanged()
		{
			if (Application.isPlaying)
			{
				Stop();
				await Init();
			}
		}
#endif
	}
}
using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	public interface IMinigameResettable {
		void OnMinigameReset();
	}

	public class Minigame : SerializedMonoBehaviour, ICoroutineWaitable, IDebugDrawer, IOverworldDeathHandler
	{
		public bool ControlsGame          = true;
		public bool OverworldDeathEnabled = true;

		public OverworldDeathConfig? DeathConfigOverride;
		public LuaScriptContainer    Script = new LuaScriptContainer(true);

		[NonSerialized, ShowInPlay]
		[UsedImplicitly]
		public MinigameState state;

		public bool IsRunning        => state == MinigameState.Running;
		public bool PlayerHasControl => state == MinigameState.Running && _playerHasControl;

		[ShowInInspector]
		private bool _playerHasControl;

		protected IMinigameResettable[] _resettables;

		[ShowInPlay, NonSerialized]
		protected Closure Lua_OnEnd;

		[ShowInPlay]
		protected MinigamePlayOptions _playOptions = MinigamePlayOptions.Default;

		public virtual void Start()
		{
			DebugSystem.Register(this);

			Script.OnStart(this, container =>
			{
				Lua.envScript.DoFileSafe("minigame_api", container._state.table);
				container._state.table["minigame"] = this;
				container._state.table["was_quit"] = false;
			});

			if (TryGetComponent(out Interactable interactable))
				interactable.OnInteract.AddListener(async () =>
				{
					await Setup();
					Begin().Forget();
				});

			_resettables = GetComponentsInChildren<IMinigameResettable>();

			_playerHasControl = true;
		}

		public void ToggleControlFromPrompt(bool controllable)
		{
			_playerHasControl = controllable;
		}

		public virtual async UniTask<bool> Setup(IMinigameSettings settings = null)
		{
			await Script_Setup(settings);
			return true;
		}

		protected void Boot()
		{
			foreach (IMinigameResettable resettable in _resettables)
				resettable.OnMinigameReset();
		}

		// Start off the minigame, with a callback to be invoked upon ending
		[Button, ShowInPlay]
		public virtual async UniTask<bool> Begin(MinigamePlayOptions options = MinigamePlayOptions.Default)
		{
			if (state != MinigameState.Off || ControlsGame && !GameController.Live.BeginMinigame(this))
				return false;

			_playOptions = options;

			Boot();

			state = MinigameState.Intro;

			if (Script.Script != null)
			{
				await Script_OnStart();

				if ((_playOptions & MinigamePlayOptions.PlayIntro) != 0)
					await Script_Intro();

				await Script_OnRun();
			}

			state = MinigameState.Running;

			return true;
		}

		[Button, ShowInPlay]
		public async void SetupAndBegin(MinigamePlayOptions options = MinigamePlayOptions.Default, IMinigameSettings settings = null)
		{
			await Setup(settings);
			await Begin(options);
		}

		public virtual void Quit() => Finish(MinigameFinish.UserQuit);

		[Button, ShowInPlay]
		public virtual async UniTask Finish(MinigameFinish finish = MinigameFinish.Normal)
		{
			if (state != MinigameState.Running) return;

			Script._state.table["was_quit"] = finish == MinigameFinish.UserQuit;

			state = MinigameState.Outro;
			await Script_OnFinish();

			if ((_playOptions & MinigamePlayOptions.PlayIntro) != 0)
				await Script_Outro();

			await Script_OnEnd();

			AfterFinish();
		}

		protected void AfterFinish()
		{
			foreach (IMinigameResettable resettable in _resettables)
				resettable.OnMinigameReset();

			state = MinigameState.Off;

			if (GameController.Live.CurrentMinigame == this)
				GameController.Live.EndMinigame();

			Lua_OnEnd?.Call();
			Lua_OnEnd = null;
		}

		public virtual IMinigameResults GetResults()
		{
			if (Script.Script != null)
			{
				DynValue results = Script.TryCall("get_results");
				if (results.AsUserdata(out IMinigameResults r))
					return r;
			}

			return null;
		}

		protected virtual void Update()
		{
			Script.OnUpdate(state == MinigameState.Running && !GameController.IsWorldPaused);
		}

		public virtual void ModifyDeathConfig(ref OverworldDeathConfig config)
		{
			if (DeathConfigOverride.HasValue)
				config = DeathConfigOverride.Value;
		}

		public virtual bool AutoImguiWindow => true;

		public void OnLayout(ref DebugSystem.State state)
		{
			if (this.state != MinigameState.Running) return;
			Script.TryCall("on_imgui");

			if (AutoImguiWindow) {
				if (state.DebugMode && ImGui.Begin(gameObject.name)) {

					OnImgui();

					if (this.state == MinigameState.Running) {

						if (ImGui.Button("Win"))	Finish(MinigameFinish.DebugWin).ForgetWithErrors();
						ImGui.SameLine();
						if (ImGui.Button("Lose"))	Finish(MinigameFinish.DebugLose).ForgetWithErrors();

						ImGui.Separator();
					}
				}

				ImGui.End();
			} else {
				OnImgui();
			}
		}

		public virtual void OnImgui()
		{


		}

		protected async UniTask Script_Setup(IMinigameSettings settings = null) => await Script.TryPlayAsync("setup", settings);
		protected async UniTask Script_OnStart()                                => await Script.TryPlayAsync("on_start");
		protected async UniTask Script_OnRun()                                  => await Script.TryPlayAsync("on_run");
		protected async UniTask Script_OnFinish()								=> await Script.TryPlayAsync("on_finish");
		protected async UniTask Script_Intro()                                  => await Script.TryPlayAsync("intro");
		protected async UniTask Script_Outro()                                  => await Script.TryPlayAsync("outro");
		protected async UniTask Script_OnEnd()                                  => await Script.TryPlayAsync("on_end");

		public virtual bool CanContinue(bool justYielded, bool isCatchup) => true;

		public class MinigameLuaProxy<T> : MonoLuaProxy<T> where T : Minigame
		{
			public MinigameState state => proxy.state;

			public bool player_has_control { get => proxy._playerHasControl; set => proxy._playerHasControl = value; }

			public WaitableUniTask setup(IMinigameSettings settings = null) => new WaitableUniTask(proxy.Setup(settings));

			public WaitableUniTask begin(MinigamePlayOptions options = MinigamePlayOptions.Default) => new WaitableUniTask(proxy.Begin(options));
			public WaitableUniTask finish()                                                         => new WaitableUniTask(proxy.Finish());

			public IMinigameResults get_results() => proxy.GetResults();

			public void on_end(Closure func) => proxy.Lua_OnEnd = func;
		}

		public class MinigameLuaProxyBase : MinigameLuaProxy<Minigame> { }

	}
}
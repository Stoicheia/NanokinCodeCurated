using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;

namespace Anjin.Minigames
{
	public class LuaMinigame : LuaScriptComponent, IDebugDrawer
	{
		// Just in case we want to change these later.
		// We'll probably display rank names a ton of places and they should stay consistent.
		public static Dictionary<MinigameRank, string> RankNames = new Dictionary<MinigameRank, string>
		{
			{MinigameRank.None, "-"},
			{MinigameRank.S, "S"},
			{MinigameRank.A, "A"},
			{MinigameRank.B, "B"},
			{MinigameRank.C, "C"},
			{MinigameRank.D, "D"},
			{MinigameRank.E, "E"},
			{MinigameRank.F, "F"},
		};

		public MinigameState State;

		public  bool Running;
		public  bool ControlsGame = true;
		public  bool PlayerHasControl => State == MinigameState.Running && _playerHasControl;
		private bool _playerHasControl;

		public Closure on_end_external; //For other scripts to set.

		public Closure on_imgui;

		private CoroutineInstance _setupInstance;

		public override void Start()
		{
			base.Start();
			DebugSystem.Register(this);
			State             = MinigameState.Off;
			_playerHasControl = true;
		}

		protected override void OnInit()
		{
			base.OnInit();
			Lua.envScript.DoFileSafe("minigame_api", ScriptTable);
			ScriptTable["minigame"] = this;
		}

		protected override void CallUpdate()
		{
			if (State == MinigameState.Running) base.CallUpdate();
		}

		public async UniTask<bool> Setup(IMinigameSettings settings = null, Closure onEnded = null)
		{
			on_end_external = onEnded;

			_setupInstance = Lua.RunCoroutine(ScriptTable, "setup", new[] {settings}, true);
			if (_setupInstance != null)
				await UniTask.WaitUntil(() => _setupInstance == null || _setupInstance.Ended);

			return true;
		}

		public async UniTask<bool> StartMinigame()
		{
			/*if (State != MinigameState.Off || ControlsGame && !GameController.Live.BeginMinigame(this))
				return false;*/

			State = MinigameState.Intro;
			await PlayAsync("on_start");
			await PlayAsync("intro");
			await PlayAsync("on_run");
			State = MinigameState.Running;

			return true;
		}

		[Button]
		public async UniTask Finish()
		{
			if (State != MinigameState.Running) return;

			State = MinigameState.Outro;
			await PlayAsync("on_finish");
			await PlayAsync("outro");

			DynValue results = ScriptTable.TryCall(Lua.envScript, "on_end");
			State = MinigameState.Off;

			if (GameController.Live.CurrentMinigame == this)
				GameController.Live.EndMinigame();

			on_end_external?.Call(results);
		}

		async UniTask PlayAsync(string name)
		{
			CoroutineInstance instance = Lua.CreateCoroutine(ScriptTable, name, null, true);
			if (instance != null)
			{
				await _player.Play(ScriptTable, instance);
				await UniTask.WaitUntil(() => instance == null || instance.Ended);
			}
		}

		public override void OnReload()
		{
			Finish();
			base.OnReload();
		}

		public void OnLayout(ref DebugSystem.State state)
		{
			if (!Running) return;
			on_imgui?.Call();
		}


		public class MinigameLuaProxy : LuaComponentBaseProxy<LuaMinigame>
		{
			public MinigameState state => proxy.State;

			public bool player_has_control { get => proxy._playerHasControl; set => proxy._playerHasControl = value; }

			public WaitableUniTask setup(IMinigameSettings settings = null, Closure onEnded = null) => new WaitableUniTask(proxy.Setup(settings, onEnded));

			public WaitableUniTask start()  => new WaitableUniTask(proxy.StartMinigame());
			public WaitableUniTask finish() => new WaitableUniTask(proxy.Finish());

			public Closure on_imgui { get => proxy.on_imgui; set => proxy.on_imgui = value; }
		}
	}
}
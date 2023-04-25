using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using g = ImGuiNET.ImGui;
// ReSharper disable ReplaceWithSingleAssignment.True

namespace Overworld.Cutscenes
{
	public class PartyChat : StaticBoy<PartyChat>, IDebugDrawer {
		public const int MAX_BUFFERED_CHATS = 1;

		public enum State {
			Off, Playing, Paused
		}

		struct LoadedScript
		{
			public string       name;
			public List<string> funcs;
			public bool         error;
			public string       message;
		}

		struct BufferedChat {
			public bool    is_global;
			public string  id;
			public Closure closure;

			public bool   force;
			public bool   no_save;
			public string save_id;
		}

		[ShowInPlay, NonSerialized]
		public State state;

		private Coplayer _player;
		private Table    _script;
		private bool     _scheduledReload;

		private List<LoadedScript> _allScripts;

		private Table _allChats;
		private Table _allChatsMap;
		private Table _tempChats;

		private Dictionary<string, Closure> _CutsceneFuncs;
		private Dictionary<string, string>  _DisplayNames;

		private Queue<BufferedChat> _bufferedChats;

		private async void Start()
		{
			DebugSystem.Register(this);
			state          = State.Off;

			_CutsceneFuncs = new Dictionary<string, Closure>();
			_DisplayNames  = new Dictionary<string, string>();

			_allScripts    = new List<LoadedScript>();
			_bufferedChats = new Queue<BufferedChat>();

			if (_player == null)
				_player = Lua.RentPlayer();
			else
				_player.RestoreReady();

			await GameController.TillIntialized();
			/*await Lua.initTask;
			await ActorController.initTask;*/
			await Init();
		}

		private async UniTask Init()
		{
			_CutsceneFuncs.Clear();
			_allScripts.Clear();

			_player.RestoreReady();
			_player.afterStopped   += Stop;

			LuaChangeWatcher.BeginCollecting();

			_script = Lua.NewEnv("party-chat");
			Lua.envScript.DoFileSafe("pchat", _script);
			_allChats    = _script.Get("all_chats").Table;
			_allChatsMap = _script.Get("all_chats_map").Table;
			_tempChats   = _script.Get("temp_chats").Table;

			List<string> all_scripts = Lua.scriptLoader.GetAllNames();

			//NOTE(C.L.): There's probably a better way to do this than loading each script twice

			foreach (string script in all_scripts)
			{
				if (!script.StartsWith("party_chat_")) continue;

				LoadedScript s = new LoadedScript();
				s.name = script;

				try
				{
					_tempChats.Clear();
					s.funcs = new List<string>();

					LuaChangeWatcher.Use(script);
					Lua.envScript.DoFile(script, _script);

					foreach (DynValue value in _tempChats.Values)
					{
						s.funcs.Add(value.String);
					}
				}
				catch (InterpreterException e)
				{
					s.error   = true;
					s.message = e.GetPrettyString();
					Debug.LogError(e.GetPrettyString());
				}
				catch (Exception e)
				{
					s.error   = true;
					s.message = e.ToString();
					Debug.Log(e.ToString());
				}

				_allScripts.Add(s);
			}

			foreach (LoadedScript script in _allScripts)
			{
				foreach (string id in script.funcs)
				{
					Table chat                                                                                                         = _allChatsMap.Get(id).Table;
					if (chat.TryGet("cutscene", out DynValue val) && val.Type == DataType.Function)
						_CutsceneFuncs[id] = val.Function;

					if (chat.TryGet("display_name", out string str)) {
						_DisplayNames[id] = str;
					}
				}
			}

			_script["coplayer"] = _player;
			_script["say_mode"] = 2;

			_player.Setup(_script);

			LuaChangeWatcher.EndCollecting(this, OnScriptChange);
			_player.baseState = _player.state;
		}

		public void _stop()
		{
			if (state < State.Playing)
				return;

			state = State.Off;
			_hideVignette();
			PartyChatHUD.TMP_Name.text = "";
			_player.Stop();
		}

		protected void OnScriptChange()
		{
			_scheduledReload = true;
		}

		private void Update()
		{
			bool can_show = true;

			if(SplicerHub.menuActive                           ||
			   GameController.Live.ControllingCutscene != null ||
			   GameController.IsWorldPaused)
				can_show = false;

			if (state == State.Playing) {
				if (GameInputs.partyChat.IsPressed) {
					PartyChatHUD.Textbox.Advance();
				}

				PartyChatHUD.Textbox.AdvanceButton         = GameInputs.partyChat;
			}

			// If we're in any play state, we need to handle pausing properly
			if (state >= State.Playing) {
				if (state == State.Playing) {
					if (!can_show) Pause();
				} else if (state == State.Paused) {
					if (can_show) Unpause();
				}
			} else {
				if (can_show && _bufferedChats.Count > 0) {
					var chat = _bufferedChats.Dequeue();
					ProcessBufferedChat(chat);
				}
			}

			if (_scheduledReload)
			{
#if UNITY_EDITOR
				switch (InternalEditorConfig.Instance.cutsceneHotReloadBehavior)
				{
					case LuaChangeWatcher.HotReloadBehaviors.ContinueExisting:
						break;

					case LuaChangeWatcher.HotReloadBehaviors.Stop:
						_scheduledReload = false;
						_stop();
						Init().Forget();
						break;

					case LuaChangeWatcher.HotReloadBehaviors.Replay: break;

					case LuaChangeWatcher.HotReloadBehaviors.WaitForEnd:
						if (state < State.Playing)
						{
							_scheduledReload = false;
							Init().Forget();
						}

						break;
				}
#else
				_scheduledReload = false;
#endif
			}
		}

		// API
		//--------------------------------------------------------
		[ShowInInspector]
		public static void PlayGlobalPartyChat(string id, bool force = false, bool buffered = true)
		{
			Live._play_global(id, force);
		}

		[LuaGlobalFunc("party_chat_play")]
		public static void PlayPartyChat(DynValue val, Table options = null)
		{
			bool   buffered = true;

			bool   force   = false;
			bool   no_save = false;
			string save_id = null;

			if (options != null) {
				options.TryGet("force",    out force,    force);
				options.TryGet("no_save",  out no_save,  no_save);
				options.TryGet("save_id",  out save_id,  save_id);
				options.TryGet("buffered", out buffered, buffered);
			}

			if (buffered && Live._bufferedChats.Count < MAX_BUFFERED_CHATS) {

				BufferedChat chat = default;

				if (val.AsString(out string glb_id)) {
					chat = new BufferedChat { is_global = true, id = glb_id, closure = null };
				} else if (val.AsFunction(out Closure closure)) {
					chat = new BufferedChat { id = save_id, closure = closure };
				}

				chat.force   = force;
				chat.no_save = no_save;
				chat.save_id = save_id;

				Live._bufferedChats.Enqueue(chat);

			} else {
				if (val.AsString(out string glb_id))
					Live._play_global(glb_id, force, no_save, save_id);
				else if (val.AsFunction(out Closure closure)) {

					if (save_id != null) {
						if (!force && SaveManager.current.PartyChatsSeen.Contains(save_id)) {
							return;
						}

						if (!no_save)
							SaveManager.current.PartyChatsSeen.AddIfNotExists(save_id);
					}

					Live._play(closure, save_id);
				}
			}
		}

		void ProcessBufferedChat(BufferedChat play)
		{
			if(play.is_global)
				Live._play_global(play.id, play.force, play.no_save, play.save_id);
			else if (play.closure != null) {

				if (play.save_id != null) {
					if (!play.force && SaveManager.current.PartyChatsSeen.Contains(play.save_id)) {
						return;
					}

					if(!play.no_save)
						SaveManager.current.PartyChatsSeen.AddIfNotExists(play.save_id);
				}

				Live._play(play.closure, play.save_id);
			}

		}

		[ShowInInspector]
		[LuaGlobalFunc("party_chat_stop")]
		public static void Stop()
		{
			Live._stop();
		}

		[LuaGlobalFunc("party_chat_pause")]
		public static void Pause()		=> Live._pause();

		[LuaGlobalFunc("party_chat_unpause")]
		public static void Unpause()	=> Live._unpause();

		[LuaGlobalFunc("party_chat_clear_buffer")]
		public static void ClearBuffer() => Live._bufferedChats.Clear();

		[LuaGlobalFunc("party_chat_seen")]
		public static bool Seen(string id)
		{
			if (SaveManager.current == null) return false;
			return SaveManager.current.PartyChatsSeen.Contains(id);
		}

		[Button]
		public void _pause()
		{
			state = State.Paused;
			_player.Pause();
			_hideVignette();
			PartyChatHUD.TMP_Name.enabled = false;
			PartyChatHUD.Textbox.Pause();
		}

		[Button]
		public void _unpause()
		{
			state = State.Playing;
			_showVignette();
			_player.Unpause();
			PartyChatHUD.TMP_Name.enabled = true;
			PartyChatHUD.Textbox.Unpause();
		}

		[ShowInInspector]
		public static void Test()
		{
			foreach (var pair in Live._CutsceneFuncs)
			{
				Debug.Log($"{pair.Key}:{pair.Value}");
			}
		}
		//--------------------------------------------------------

		void _play_global(string id, bool force = false, bool no_save = false, string save_id = null)
		{
			if (state > State.Off) return;

			if (!force && SaveManager.current.PartyChatsSeen.Contains(save_id ?? id)) {
				return;
			}

			if(!no_save)
				SaveManager.current.PartyChatsSeen.AddIfNotExists(save_id ?? id);

			if (!_CutsceneFuncs.TryGetValue(id, out Closure func)) return;

			_play(func, id);
		}

		void _play(Closure func, string id = null)
		{
			if (func == null) return;

			CoroutineInstance instance = Lua.CreateCoroutine(_script, func);

			_showVignette();

			if (id != null && _DisplayNames.TryGetValue(id, out string display_name))
				PartyChatHUD.TMP_Name.text = display_name;
			else
				PartyChatHUD.TMP_Name.text = "";

			state = State.Playing;
			_player.Play(_script, instance);
		}

		void _showVignette() => GameEffects.DoVignette(0.75f, 0.25f);
		void _hideVignette() => GameEffects.DoVignette(0, 0.25f);

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.IsMenuOpen("Party Chat"))
			{
				if (g.Begin("Party Chat"))
				{
					g.TextColored(ColorsXNA.Goldenrod.ToV4(), "State: " + this.state);
					if (this.state >= State.Playing)
					{
						g.SameLine();
						if (g.Button("Stop"))
						{
							Stop();
						}

						if (this.state != State.Paused) {
							if (g.Button("Pause"))   Pause();
						} else {
							if (g.Button("Unpause")) Unpause();
						}
					}

					g.TextColored(ColorsXNA.Goldenrod.ToV4(), "Buffered Chats: " + _bufferedChats.Count);

					g.BeginChild("list");

					foreach (var script in _allScripts)
					{
						g.PushID(script.name);

						if (g.CollapsingHeader(script.name))
						{
							g.Indent(16);
							if (!script.error)
							{
								foreach (string func in script.funcs)
								{
									g.PushID(func);
									g.TextColored(ColorsXNA.CornflowerBlue, func);
									g.SameLine(g.GetWindowContentRegionWidth() - 48);
									if (g.Button("Play"))
									{
										PlayGlobalPartyChat(func);
									}

									g.PopID();
								}
							}
							else
							{
								g.TextColored(Color.red.ToV4(), "Load error:\n" + script.message);
							}

							g.Unindent(16);
						}

						g.PopID();
					}


					g.EndChild();
				}

				g.End();
			}
		}
	}
}
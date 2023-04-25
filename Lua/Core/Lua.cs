using System;
using System.Collections.Generic;
using System.Diagnostics;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using Util;
using Util.Odin.Attributes;
using Coroutine = MoonSharp.Interpreter.Coroutine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.Scripting
{
	/// <summary>
	/// Main API for all Lua functionalities in the game
	/// Offers some functionalities...
	/// - Global scripts (receive events from anywhere, e.g. level scripts)
	/// - Play with a pooled CoroutinePlayer (Rent/Return functions)
	/// And offers a number of superior wrappers around MoonSharp:
	/// - Create empty tables.
	/// - Create a env table. (all common requires already imported)
	/// - Create a env table out of a script.
	/// - Invoke functions on table.
	/// - Invoke coroutine on table.
	/// - Invoke coroutine on table with a CoroutinePlayer.
	/// - Invoke coroutines in global scripts.
	/// All functions print errors generously and Lua stacktrace where applicable.
	/// </summary>
	[DefaultExecutionOrder(-1000)]
	public class Lua : StaticBoy<Lua>
	{
		private const string MAIN_SCRIPT_NAME = "main";
		private const string ELAPSED_FORMAT   = "s\\.fff";

		public enum InitState { Zero, Env, Loaded, Ready }

		private static InitState         _state;
		private static Stopwatch         _timer = new Stopwatch();
		private static bool              _editor;
		public static  Script            envScript;
		public static  Table             envGlobals;
		public static  AnjinScriptLoader scriptLoader;
		public static  AsyncLazy         initTask;

		public static  List<Table>                 GlobalScripts;
		private static List<Action>                _onReady;
		private static List<ILuaInit>              _onReadyInterfaces;
		private static Dictionary<LuaAsset, Table> _globalScriptsMapped;
		private static List<CoroutineInstance>     _activeCoroutines;

		[ShowInPlay, ReadOnly]
		private ComponentPool<Coplayer> _playerPool;

		private static List<Coplayer> _tmpCoroutines1 = new List<Coplayer>();
		private static List<Coplayer> _tmpCoroutines2 = new List<Coplayer>();

		public static bool Ready => _state == InitState.Ready;

		public static Table LevelTable;
		//public static Table CurrentGlobalTable => LevelTable ?? envGlobals;

		[ShowInInspector]
		protected new void Awake()
		{
			base.Awake();

			Live._playerPool = new ComponentPool<Coplayer>(transform)
			{
				objectName   = "Script Player",
				initSize     = GameOptions.current.load_on_demand ? 0 : 8,
				maxSize      = 64,
				allocateTemp = true
			};
		}


#if UNITY_EDITOR
		/// <summary>
		/// Completely Initialize the Lua core without async.
		/// This is a Lua initialization for the editor only, like unit tests etc.
		/// Uses asset database for all script loading.
		/// </summary>
		public static void InitializeEditor(bool force = false)
		{
			if (_state != InitState.Zero && _editor && !force)
				return;

			InitCore();
			InitEnv(new HybridScriptLoader(true));

			LuaUtil.RegisterUserdata(envScript);
			LuaUtil.RegisterConstructors(envScript.Globals);

			_state = InitState.Loaded;

			InitScript();
			InitFinish();

			_editor = true;
		}
#endif


		public static async UniTask InitalizeThroughGameController()
		{
			// Note(C.L. 8-9-22): This shouldn't need to be called here, because GameController should already do this
			// UniTask2.InitPlayerLoopHelper(); // In some circumstances, Lua.Initialize can run before UniTask's initialization
			if (_state > InitState.Zero && !_editor && GameOptions.current.keep_lua_core)
				return;

			InitCore();
			InitEnv(new HybridScriptLoader());

			await InitLoad();
		}

		private static void Initialize()
		{
			//UniTask2.InitPlayerLoopHelper(); // In some circumstances, Lua.Initialize can run before UniTask's initialization

			InitCore();
			InitEnv(new HybridScriptLoader());

			initTask = InitLoad().ToAsyncLazy();
		}


		private static void InitCore()
		{
			_editor = false;

			_state = InitState.Zero;
			_timer.Restart();

			envScript    = null;
			envGlobals   = null;
			scriptLoader = null;
			initTask     = null;

			_onReady             = new List<Action>();
			_onReadyInterfaces   = new List<ILuaInit>();
			GlobalScripts        = new List<Table>();
			_globalScriptsMapped = new Dictionary<LuaAsset, Table>();
			_activeCoroutines    = new List<CoroutineInstance>();
		}

		private static void InitEnv(AnjinScriptLoader loader)
		{
			scriptLoader = loader;

			envScript = new Script
			{
				Options =
				{
					ScriptLoader = loader,
					DebugPrint   = Debug.Log
				}
			};

			envScript.Options.ColonOperatorClrCallbackBehaviour = ColonOperatorBehaviour.TreatAsColon;

			envGlobals          = envScript.Globals;
			envGlobals["_name"] = "Globals";


			_state = InitState.Env;
			LogTrace("--", "Environment created");
		}

		private static async UniTask InitLoad()
		{
			await UniTask.WhenAll(
				((HybridScriptLoader)scriptLoader).LoadAll(),
				UniTask.RunOnThreadPool(() =>
				{
					LuaUtil.RegisterUserdata(envScript);
					LuaUtil.RegisterConstructors(envScript.Globals);
				})
			);

			_state = InitState.Loaded;

			LogTrace("--", "Script loader ready");

			InitScript();
			InitFinish();
		}

		/// <summary>
		/// Initialize the main script of the lua core.
		/// </summary>
		private static void InitScript()
		{
			if (_state < InitState.Loaded)
			{
				"Lua".LogError("Cannot get main script because we haven't loaded the scripts.");
				return;
			}

			// I think this was fixed by some changes in the logic, haven't seen it in a while
			if (!scriptLoader.ScriptFileExists("main"))
			{
				DebugLogger.LogError("Couldn't find 'main' lua script for Lua system. This can happen randomly sometimes, please use the menu item located at 'Anjin/Workflow/Reload Lua'", LogContext.Lua, LogPriority.Critical);
				return;
			}

			envScript.DoFileSafe(MAIN_SCRIPT_NAME, envGlobals);

			LevelTable = new Table(envScript);
			glb_import_index(LevelTable, envGlobals);

			if (Live != null)
			{
				LuaChangeWatcher.Watch("LUA_MAIN_WATCH", MAIN_SCRIPT_NAME, InitScript);
			}

			LogTrace("--", "Main script loaded");
		}

		private static void InitFinish()
		{
			_state = InitState.Ready;

			foreach (Action readyHandler in _onReady) readyHandler();
			foreach (ILuaInit readyHandler in _onReadyInterfaces) readyHandler.OnLuaReady();

			LogTrace("--", "Fully initialized!");
		}


		/// <summary>
		/// Readying all the scripts involves asynchronous loading, so
		/// you must use this function to wait for all the game's scripts
		/// to be loaded.
		/// (text is very light, so we can easily afford to load every script on startup)
		/// </summary>
		/// <param name="handler"></param>
		public static void OnReady(Action handler)
		{
			if (Ready)
				handler();
			else
				_onReady.Add(handler);
		}


		/// <summary>
		/// Readying all the scripts involves asynchronous loading, so
		/// you must use this function to wait for all the game's scripts
		/// to be loaded.
		/// (text is very light, so we can easily afford to load every script on startup)
		/// </summary>
		/// <param name="handler"></param>
		public static void OnReady(ILuaInit init)
		{
			if (Ready)
				init.OnLuaReady();
			else
				_onReadyInterfaces.Add(init);
		}


		private void Update()
		{
			if (!Ready) return;

			if (envScript != null)
			{
				InvokeGlobal("update", null, true); // There is now a use.

				// Invoke(envGlobals, "update"); // This is currently unused and I'm not sure if it will really serve any purpose, so I'm disabling to avoid allocation every frame

				for (int i = 0; i < _activeCoroutines.Count; i++)
				{
					CoroutineInstance co = _activeCoroutines[i];

					// 1st death check
					if (co.Ended)
					{
						_activeCoroutines.RemoveAt(i--);
						continue;
					}

					co.TryContinue(Time.deltaTime);

					// 2nd death check
					if (co.Ended)
					{
						_activeCoroutines.RemoveAt(i--);
					}
				}
			}
		}

		public static void ResetLeveltable() => LevelTable.Clear();

		public static void RegisterToLevelTable(Component comp, string name = null)
		{
			if (comp == null || LevelTable == null) return;
			LevelTable[name ?? comp.name] = comp;
		}

		public static void RegisterToLevelTable(GameObject obj, string name = null)
		{
			if (obj == null || LevelTable == null) return;
			LevelTable[name ?? obj.name] = obj;
		}


		/// <summary>
		/// Rent a coroutine player for temporary use.
		/// Must be released after.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static Coplayer RentPlayer(bool autoreturn = false)
		{
			Coplayer ret = Live._playerPool.Rent();
			if (autoreturn) ret.AutoReturn();
			return ret;
		}

		/// <summary>
		/// Release the coroutine player so it can be reused.
		/// </summary>
		public static void ReturnPlayer(ref Coplayer player)
		{
			if (Live == null || Live._playerPool == null) return;
			Live._playerPool.ReturnSafe(player);
			player = null;
		}

		/// <summary>
		/// Release the coroutine player so it can be reused.
		/// </summary>
		public static void ReturnPlayer(Coplayer player)
		{
			Live._playerPool.ReturnSafe(player);
		}

		/// <summary>
		/// Add a script to the global scripts which receive all events.
		/// </summary>
		public static Table AddGlobalScript(LuaAsset asset)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to AddGlobalScript before the environment is ready.");

			Table script = NewScript(asset);

			GlobalScripts.Add(script);
			_globalScriptsMapped[asset] = script;

			return script;
		}

		/// <summary>
		/// Remove a global script that was added through a LuaAsset.
		/// </summary>
		public static void RemoveGlobalScript(LuaAsset script)
		{
			if (_globalScriptsMapped.TryGetValue(script, out Table tbl))
			{
				GlobalScripts.Remove(tbl);
				_globalScriptsMapped.Remove(script);
			}
		}

		/// <summary>
		/// Create a new table from this environment.
		/// </summary>
		public static Table NewTable(string name = null)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to NewTable before the environment is ready.");

			if (name != null)
				return new Table(envScript) { ["_name"] = name };
			else
				return new Table(envScript);
		}

		// Environments
		// ----------------------------------------

		public static Table NewEnv(string name, Table container = null, bool imports = true)
		{
			Table env = container;
			if (container == null)
				env = NewTable(name ?? "env");

			Invoke(envGlobals, "import_index", new object[] { env, LevelTable ?? envGlobals });

			if (imports)
			{
				envScript.DoFileSafe("util", env);
				envScript.DoFileSafe("assets", env);
				envScript.DoFileSafe("wait", env);
				envScript.DoFileSafe("coplayer", env);
			}

			return env;
		}

		/// <summary>
		/// Create a script table for the piece of code.
		/// This table's global context has an indexer to
		/// 'import' the environment's globals.
		/// </summary>
		public static Table NewScript(string code, string friendlyName, string id = null)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to NewScript before the environment is ready.");

			Table script = NewEnv($"scriptenv: {friendlyName}");
			LoadCodeInto(code, script, friendlyName, id);

			return script;
		}

		/// <summary>
		/// Create a new script table from a file name.
		/// </summary>
		public static Table NewScript(string file)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to NewScript before the environment is ready.");

			LuaChangeWatcher.Use(file);
			Table script = NewEnv($"scriptenv: {file}");
			LoadFileInto(file, script);

			return script;
			// var code = (string) scriptLoader.LoadFile(file, envGlobals);
			// return NewScript(code, file);
		}

		/// <summary>
		/// Create a new script table from a LuaAsset.
		/// </summary>
		public static Table NewScript(LuaAsset asset)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to NewScript before the environment is ready.");

			LuaChangeWatcher.Use(asset);
			LuaChangeWatcher.FlushChanges(asset);
			return NewScript(asset.TranspiledText, asset.name, asset.name);
		}

		// LOADING INTO TABLES
		// ----------------------------------------

		/// <summary>
		/// Load a piece of code into a table.
		/// </summary>
		public static DynValue LoadCodeInto(
			string code,
			Table  into,
			string name = null,
			string id   = null)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to LoadCodeInto before the environment is ready.");

			if (into == null)
			{
				DebugLogger.LogError("Cannot load code into null table.", LogContext.Lua, LogPriority.High);
				return null;
			}

			Profiler.BeginSample($"Lua.LoadCodeInto({name})");
			try
			{
				code = LuaUtil.TranspileToLua(code);

				try
				{
					DynValue dv = envScript.DoString(code, into, name, id);
					return dv;
				}
				catch (InterpreterException sre)
				{
					DebugLogger.LogError(sre.GetPrettyString(into), LogContext.Lua, LogPriority.High);
				}
				catch (Exception e)
				{
					DebugLogger.LogError($"Lua: LoadIntoTable failed with the error '{e}'", LogContext.Lua, LogPriority.High);
				}
			}
			finally
			{
				Profiler.EndSample();
			}


			return DynValue.Nil;
		}

		/// <summary>
		/// Load a lua asset into a table.
		/// </summary>
		public static DynValue LoadAssetInto(LuaAsset asset, Table into)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to LoadAssetInto before the environment is ready.");

			LuaChangeWatcher.Use(asset);
			LuaChangeWatcher.FlushChanges(asset);

			// AjLog.LogTrace(">>", $"Lua.load_asset {asset.name}");

			return LoadCodeInto(asset.Text, into, asset.name, asset.name);
		}

		public static void LoadFilesInto(string[] battleRequires, Table battleTable)
		{
			foreach (string require in battleRequires)
			{
				LoadFileInto(require, battleTable);
			}
		}

		/// <summary>
		/// Loada file name into a table.
		/// </summary>
		public static DynValue LoadFileInto(string file, Table into)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to LoadFileInto before the environment is ready.");

			if (into == null)
			{
				DebugLogger.LogError("Cannot load code into null table.", LogContext.Lua, LogPriority.High);
				return null;
			}

			Profiler.BeginSample($"Lua.LoadFileInto({file})");
			try
			{
				LuaChangeWatcher.Use(file);

				try
				{
					var      code = (string)scriptLoader.LoadFile(file, into);
					DynValue dv   = envScript.DoString(code, into, file, file);
					return dv;
				}
				catch (InterpreterException sre)
				{
					DebugLogger.LogError(sre.GetPrettyString(into), LogContext.Lua, LogPriority.High);
				}
				catch (Exception e)
				{
					DebugLogger.LogError($"Lua: LoadIntoTable failed with the error '{e}'", LogContext.Lua, LogPriority.High);
				}
			}
			finally
			{
				Profiler.EndSample();
			}

			return DynValue.Nil;
		}

		public static void glb_import_index(Table targetTable, object parentObject)
		{
			envScript.Call(envGlobals.Get("import_index"), new[] { targetTable, parentObject });
		}

		public static Table glb_get_asset_table(DynValue asset = null)
		{
			return Lua.envScript.Call(Lua.envGlobals.Get("get_asset_table"), asset ?? DynValue.Nil).Table;
		}

		// INVOCATION
		// ----------------------------------------

		public static DynValue Invoke(LuaAsset asset)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to Invoke before the environment is ready.");

			Table script = NewEnv(asset.name);
			try
			{
				return envScript.DoString(asset.TranspiledText, script, asset.name, asset.name);
			}
			catch (InterpreterException e)
			{
				DebugLogger.LogError(e.GetPrettyString(script), LogContext.Lua, LogPriority.High);
			}

			return DynValue.Nil;
		}

		public static DynValue InvokeSafe(Closure func, object[] args = null) => func == null ? DynValue.Nil : Invoke(func, args);

		public static DynValue Invoke(Closure func, object[] args = null)
		{
			try
			{
				return args == null ? envScript.Call(func) : envScript.Call(func, args);
			}
			catch (InterpreterException e)
			{
				DebugLogger.LogError(e.GetPrettyString(), LogContext.Lua, LogPriority.High);
			}

			return DynValue.Nil;
		}

		/// <summary>
		/// Invoke a function on a table.
		/// Does nothing special aside from rigorous error printing.
		/// </summary>
		/// <returns>The result from the function, otherwise null if the invocation has failed.</returns>
		public static DynValue Invoke(Table script, string funcname, object[] args = null, bool optional = false)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to Invoke before the environment is ready.");

			if (script == null)
			{
				DebugLogger.LogError("Lua: Trying to invoke function on a null script table.", LogContext.Lua, LogPriority.High);
				return DynValue.Nil;
			}

			Profiler.BeginSample($"Lua.Invoke({funcname})");
			try
			{
				DynValue dvfunc = script.Get(funcname);
				if (dvfunc.IsNil())
				{
					if (!optional) DebugLogger.LogError($"Lua: '{funcname}'() could not be found in the script table '{script.GetEnvName()}'.", LogContext.Lua, LogPriority.High);
					return DynValue.Nil;
				}

				if (dvfunc.Type != DataType.Function)
				{
					DebugLogger.LogError($"Lua: '{funcname}' was found in the script table '{script.GetEnvName()}', but is not a function.", LogContext.Lua, LogPriority.High);
					return DynValue.Nil;
				}

				try
				{
					return args == null
						? envScript.Call(dvfunc, LuaUtil.NO_ARGS)
						: envScript.Call(dvfunc, args);
				}
				catch (InterpreterException e)
				{
					DebugLogger.LogError(e.GetPrettyString(script), LogContext.Lua, LogPriority.High);
				}
			}
			finally
			{
				Profiler.EndSample();
			}


			return DynValue.Nil;
		}


		[CanBeNull]
		public static CoroutineInstance CreateCoroutine([NotNull] Closure function, object[] args = null)
			=> CreateCoroutine(null, function, args);

		[CanBeNull]
		public static CoroutineInstance CreateCoroutine(Table script, Closure func, object[] args = null)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to InvokeCoroutine before the environment is ready.");

			try
			{
				if (func == null)
				{
					DebugLogger.LogError("Lua: Function is null.", LogContext.Lua, LogPriority.High);
					return null;
				}

				Coroutine coroutine = envScript.CreateCoroutine(func).Coroutine;
				return new CoroutineInstance(script, func, coroutine, args);
			}
			catch (InterpreterException e)
			{
				DebugLogger.LogError(e.GetPrettyString(), LogContext.Lua, LogPriority.High);
				return null;
			}
		}

		/// <summary>
		/// Invoke a function of a table as a managed coroutine.
		/// </summary>
		/// <param name="script">The script table which contains the function.</param>
		/// <param name="funcName">The function to invoke.</param>
		/// <param name="add_to_runner">Whether the coroutine should be automatically managed by the global coroutine runner.</param>
		/// <param name="args">The arguments to pass to the function.</param>
		/// <param name="optional">Sets whether or not this is an optional invocation, i.e. firing an event. No errors will be printed.</param>
		/// <returns>A CoroutineInstance if the invocation was successful, otherwise null.</returns>
		[CanBeNull]
		public static CoroutineInstance CreateCoroutine([CanBeNull] Table script, string funcName, object[] args = null, bool optional = false)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to InvokeCoroutine before the environment is ready.");

			if (script == null)
			{
				DebugLogger.LogError("Lua: Trying to invoke coroutine on a null script table.", LogContext.Lua, LogPriority.High);
				return null;
			}

			if (string.IsNullOrEmpty(funcName))
			{
				DebugLogger.LogError("Lua: Attempting to InvokeCoroutine with a null or empty function name!", LogContext.Lua, LogPriority.High);
				return null;
			}

			try
			{
				DynValue dfunc = script.Get(funcName);

				if (dfunc.IsNil())
				{
					if (!optional) DebugLogger.LogError($"Lua: Could not find a function '{funcName}' in the script '{script.GetEnvName()}'.", LogContext.Lua, LogPriority.High);
					return null;
				}

				if (dfunc.Type != DataType.Function)
				{
					DebugLogger.LogError($"Lua: Found function '{funcName}', but it is not a function.", LogContext.Lua, LogPriority.High);
					return null;
				}

				return CreateCoroutine(script, dfunc.Function, args);
			}
			catch (InterpreterException e)
			{
				DebugLogger.LogError(e.GetPrettyString(), LogContext.Lua, LogPriority.High);
				return null;
			}
		}

		[CanBeNull]
		public static CoroutineInstance RunCoroutine([NotNull] Closure function, object[] args = null)
			=> RunCoroutine(null, function, args);

		[CanBeNull]
		public static CoroutineInstance RunCoroutine([NotNull] Table script, [NotNull] Closure function, object[] args = null)
		{
			// return RunCoroutine(script, function, args);
			CoroutineInstance coroutine = CreateCoroutine(script, function, args);
			if (coroutine != null)
				_activeCoroutines.Add(coroutine);
			return coroutine;
		}

		[CanBeNull]
		public static CoroutineInstance RunCoroutine([NotNull] Table script, [NotNull] string funcName, object[] args = null, bool optional = false)
		{
			CoroutineInstance coroutine = CreateCoroutine(script, funcName, args, optional);
			if (coroutine != null)
				_activeCoroutines.Add(coroutine);
			return coroutine;
		}


		/// <summary>
		/// Invoke a function in a table with a coroutine player.
		/// The player will immediately start playing, CoroutinePlayer should not be called manually.
		/// </summary>
		[CanBeNull]
		public static Coplayer RunPlayer([CanBeNull] Table script, string funcName, object[] args = null, bool optional = false)
		{
			if (!Ready) throw new InvalidOperationException("Lua: call to InvokePlayer before the environment is ready.");

			if (script == null)
			{
				DebugLogger.LogError("Lua: Trying to InvokePlayer on a null script table.", LogContext.Lua, LogPriority.High);
				return null;
			}

			CoroutineInstance instance = CreateCoroutine(script, funcName, args, optional);
			if (instance == null)
				// Error message printing already handled in InvokeCoroutine.
				return null;

			Coplayer player = RentPlayer(true);
			player.Play(script, instance).Forget();
			return player;
		}

		/// <summary>
		/// Invoke a function in a table with a coroutine player.
		/// The player will immediately start playing, CoroutinePlayer should not be called manually.
		/// </summary>
		[CanBeNull]
		public static Coplayer RunPlayer([CanBeNull] Table script, Closure func, object[] args = null, bool optional = false)
		{
			if (!Ready) throw new InvalidOperationException("Lua: call to InvokePlayer before the environment is ready.");

			if (script == null)
			{
				DebugLogger.LogError("Lua: Trying to InvokePlayer on a null script table.", LogContext.Lua, LogPriority.High);
				return null;
			}

			CoroutineInstance instance = CreateCoroutine(script, func, args);
			if (instance == null)
				// Error message printing already handled in InvokeCoroutine.
				return null;

			Coplayer player = RentPlayer(true);
			player.Play(script, instance).Forget();
			return player;
		}

		/// <summary>
		/// Invoke a function in any of the global scripts.
		/// If the function is in several global scripts they will all run, making
		/// this very suitable for wiring game events.
		/// This runs the function as a free agent which can run over
		/// a period of time (using coroutine yields + waitables) and animate parts of the the game.
		/// </summary>
		public static List<Coplayer> RunGlobal(string funcName, object[] args = null, bool optional = false, bool manual_start = false)
		{
			if (!Ready)
				throw new InvalidOperationException("Lua: call to InvokeGlobal before the environment is ready.");

			_tmpCoroutines2.Clear();

			foreach (Table script in GlobalScripts)
			{
				if (script == null)
				{
					DebugLogger.LogWarning("Lua: null in global scripts. Skipping...", LogContext.Lua, LogPriority.Low);
					continue;
				}

				CoroutineInstance coroutine = CreateCoroutine(script, funcName, args, optional);
				if (coroutine == null)
					continue;

				Coplayer coplayer = RentPlayer();
				coplayer.Prepare(script, coroutine);
				if (!manual_start)
					coplayer.Play().Forget();

				_tmpCoroutines2.Add(coplayer);
			}

			if (_tmpCoroutines2.Count == 0 && !optional)
				"Lua".LogError($"InvokeGlobal: Could not find a global function '{funcName}'.");

			return _tmpCoroutines2;
		}

		public static bool FindFirstGlobal<T>(string key, out T value)
		{
			value = default;
			foreach (Table script in GlobalScripts)
			{
				if (script == null)
				{
					DebugLogger.LogWarning("Lua: null in global scripts. Skipping...", LogContext.Lua, LogPriority.Low);
					continue;
				}

				if (script.TryGet(key, out T _v))
				{
					value = _v;
					return true;
				}
			}

			return false;
		}

		public static bool FindFirstGlobal<T>(string key, out T value, out Table script)
		{
			value  = default;
			script = null;

			foreach (Table _script in GlobalScripts)
			{
				if (_script == null)
				{
					DebugLogger.LogWarning("Lua: null in global scripts. Skipping...", LogContext.Lua, LogPriority.Low);
					continue;
				}

				if (_script.TryGet(key, out T _v))
				{
					value  = _v;
					script = _script;
					return true;
				}
			}

			return false;
		}

		public static List<DynValue> FindAllGlobal(string key)
		{
			List<DynValue> values = new List<DynValue>();

			foreach (Table script in GlobalScripts)
			{
				if (script == null)
				{
					DebugLogger.LogWarning("Lua: null in global scripts. Skipping...", LogContext.Lua, LogPriority.Low);
					continue;
				}

				DynValue _v;
				if (script.TryGet(key, out _v))
				{
					values.Add(_v);
				}
			}

			return values;
		}

		private static readonly List<Coplayer> _tmpCoplayers = new List<Coplayer>();

		public static void InvokeGlobal(string funcName, object[] args = null, bool optional = false)
		{
			if (_state < InitState.Ready)
				throw new InvalidOperationException("Lua: call to InvokeGlobal before the environment is ready.");

			foreach (Table script in GlobalScripts)
			{
				if (script == null)
				{
					DebugLogger.LogWarning("Lua: null in global scripts. Skipping...", LogContext.Lua, LogPriority.Low);
					continue;
				}

				Invoke(script, funcName, args, optional);
			}
		}

		public static DynValue InvokeGlobalFirst(string funcName, object[] args = null)
		{
			for (var i = 0; i < GlobalScripts.Count; i++)
			{
				Table script = GlobalScripts[i];

				foreach (TablePair pair in script.Pairs)
				{
					if (pair.Key.Type == DataType.String &&
					    pair.Value.Type == DataType.Function &&
					    pair.Key.String == funcName)
					{
						return Invoke(pair.Value.Function, args);
					}
				}
			}

			return DynValue.Nil;
		}

		/// <summary>
		/// A common functionality for one-off scripts like LuaOnSwordHit.
		/// Will either call in global or invoke a new instance of the script if it's been set.
		/// </summary>
		[NotNull]
		public static List<Coplayer> RunScriptOrGlobal(string function, LuaAsset scriptAsset, object[] args = null)
		{
			_tmpCoroutines1.Clear();
			if (string.IsNullOrEmpty(function)) return _tmpCoroutines1;

			if (scriptAsset != null)
			{
				Table    script = NewScript(scriptAsset);
				Coplayer player = RunPlayer(script, function, args);
				_tmpCoroutines1.Add(player);
			}
			else
			{
				List<Coplayer> players = RunGlobal(function, args, true);
				_tmpCoroutines1.AddRange(players);
			}

			if (_tmpCoroutines1.Count == 0)
			{
				Live.LogWarn("No global function 'function' found to execute.");
			}

			return _tmpCoroutines1;
		}

		/// <summary>
		/// Schedule a reload of for a global script of a specific LuaAsset.
		/// </summary>
		/// <param name="script"></param>
		/// <returns></returns>
		public static void ScheduleReload(LuaAsset script)
		{
			RemoveGlobalScript(script);
			AddGlobalScript(script);
		}

		public static void LogTrace(string op, string msg)
		{
			AjLog.Log("Lua", op, $"({_timer.Elapsed.ToString(ELAPSED_FORMAT)}) {msg}");
		}

		public static void LogError(string op, string msg)
		{
			AjLog.LogError("Lua", op, $"({_timer.Elapsed.ToString(ELAPSED_FORMAT)}) {msg}");
		}

#if UNITY_EDITOR
		[MenuItem("Anjin/Workflow/Reload Lua")]
		private static void ReloadLua()
		{
			_state = InitState.Zero;
			Initialize();
		}
#endif
	}
}
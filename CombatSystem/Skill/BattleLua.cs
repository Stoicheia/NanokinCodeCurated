using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Combat.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using static Combat.LuaEnv;

namespace Combat
{
	/// <summary>
	/// A combat Lua object. It is effectively the main API definition for combat.
	/// - Adds important requires for combat.
	/// - Supports LuaChangeWatcher.
	/// -
	/// </summary>
	public abstract class BattleLua : BattleResource
	{
		/// <summary>
		/// The script to load into this host. (by name, as would be used for a require clause)
		/// </summary>
		public string scriptName;

		/// <summary>
		/// The script to load into this host. (by specific asset)
		/// </summary>
		public LuaAsset scriptAsset;

		// State
		public bool      initialized;
		public Fighter   user;
		public Targeting targeting;

		// Internal state
		protected LuaEnv       baseEnv;
		protected List<LuaEnv> instances = new List<LuaEnv>();
		protected AnimSM       animsm;

		protected Table baseEnvTable => baseEnv.table;

		[NotNull]
		public string EnvID => scriptAsset != null
			? scriptAsset.name
			: scriptName ?? "lua";

		public bool IsForkingRequested => baseEnvTable.TryGet("forking", false);

		protected BattleLua(Battle battle, LuaAsset asset = null, string scriptName = null)
		{
			this.battle     = battle;
			targeting       = new Targeting();
			animsm          = new AnimSM(this);
			scriptAsset     = asset;
			this.scriptName = scriptName;
		}

		[CanBeNull]
		protected virtual ScriptStore ScriptStore => null;

		[NotNull]
		protected virtual string EnvName => "combat-script-host";

		public virtual void Cleanup()
		{
			LuaChangeWatcher.ClearWatches(this);
			battle    = null;
			user      = null;
			targeting = null;
			baseEnv   = None;
		}

		public virtual void Reinitialize()
		{
			initialized = false;
			Initialize();
		}

		protected virtual void Initialize()
		{
			if (initialized)
			{
				DebugLogger.LogError("BattleLua is already initialized!", LogContext.Combat, LogPriority.Low);
				return;
			}

			LuaChangeWatcher.BeginCollecting();
			baseEnv = CreateEnv();
			LuaChangeWatcher.EndCollecting(this, Reinitialize);
		}

		protected LuaEnv GetBaseEnvOrFork() => IsForkingRequested ? Fork() : baseEnv;

		protected LuaEnv Fork()
		{
			// var ret = new BattleLuaEnv(animsm, Lua.NewEnv(EnvName, imports: false));
			// ret.table.Set(baseEnvTable);

			// Remove dead instances
			// TODO collect and reuse
			for (var i = 0; i < instances.Count; i++)
			{
				LuaEnv instance = instances[i];
				if (!instance.IsAlive)
					instances.RemoveAt(i--);
			}

			LuaEnv ret = CreateEnv();
			instances.Add(ret);
			return ret;
		}


		private LuaEnv CreateEnv()
		{
			var   env  = new LuaEnv(animsm, Lua.NewEnv(EnvName), EnvID);
			Table envt = env.table;

			// API & Framework
			LuaUtil.RegisterGlobals(envt, typeof(ProcAPI), false);
			Lua.LoadFilesInto(LuaUtil.battleRequires, envt);

			envt["up"]        = _stateUp;
			envt["low"]       = _stateLow;
			envt["scale"]     = _stateScale;
			envt["max"]       = _stateMax;
			envt["min"]       = _stateMin;
			envt["forbid"]    = _stateForbid;
			envt["restrict"]  = _stateRestrict;
			envt["randomize"] = _stateRandomize;
			envt["set"]       = _stateSet;

			UpdateGlobals(ref env);

			// Store resources
			if (ScriptStore != null)
				ScriptStore?.WriteToTable(envt);

			// The script itself
			if (scriptName != null)
				Lua.LoadFileInto(scriptName, envt);
			else if (scriptAsset != null)
				Lua.LoadAssetInto(scriptAsset, envt);
			else
			{
				this.LogError($"BattleLua.CreateEnv(): invalid script asset for '{EnvName}')");
			}

			return env;
		}

		protected virtual void UpdateGlobals(ref LuaEnv env)
		{
			Table envt = env.table;

#if UNITY_EDITOR
			// Update, in case we changed something in the inspector
			ScriptStore?.WriteToTable(env.table);
#endif

			env.dealer = user;
			animsm.SetAutoTarget(targeting);

			envt[OBJ_ANIMSM] = animsm;
			envt[OBJ_BATTLE] = battle;
			if (battle?.animated == true)
				envt[OBJ_ARENA] = battle?.arena;

			envt[OBJ_USER] = user;
			if (user != null)
				envt[OBJ_USER_HOME] = user.home;

			envt[OBJ_BRAIN] = battle?.GetBaseBrain(user);

			envt[OBJ_TARGETING] = targeting;
			if (targeting != null && targeting.picks.Count > 0)
			{
				Target t = targeting.picks[0];
				envt[OBJ_TARGET]         = t;
				envt[OBJ_TARGET_FIGHTER] = GetTargetObject();
			}
			else
			{
				envt[OBJ_TARGET]         = Target.Empty;
				envt[OBJ_TARGET_FIGHTER] = null;
			}
		}

		/// <summary>
		/// A contextual object of the target. If there is only one fighter/
		/// One fighter/slot --> the fighter/slot itself
		/// Multiple fighter/slots ---> the target object
		/// Multiple targets --> the targeting container
		/// </summary>
		/// <returns></returns>
		protected object GetTargetObject()
		{
			// Many picks
			if (targeting.picks.Count == 0) return targeting;
			if (targeting.picks.Count > 1) return targeting;

			Target t = targeting.picks[0];

			if (t.fighters.Count > 0 && t.slots.Count > 0) return t; // This is a weird scenario, targeting both slots and fighters..?
			if (t.fighters.Count == 1) return t.Fighter;
			if (t.slots.Count == 1) return t.Slot;

			return t;
		}

		protected DynValue Invoke(LuaEnv env, string name, [CanBeNull] object[] args = null, bool optional = false)
		{
			UpdateGlobals(ref env);
			return Lua.Invoke(env.table, name, args, optional);
		}

		protected DynValue Invoke(string name, [CanBeNull] object[] args = null, bool optional = false)
		{
			UpdateGlobals(ref baseEnv);
			return Lua.Invoke(baseEnvTable, name, args, optional);
		}

		protected DynValue Invoke(Closure closure, [CanBeNull] object[] args = null, bool optional = false)
		{
			UpdateGlobals(ref baseEnv);
			return Lua.Invoke(closure, args);
		}

		protected virtual void OnEnvCreated(LuaEnv env) { }

		private static readonly DynValue _stateUp        = UserData.CreateStatic(typeof(StateAPI_up));
		private static readonly DynValue _stateLow       = UserData.CreateStatic(typeof(StateAPI_low));
		private static readonly DynValue _stateScale     = UserData.CreateStatic(typeof(StateAPI_scale));
		private static readonly DynValue _stateMax       = UserData.CreateStatic(typeof(StateAPI_max));
		private static readonly DynValue _stateMin       = UserData.CreateStatic(typeof(StateAPI_min));
		private static readonly DynValue _stateForbid    = UserData.CreateStatic(typeof(StateAPI_forbid));
		private static readonly DynValue _stateRestrict  = UserData.CreateStatic(typeof(StateAPI_restrict));
		private static readonly DynValue _stateRandomize = UserData.CreateStatic(typeof(StateAPI_randomize));
		private static readonly DynValue _stateSet       = UserData.CreateStatic(typeof(StateAPI_set));
	}
}
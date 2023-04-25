using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using SaveFiles.Elements.Inventory.Items.Scripting;
using UnityEngine;

namespace Combat
{
	public struct LuaEnv
	{
		// Self Properties
		// ----------------------------------------
		public const string FUNC_TAG            = "tag";
		public const string FUNC_USABLE         = "usable";
		public const string FUNC_USABLE_MENU    = "usable_menu";
		public const string FUNC_GET_ERROR      = "get_error";
		public const string FUNC_GET_ERROR_MENU = "get_error_menu";
		public const string FUNC_COST           = "cost";
		public const string FUNC_DISPLAY_NAME   = "display_name";
		public const string FUNC_DESCRIPTION    = "description";

		public const string FUNC_CONSUME = "consume";

		// API Events
		// ----------------------------------------
		public const string FUNC_INIT      = "init";
		public const string FUNC_PASSIVE   = "passive";
		public const string FUNC_TARGET    = "target";
		public const string FUNC_TARGET_UI = "target_ui";
		public const string FUNC_PREPARE   = "prepare";
		public const string FUNC_UNPREPARE = "unprepare";
		public const string FUNC_END_DATA  = "end_data";
		public const string FUNC_USE       = "use";
		public const string FUNC_LOAD      = "load";
		public const string FUNC_SAVE      = "save";

		// API Animations
		// ----------------------------------------
		public const string FUNC_PASSIVE_ANIM = "__passive";
		public const string FUNC_USE_ANIM     = "__use";
		public const string FUNC_CUSTOM_ANIM  = "__action";

		// API Objects
		// ----------------------------------------
		public const string OBJ_ANIMSM         = "asm";
		public const string OBJ_BATTLE         = "battle";
		public const string OBJ_ARENA          = "arena";
		public const string OBJ_USER           = "user";
		public const string OBJ_USER_HOME      = "home";
		public const string OBJ_TARGETING      = "choices";
		public const string OBJ_TARGET         = "choice";
		public const string OBJ_TARGET_FIGHTER = "victim";
		public const string OBJ_TARGET_SLOT    = "victim";
		public const string OBJ_BRAIN          = "brain";
		public const string OBJ_PROCS          = "procs"; // ProcTable for a BattleAction, bound for the duration of the action's animation function.

		// API Properties
		// ----------------------------------------
		public const string PROP_SHOW_RETICLE   = "show_reticle";
		public const string FUNC_TARGET_DEFAULT = "pick_fighter";

		public static LuaEnv None => new LuaEnv
		{
			lua       = false,
			_asmstack = new Stack<AnimSM>(),
		};

		// Env
		// ----------------------------------------
		public  BattleResource       owner;
		public  bool                 lua;
		public  string               id;
		public  Table                table;
		public  Table                istore;
		public  List<BattleResource> dependencies;
		private AnimSM               _animsm;
		private Stack<AnimSM>        _asmstack;

		// Context
		// ----------------------------------------
		public Battle battle;
		[CanBeNull]
		public BattleSkill skill;
		[CanBeNull]
		public LuaPlugin plugin;
		[CanBeNull]
		public BattleSticker sticker;
		[CanBeNull]
		public Fighter dealer;

		public LuaEnv(AnimSM animsm, Table table, [CanBeNull] string id) : this()
		{
			if (string.IsNullOrEmpty(id))
				Debug.LogError("ID cannot be empty.");

			owner      = null;
			this.table = table;
			this.id    = id;
			_animsm    = animsm;

			lua          = true;
			dependencies = new List<BattleResource>();
			_asmstack    = new Stack<AnimSM>();

			// Set("env", animsm);
		}

		public LuaEnv(LuaEnv from, [CanBeNull] string id)
		{
			if (!from.lua)
				Debug.LogError("Cannot copy a non-Lua environment.");

			owner = null;

			this.id = string.IsNullOrEmpty(id)
				? $"{from.id}"
				: $"{from.id}/{id}";

			lua          = true;
			table        = from.table;
			dependencies = from.dependencies;
			_animsm      = from._animsm;
			_asmstack    = from._asmstack;

			istore = Lua.NewTable();
			if (from.istore != null)
			{
				// iterate pairs and copy
				foreach (TablePair pair in from.istore.Pairs)
					istore.Set(pair.Key, pair.Value);
			}


			// Transfer some values from the global env table (frozen state)
			if (from.table.ContainsKey(OBJ_TARGET)) istore.Set(OBJ_TARGET, from.table.Get(OBJ_TARGET));
			if (from.table.ContainsKey(OBJ_TARGETING)) istore.Set(OBJ_TARGETING, from.table.Get(OBJ_TARGETING));
			if (from.table.ContainsKey(OBJ_TARGET_SLOT)) istore.Set(OBJ_TARGET_SLOT, from.table.Get(OBJ_TARGET_SLOT));
			if (from.table.ContainsKey(OBJ_TARGET_FIGHTER)) istore.Set(OBJ_TARGET_FIGHTER, from.table.Get(OBJ_TARGET_FIGHTER));

			battle  = from.battle;
			skill   = from.skill;
			plugin  = from.plugin;
			sticker = from.sticker;
			dealer  = from.dealer;
		}

		public bool IsAlive => dependencies.All(d => d.IsAlive); // TODO replace LINQ with code

		public void RemoveDeadDependencies()
		{
			throw new NotImplementedException(); // TODO
		}

		/// <summary>
		/// Check if the env contains the value
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey([CanBeNull] string key)
		{
			if (key == null) return false;
			return lua && table.ContainsKey(key);
		}

		/// <summary>
		/// Set a value in the environment.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="v"></param>
		public void iset(string key, DynValue v)
		{
			if (!lua) return;
			if (istore == null) throw new Exception("Instance store is null");

			istore.Set(key, v);
		}

		/// <summary>
		/// Get a value from the environment.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public DynValue iget(string key)
		{
			if (!lua) return DynValue.Nil;
			if (istore == null) throw new Exception("Instance store is null");

			return istore.Get(key);
		}

		public bool ihas(string key)
		{
			if (!lua) return false;
			if (owner == null) return false;
			var dv = istore.Get(key);
			return dv.IsNotVoid() && dv.IsNotNil();
		}

		public void iunpack(LuaEnv genv)
		{
			foreach (TablePair pair in istore.Pairs)
			{
				genv.Set($"i{pair.Key.String}", pair.Value);
			}
		}

		public void iclear(LuaEnv genv)
		{
			foreach (TablePair pair in istore.Pairs)
			{
				genv.table.Remove(pair.Key);
			}
		}

		/// <summary>
		/// Get a value from the environment.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public DynValue Get(string key)
		{
			if (!lua) return DynValue.Nil;
			return table.Get(key);
		}


		/// <summary>
		/// Set a value in the environment.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="v"></param>
		public void Set(string key, object v)
		{
			if (!lua) return;
			table[key] = v;
		}

		/// <summary>
		/// Gets the value of the specified key as a string, if it exists.
		/// Otherwise, the default value.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="v"></param>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		public bool TryGet<TValue>(string key, out TValue v)
		{
			v = default;
			if (!lua) return false;

			return table.TryGet(key, out v);
		}

		public void Push(AnimSM asm)
		{
			_asmstack.Push(asm);
			if (lua)
				Set("api", asm);
		}

		public void Pop(AnimSM asm)
		{
			if (_asmstack.Peek() != asm)
				throw new InvalidOperationException("asm stack is out of sync");

			_asmstack.Pop();

			if (lua)
				Set("asm", _asmstack.Count > 0
					? _asmstack.Peek()
					: _animsm);
		}

		public LuaEnv Or([CanBeNull] BattleResource parent)
		{
			return parent == null
				? this
				: Or(parent.GetEnv());
		}

		public LuaEnv Or(LuaEnv env)
		{
			// return env if lua is false
			return !lua
				? env
				: this;
		}
	}
}
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Combat.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Puppets.Assets;
using UnityEngine;
using UnityEngine.Profiling;
using static Combat.LuaEnv;


namespace Combat
{
	/// <summary>
	/// An instance of a fighter's skill, which is a Lua script host and more.
	/// Check skill-script.lua for documentation and usage.
	/// </summary>
	public sealed class BattleSkill : BattleLua
	{
		public const string REF_SELF_SKILL = "self_skill";

		public bool discardInstance = false;

		public SkillAsset     asset;
		public ScriptableLimb limb;

		protected override string EnvName => $"skill-{asset.name}";

		protected override ScriptStore ScriptStore => asset.luaPackage.Store;

		public bool IsPassive => baseEnvTable.ContainsKey(FUNC_PASSIVE) && !baseEnvTable.ContainsKey(FUNC_USE);

		public string Address => asset.Address;

		public BattleSkill(Battle battle, [NotNull] SkillAsset asset) : base(battle, asset.luaPackage.Asset) //
		{
			this.asset = asset;
			Reinitialize();
		}

		public override void Reinitialize()
		{
			Profiler.BeginSample($"BattleSkill.Reset: {asset}");
			base.Reinitialize();

			if (!baseEnvTable.ContainsKey(FUNC_USE) &&
			    !baseEnvTable.ContainsKey(FUNC_PASSIVE) &&
			    !baseEnvTable.ContainsKey(FUNC_INIT))
			{
				DebugLogger.LogError($"The script table {baseEnvTable.GetEnvName()} has neither {FUNC_USE} nor {FUNC_PASSIVE} defined. The skill is useless.", LogContext.Combat, LogPriority.High);
			}

			Profiler.EndSample();
		}

		protected override void OnEnvCreated(LuaEnv env)
		{
			env.skill = this;
		}

		protected override void UpdateGlobals(ref LuaEnv env)
		{
			base.UpdateGlobals(ref env);

			env.table[REF_SELF_SKILL] = asset;
		}

		public bool HasTag([NotNull] string tag)
		{
			DynValue dvtag = baseEnvTable.Get(FUNC_TAG);

			while (dvtag.IsNotNil())
			{
				if (dvtag.AsFunction(out Closure func))
				{
					dvtag = Lua.Invoke(func);
				}
				else if (dvtag.AsString(out string str))
				{
					return tag == str;
				}
				else if (dvtag.AsTable(out Table tbl))
				{
					for (var i = 1; i <= tbl.Length; i++)
					{
						string dvstr = tbl.Get(i).String;
						if (dvstr == tag)
							return true;
					}

					return false;
				}
			}

			return false;
		}

		[CanBeNull]
		public BattleAnim Init()
		{
			if (!baseEnvTable.ContainsKey(FUNC_INIT))
				return null;

			targeting.Clear();
			targeting.AddPick(new Target(user));

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);
			Invoke(FUNC_INIT);
			return animsm.EndInstant();
		}

		[CanBeNull]
		public BattleAnim Passive()
		{
			if (!baseEnvTable.ContainsKey(FUNC_PASSIVE))
				return null;

			targeting.Clear();
			targeting.AddPick(new Target(user));

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);
			Invoke(FUNC_PASSIVE);
			return animsm.EndCoplayer(FUNC_PASSIVE_ANIM);
		}

		public (bool, string) Usable()
		{
			DynValue dvusable = baseEnvTable.Get(FUNC_USABLE);

			while (dvusable.IsNotNil())
			{
				if (dvusable.AsBool(out bool usable)) return (usable, null);
				else if (dvusable.AsString(out string str)) return (false, str);
				else if (dvusable.AsFunction(out Closure func)) dvusable = Invoke(func, optional: true);
			}

			return (true, "");
		}

		public string GetError()
		{
			DynValue dvusable = baseEnvTable.Get(FUNC_GET_ERROR);

			while (dvusable.IsNotNil())
			{
				if (dvusable.AsString(out string error))
				{
					return error;
				}
				else if (dvusable.AsFunction(out Closure func))
				{
					dvusable = Invoke(FUNC_GET_ERROR, optional: true);
				}
			}

			return "";
		}

		public string DisplayName()
		{
			DynValue dvusable = baseEnvTable.Get(FUNC_DISPLAY_NAME);

			while (dvusable.IsNotNil())
			{
				if (dvusable.AsString(out string error))
				{
					return error;
				}
				else if (dvusable.AsFunction(out Closure func))
				{
					dvusable = Invoke(FUNC_DISPLAY_NAME, optional: true);
				}
			}

			return "";
		}

		public string Description()
		{
			DynValue dvusable = baseEnvTable.Get(FUNC_DESCRIPTION);

			while (dvusable.IsNotNil())
			{
				if (dvusable.AsString(out string error))
				{
					return error;
				}
				else if (dvusable.AsFunction(out Closure func))
				{
					dvusable = Invoke(FUNC_DESCRIPTION, optional: true);
				}
			}

			return "";
		}

		public int Cost()
		{
			// Cost
			DynValue dvcost = baseEnvTable.Get(FUNC_COST);

			while (dvcost.IsNotNil())
			{
				if (dvcost.AsTable(out Table costtbl))
				{
					// ret.hpcost = costtbl.TryGet("hp", 0);
					return costtbl.TryGet("sp", 0);
				}
				else if (dvcost.AsFloat(out float spcost))
				{
					return (int)spcost;
				}
				else if (dvcost.AsFunction(out Closure func))
				{
					dvcost = Lua.Invoke(func);
				}
			}

			return 0;
		}

		public void Target([NotNull] Targeting target)
		{
			targeting = target;
			UpdateGlobals(ref baseEnv);

			DynValue dvtarget  = baseEnvTable.Get(FUNC_TARGET);
			DynValue dvreticle = baseEnvTable.Get(PROP_SHOW_RETICLE);

			if (!dvreticle.IsNil() && dvreticle.AsBool(out bool showReticle))
			{
				targeting.showSlotReticle = showReticle;
			}

			if (dvtarget.IsNil())
			{
				Invoke(FUNC_TARGET_DEFAULT);
			}
			else if (dvtarget.AsFunction(out Closure closure))
			{
				Invoke(closure);
			}
			else if (dvtarget.AsString(out string shape))
			{
				List<Target> ret = TargetAPI.shape(battle, user, shape);
				targeting.AddOptions(ret);
				// Invoke("pick_shape", LuaUtil.Args(shape));
			}
		}

		[CanBeNull]
		public BattleAnim Prepare()
		{
			if (!baseEnvTable.ContainsKey(FUNC_PREPARE))
				return null;

			LuaEnv env = GetBaseEnvOrFork();
			UpdateGlobals(ref env);

			animsm.Start(env);
			animsm.Invoke(FUNC_PREPARE, out DynValue _, LuaUtil.Args(GetTargetObject()));
			var ret = animsm.EndInstant();
			return ret;
		}

		[CanBeNull]
		public BattleAnim Unprepare()
		{
			if (!baseEnvTable.ContainsKey(FUNC_UNPREPARE))
				return null;

			LuaEnv env = GetBaseEnvOrFork();
			UpdateGlobals(ref env);

			animsm.Start(env);
			animsm.Invoke(FUNC_UNPREPARE, out DynValue _, LuaUtil.Args(GetTargetObject()));
			var ret = animsm.EndInstant();
			return ret;
		}

		/// <summary>
		/// TODO what does this do?? -oxy
		/// Usage example:
		/// function end_data()
		///		return {
		///			slot = {...}
		///			points = {...}
		///		}
		/// end
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		public Dictionary<string, Table> EndData()
		{
			Dictionary<string, Table> toReturn = new Dictionary<string, Table>();
			if (!baseEnvTable.ContainsKey(FUNC_END_DATA))
				return null;

			LuaEnv env = GetBaseEnvOrFork();
			UpdateGlobals(ref env);

			animsm.Start(env);
			if (animsm.Invoke(FUNC_END_DATA, out DynValue dtable, LuaUtil.Args(GetTargetObject())) && dtable.AsTable(out Table table))
			{
				foreach (TablePair pair in table.Pairs)
				{
					DynValue key   = pair.Key;
					DynValue value = pair.Value;
					if (key.AsString(out string prop) && value.AsTable(out Table info))
					{
						toReturn[prop] = info;
					}
				}
			}

			return toReturn;
		}

		[CanBeNull]
		public BattleAnim Use()
		{
			LuaEnv env = GetBaseEnvOrFork();
			UpdateGlobals(ref env);

			animsm.Start(env);
			Invoke(env, FUNC_USE, LuaUtil.Args(GetTargetObject()));
			var ret = animsm.EndCoplayer(FUNC_USE_ANIM, null, true);
			return ret;
		}

		[CanBeNull]
		public BattleAnim Load(FighterInfo info)
		{
			if (!baseEnvTable.ContainsKey(FUNC_LOAD))
				return null;

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);

			Invoke(FUNC_LOAD, LuaUtil.Args(info));
			return animsm.EndInstant();
		}

		[CanBeNull]
		public BattleAnim Save(FighterInfo info)
		{
			if (!baseEnvTable.ContainsKey(FUNC_SAVE))
				return null;

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);

			Invoke(FUNC_SAVE, LuaUtil.Args(info));
			return animsm.EndInstant();
		}

		[CanBeNull]
		private BattleAnim Animable(int i)
		{
			return animsm.EndCoplayer($"{FUNC_CUSTOM_ANIM}{i}");
		}
	}
}
using System.Collections.Generic;
using Anjin.Scripting;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.Core.Flags
{
	//This one cannot be static since it needs the this[] indexer
	//[LuaUserdata(RegisterStatic = true, LuaStaticName = "Flags")]
	[LuaUserdata]
	public class FlagsLua
	{
		public int    get_int(string    path) => Flags.GetFlagValue<IntFlag, int>(path);
		public float  get_float(string  path) => Flags.GetFlagValue<FloatFlag, float>(path);
		public bool   get_bool(string   path) => Flags.GetFlagValue<BoolFlag, bool>(path);
		public string get_string(string path) => Flags.GetFlagValue<StringFlag, string>(path);

		public void set_int(string    path, int    val) => Flags.SetInt(path, val);
		public void set_float(string  path, float  val) => Flags.SetFloat(path, val);
		public void set_bool(string   path, bool   val) => Flags.SetBool(path, val);
		public void set_string(string path, string val) => Flags.SetString(path, val);

		public void increment(string path, int amount) => Flags.Increment(path, amount);


		/// <summary>
		/// Retrieve the value of a flag by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[LuaGlobalFunc]
		public static object flag(string name)
		{
			var flag = Flags.Find(name);

			if (flag == null)
			{
				AjLog.LogWarn($"Cannot get unknown flag '{name}', returning null. (will coerce to false in lua)", nameof(FlagsLua), nameof(flag));
				return null;
			}

			return flag.GetValue();
		}

		/// <summary>
		/// Check that all flags are true.
		/// Only bool flags are supported.
		/// </summary>
		/// <param name="names"></param>
		/// <returns></returns>
		[LuaGlobalFunc]
		public static bool flags(List<string> names)
		{
			var value = true;

			foreach (string name in names)
			{
				FlagStateBase flag = Flags.Find(name);

				switch (flag)
				{
					case BoolFlag b:
						value = value && b.Value;
						break;

					case null:
						AjLog.LogError($"Cannot get unknown flag '{name}', returning null. (will coerce to false in lua)", nameof(FlagsLua), nameof(flags));
						break;
				}
			}

			return value;
		}

		/// <summary>
		/// Let the condition pass once when the flag isn't set,
		/// but only once. The flag will be set for next time.
		///
		/// Good for things like entrance cutscenes that only play once,
		/// e.g.:
		///
		/// if flag_once("fp_introduced_dude") then
		///		-- crazy cutscene
		/// end
		///
		/// -- rest of the script
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[LuaGlobalFunc]
		public static bool flag_once(string name)
		{
			object result = flag(name);
			if (result is BoolFlag bflag)
			{
				if (!bflag.Value)
					flag_set(name, true);

				return !bflag.Value;
			}

			return false;
		}

		[LuaGlobalFunc]
		public static object flag_state(string name) => Flags.Find(name);

		[LuaGlobalFunc]
		public static void flag_set(string name, object val = null)
		{
			if (val == null)
				val = true; // Allow flag_set('name') to set true (we will often set flags true, much more than true-->false)

			var flag = Flags.Find(name);
			if (flag == null)
			{
				if (val is bool)
				{
					var def = new BoolFlagDef { Name = name };
					flag = new BoolFlag(def);

					FlagDefDatabase.LoadedDB.Flags.Add(def);
					Flags.Live.AllFlags.Add(flag);
					Flags.Live.NameRegistry[name] = flag;

					Debug.Log($"Created missing bool flag '{name}'.");
				}
				else if (val is int)
				{
					var def = new IntFlagDef { Name = name };
					flag = new IntFlag(def);

					FlagDefDatabase.LoadedDB.Flags.Add(def);
					Flags.Live.AllFlags.Add(flag);
					Flags.Live.NameRegistry[name] = flag;
					Debug.Log($"Created missing int flag '{name}'.");
				}
				else if (val is string)
				{
					var def = new StringFlagDef { Name = name };
					flag = new StringFlag(def);

					FlagDefDatabase.LoadedDB.Flags.Add(def);
					Flags.Live.AllFlags.Add(flag);
					Flags.Live.NameRegistry[name] = flag;
					Debug.Log($"Created missing string flag '{name}'.");
				}
				else if (val is float)
				{
					var def = new FloatFlagDef { Name = name };
					flag = new FloatFlag(def);

					FlagDefDatabase.LoadedDB.Flags.Add(def);
					Flags.Live.AllFlags.Add(flag);
					Flags.Live.NameRegistry[name] = flag;
					Debug.Log($"Created missing float flag '{name}'.");
				}
				else
				{
					AjLog.LogError($"Cannot create missing flag '{name}' because the type {val.GetType()} is unsupported.", nameof(FlagsLua), nameof(flag_set));
					return;
				}
			}


			flag.SetValue(val);
		}


		[LuaGlobalFunc]
		public static void flag_listener(List<string> flags, string listenerID, Closure listener)
		{
			foreach (string name in flags)
			{
				FlagStateBase flag = Flags.Find(name);

				if (flag == null)
				{
					AjLog.LogError($"Cannot listen on unknown flag '{name}', skipping...", nameof(FlagsLua), nameof(flag_listener));
					continue;
				}

				flag.AddListener(listenerID, listener);
			}
		}
	}
}
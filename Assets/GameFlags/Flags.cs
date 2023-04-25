using System;
using System.Collections.Generic;
using Anjin.Util;
using ImGuiNET;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Core.Flags
{
	public class Flags : StaticBoy<Flags>
	{
		public const string DBG_NAME = "Flags";

		[NonSerialized, ShowInPlay]
		public List<FlagStateBase> AllFlags;

		[NonSerialized, ShowInPlay]
		public Dictionary<string, FlagStateBase> IDRegistry;
		[NonSerialized, ShowInPlay]
		public Dictionary<string, FlagStateBase> NameRegistry;

		public static Action<FlagStateBase> OnAnyFlagUpdated;

		public static Action<BoolFlag>   boolChanged;
		public static Action<IntFlag>    intChanged;
		public static Action<FloatFlag>  floatChanged;
		public static Action<StringFlag> stringChanged;

		private static bool _init;

		public override void Awake()
		{
			base.Awake();
			EnsureInit();
			DebugSystem.onLayoutDebug += ONLayoutDebug;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void OnInit()
		{
			_init            = false;
			OnAnyFlagUpdated = null;
		}

		public void EnsureInit()
		{
			if (_init) return;
			_init    = true;
			AllFlags = new List<FlagStateBase>();

			IDRegistry   = new Dictionary<string, FlagStateBase>();
			NameRegistry = new Dictionary<string, FlagStateBase>();

			for (int i = 0; i < FlagDefDatabase.LoadedDB.Flags.Count; i++)
			{
				RegisterFlag(FlagDefDatabase.LoadedDB.Flags[i]);
			}
		}

		void RegisterFlag(FlagDefinitionBase definition)
		{
			if (IDRegistry.ContainsKey(definition.ID) ||
			    NameRegistry.ContainsKey(definition.Name)) return;

			FlagStateBase state = null;

			switch (definition)
			{
				case BoolFlagDef boolFlag:
					state = new BoolFlag(boolFlag);
					break;

				case FloatFlagDef floatFlag:
					state = new FloatFlag(floatFlag);
					break;

				case IntFlagDef intFlag:
					state = new IntFlag(intFlag);
					break;

				case StringFlagDef stringFlag:
					state = new StringFlag(stringFlag);
					break;
			}

			AllFlags.Add(state);

			IDRegistry[definition.ID]     = state;
			NameRegistry[definition.Name] = state;
		}

		/*private void Update()
		{
			for (int i = 0; i < AllFlags.Count; i++) {
				var f = AllFlags[i];

			}
		}*/

		public void Refresh()
		{
			for (int i = 0; i < FlagDefDatabase.LoadedDB.Flags.Count; i++)
				RegisterFlag(FlagDefDatabase.LoadedDB.Flags[i]);
		}

		public static void ResetAll()
		{
			foreach (FlagStateBase flag in Live.AllFlags) {
				flag.ResetValue();
			}
		}

		public static FlagStateBase Find(string name)
		{
			Live.EnsureInit();
			return Live.NameRegistry.GetOrDefault(name);
		}

		public static F Find<F>(string path) where F : FlagStateBase
		{
			Live.EnsureInit();
			return (F) Live.NameRegistry.GetOrDefault(path);
		}

		public static bool Find<F>(string path, out F flag) where F : FlagStateBase
		{
			Live.EnsureInit();
			bool found = Live.NameRegistry.TryGetValue(path, out FlagStateBase _flag);
			flag = _flag as F;
			return found;
		}

		public static V GetFlagValue<F, V>(string path) where F : Flag<V>
		{
			var flag = Find<F>(path);
			return (flag != null) ? flag.Value : default;
		}

		public static void SetFlagValue<F, V>(string path, V val) where F : Flag<V>
		{
			var flag = Find<F>(path);
			if (flag != null)
				flag.Value = val;
		}

		public static bool FlagExists(string path) => Live.NameRegistry.ContainsKey(path);

		public static void SetBool(string   path, bool   val) => SetFlagValue<BoolFlag, bool>(path, val);
		public static void SetFloat(string  path, float  val) => SetFlagValue<FloatFlag, float>(path, val);
		public static void SetInt(string    path, int    val) => SetFlagValue<IntFlag, int>(path, val);
		public static void SetString(string path, string val) => SetFlagValue<StringFlag, string>(path, val);

		public static bool   GetBool(string   path) => GetFlagValue<BoolFlag, bool>(path);
		public static float  GetFloat(string  path) => GetFlagValue<FloatFlag, float>(path);
		public static int    GetInt(string    path) => GetFlagValue<IntFlag, int>(path);
		public static string GetString(string path) => GetFlagValue<StringFlag, string>(path);

		public static void Increment(string name, int amount)
		{
			FlagStateBase flag = Find<FlagStateBase>(name);

			switch (flag)
			{
				case IntFlag iflag:
					iflag.Value += amount;
					break;

				case FloatFlag fflag:
					fflag.Value += amount;
					break;

				case null:
					AjLog.LogError($"Cannot increment unknown flag '{name}'.", nameof(Flags), nameof(Increment));
					break;
			}
		}

		public void Decrement(string path, int amount)
		{
			FlagStateBase flag = Find<FlagStateBase>(path);

			switch (flag)
			{
				case IntFlag iflag:
					iflag.Value -= amount;
					break;

				case FloatFlag fflag:
					fflag.Value -= amount;
					break;

				case null:
					AjLog.LogError($"Cannot decrement unknown flag '{name}'.", nameof(Flags), nameof(Decrement));
					break;
			}
		}


		void ONLayoutDebug(ref DebugSystem.State state)
		{
			if (state.Begin(DBG_NAME))
			{
				ImGui.Columns(2);

				for (int i = 0; i < AllFlags.Count; i++)
				{
					ImGui.PushID(i);
					var flag = AllFlags[i];


					ImGui.Text(flag.DefBase.Name);

					ImGui.NextColumn();

					switch (flag)
					{
						case BoolFlag boolFlag:
							bool b = boolFlag.Value;
							ImGui.Checkbox("", ref b);
							boolFlag.Value = b;
							break;

						case FloatFlag floatFlag:
							ImGui.InputFloat("", ref floatFlag._value);
							break;

						case IntFlag intFlag:
							ImGui.InputInt("", ref intFlag._value);
							break;

						case StringFlag stringFlag:
							ImGui.InputText("", ref stringFlag._value, 1000);
							break;
					}

					ImGui.NextColumn();

					ImGui.PopID();
				}


				ImGui.Columns();
				ImGui.End();
			}
		}
	}
}
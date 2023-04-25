using System;
using System.Linq;
using Anjin.Scripting;
using Assets.Nanokins;
using Combat.Scripting;
using Combat.Startup;
using Combat.UI;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Util.Addressable;
using Util.Odin.Attributes;
using Util.UnityEditor.Launch;
using static Combat.LuaEnv;

namespace Combat
{
#if UNITY_EDITOR
	[BasePath(BASE_PATH)]
#endif
	[Serializable, Inline]
	[LuaUserdata]
	public class SkillAsset : SerializedScriptableObject, IAddressable, ILuaObject
	{
		[Title("Info")]
		public string DisplayName;

		[Multiline]
		public string Description;

		[Optional]
		public Sprite Icon;

		[Tooltip("Set to true if the skill should not be usable by anyone. (may crash the game)")]
		public bool IsWIP;

		public bool ShowNameOnUse = true;

		public bool CustomDisplayName = false;
		public bool CustomDescription = false;

		//public bool Physical = false;
		//public bool Magical = false;

		[Title("Script", HorizontalLine = false)]
		[OdinSerialize, NonSerialized]
		[Inline]
		public LuaScriptPackage luaPackage = new LuaScriptPackage
		{
			Asset = null,
			Store = new ScriptStore()
		};

		public string Address { get; set; }

		public bool IsValid(bool ignore_wip = false) => (!IsWIP || ignore_wip) && luaPackage.Asset != null;

		/// <summary>
		/// Information to be evaluated by executing the script.
		/// </summary>
		public struct EvaluatedInfo
		{
			public             bool   passive;
			public             int    spcost;
			public             int    hpcost;
			public             bool   variableCost;
			[CanBeNull] public string scriptLoadError;
			public             string displayName;
			public             string description;

			public TargetingInfo? targetingInfo;
		}

		public struct TargetingInfo
		{
			public TargetingInfoSlot[,] slots;

			public TargetingInfo(int i)
			{
				// TODO: Pool
				slots = new TargetingInfoSlot[3, 3];
			}
		}

		public struct TargetingInfoSlot
		{
			public bool targeted;
		}

		/// <summary>
		/// Evaluate information that can only be guessed
		/// from the script.
		/// </summary>
		/// <returns></returns>
		public EvaluatedInfo EvaluateInfo([CanBeNull] TargetUILua targetUI = null)
		{
			var ret = new EvaluatedInfo();
			ret.scriptLoadError = null;

			LuaAsset asset = luaPackage.Asset;
			if (!asset)
			{
				ret.scriptLoadError = $"Error: Script for skill {name} failed to load!";
				return ret;
			}

			Table script = Lua.NewEnv($"skill-detailing-{name}");
			script["ui"] = targetUI;

			Lua.LoadFilesInto(LuaUtil.battleUIRequires, script);
			Lua.LoadAssetInto(luaPackage.Asset, script);

			//Skill name
			if (!CustomDisplayName)
			{
				ret.displayName = DisplayName;
			}
			else
			{
				DynValue dvdisplayname = script.Get(FUNC_DISPLAY_NAME);

				if (dvdisplayname.IsNil())
				{
					ret.displayName = DisplayName;
				}
				else
				{
					if (dvdisplayname.AsString(out string displayName))
					{
						ret.displayName = displayName;
					}
					else if (dvdisplayname.AsFunction(out Closure name_func))
					{
						DynValue result = name_func.Call(LimbMenu.input);

						if (result.AsString(out string displayname))
						{
							ret.displayName = displayname;
						}
					}
				}
			}

			//Skill description
			if (!CustomDescription)
			{
				ret.description = Description;
			}
			else
			{
				DynValue dvdescription = script.Get(FUNC_DESCRIPTION);

				if (dvdescription.IsNil())
				{
					ret.description = Description;
				}
				else
				{
					if (dvdescription.AsString(out string description))
					{
						ret.description = description;
					}
					else if (dvdescription.AsFunction(out Closure desc_func))
					{
						DynValue result = desc_func.Call(LimbMenu.input);

						if (result.AsString(out string desc))
						{
							ret.description = desc;
						}
					}
				}
			}

			// Passive
			DynValue dvpassive = script.Get(FUNC_PASSIVE);
			DynValue dvcast    = script.Get(FUNC_USE);
			ret.passive = dvpassive.IsNotNil() && dvcast.IsNil();


			// Cost
			DynValue dvcost = script.Get(FUNC_COST);
			if (dvcost.AsTable(out Table costtbl))
			{
				ret.hpcost = costtbl.TryGet("hp", 0);
				ret.spcost = costtbl.TryGet("sp", 0);
			}
			else if (dvcost.AsFunction(out Closure _))
			{
				ret.variableCost = true;
			}
			else if (dvcost.AsFloat(out float spcost))
			{
				ret.spcost = (int)spcost;
			}


			// Targeting
			if (targetUI != null)
			{
				DynValue targeting_ui = script.Get(FUNC_TARGET_UI);
				DynValue targeting    = script.Get(FUNC_TARGET);

				if (targeting_ui.AsFunction(out Closure ui_func))
				{
					try
					{
						ui_func.Call(targetUI);
					}
					catch (Exception e)
					{
						DebugLogger.LogException(e);
					}
				}
				else if (targeting.AsFunction(out Closure func))
				{
					Lua.Invoke(func);
				}
			}

			return ret;
		}

		public override string ToString() => $"SkillAsset({name})";


#if UNITY_EDITOR
		public const string BASE_PATH = "Assets/Combat/Skills";

		private void OnValidate()
		{
			Addressables2.SetDefaultDirect(ref luaPackage.Asset, this);
		}

		[Button(ButtonSizes.Large), GUIColor(0, 1, 0), PropertyOrder(-1)]
		private void Test()
		{
			SkillDevRecipe recipe = InternalEditorConfig.Instance.LastLauncherRecipe as SkillDevRecipe ?? new SkillDevRecipe
			{
				internalRecipe = InternalEditorConfig.Instance.LastLauncherRecipe // Wrap the last recipe with a SkillDevRecipe
			};

			BattleConsoleConfig editorConfig = InternalEditorConfig.Instance.battleConsole;
			InternalEditorConfig.Instance.LastTestedSkill = this;

			recipe.EnemyTeamRecipe = new TeamRecipe(editorConfig.EnemyNanokins.Cast<MonsterRecipe>().ToList());
			recipe.overrideBrain   = new DebugBrain();

			DebugBrain.DefaultSkill = this;
			NanokinLauncher.LaunchCombat(recipe);
		}
#endif
		[CanBeNull]
		public LuaAsset Script => luaPackage.Asset;

		public ScriptStore LuaStore
		{
			get => luaPackage.Store;
			set => luaPackage.Store = value;
		}

		public string[] Requires => LuaUtil.battleRequires;
	}
}
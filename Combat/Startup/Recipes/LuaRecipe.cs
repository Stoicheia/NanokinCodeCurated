using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Assets.Nanokins;
using Combat.Components;
using Combat.Entities;
using Combat.Scripting;
using Combat.Startup;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Pathfinding.Util;
using SaveFiles;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Util.Odin.Attributes;

namespace Combat.Launch
{
	[Serializable]
	public class LuaRecipe : BattleRecipe, ILuaObject
	{
		public const int DEFAULT_LEVEL = 30;

		[CanBeNull]
		public LuaAsset Script => luaPackage.Script;

		private Table      _env;
		private List<Chip> _chips;

		[Title("Script", HorizontalLine = false)]
		[OdinSerialize, NonSerialized]
		[Inline]
		public LuaScriptPackage luaPackage = new LuaScriptPackage
		{
			Asset = null,
			Store = new ScriptStore()
		};

		public ScriptStore LuaStore
		{
			get => luaPackage.Store;
			set => luaPackage.Store = value;
		}

		public string[] Requires => luaPackage.Requires;

		protected override async UniTask OnBake()
		{
			_chips = new List<Chip>();

			_env           = Lua.NewEnv(Script.name);
			_env["btl"]    = this;
			_env["battle"] = this;

			LuaStore.WriteToTable(_env);
			Lua.LoadAssetInto(Script, _env);

			// Automatically mark the first team as player if none of them are player
			bool none = true;
			foreach (Team t in battle.teams)
			{
				if (t.isPlayer)
				{
					none = false;
					break;
				}
			}

			if (none)
				battle.teams[0].isPlayer = true;

			_env = null;

			foreach (Chip chip in _chips)
			{
				await runner.Hook(chip);
			}
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class Proxy : LuaProxy<LuaRecipe>
		{
			private Battle battle => proxy.battle; //

			private UniTaskBatch loadingBatch => proxy.loadingBatch;

			/// <summary>
			/// Make the battle loop endlessly when the fight ends.
			/// </summary>
			public void loop()
			{
				proxy._chips.Add(new LoopFightChip());
			}

			public void plugin(string name)
			{
				proxy.runner.instancePlugins.Add(new LuaPlugin(name));
			}

			// [UsedImplicitly]
			// public Team team()
			// {
			// 	Team t = battle.AddTeam();
			// 	AfterTeamCreate(t, null);
			// 	return t;
			// }

			[UsedImplicitly]
			public Team team([CanBeNull] Table conf = null)
			{
				BattleBrain brain = null;

				if (conf.TryGet("brain", out Closure ai))
				{
					brain = new UtilityAIBrain(proxy._env, ai);
					if (!proxy._env.ContainsKey("need"))
						Lua.LoadFileInto(UtilityAIBrain.API_FILE, proxy._env);
				}
				else if (conf.TryGet("brain", out string stringBrain) || conf.TryGet("skill", out stringBrain))
				{
					if (LuaUtil.ParseEnum(stringBrain, out BattleBrains brainType))
					{
						brain = CombatAPI.Brain(brainType);
					}
					else
					{
						string skillname = GameAssets.Skills.FirstOrDefault(s => s.Contains(stringBrain));
						if (skillname != null)
						{
							SkillAsset skill = GameAssets.GetSkill(skillname);
							brain = new SkillTestBrain(skill);
						}
					}
				}
				else if (conf.TryGet("brain", out SkillAsset dskill) || conf.TryGet("skill", out dskill))
					brain = new SkillTestBrain(dskill);
				else if (conf.ContainsKey("brain") && conf.Get("brain").IsNil()) // Explicitly no brain
					brain = new SkipTurnBrain();
				else
					brain = new AutoBrain();

				if (proxy.overrideBrain != null)
					brain = proxy.overrideBrain;

				Team t = battle.AddTeam(brain);
				conf.TryGet("player", out t.isPlayer);

				AfterTeamCreate(t, conf);
				return t;
			}

			[UsedImplicitly]
			[NotNull]
			public Team team_player([NotNull] Table conf)
			{
				Team t = battle.AddTeam(new PlayerBrain(), auslots: true);
				t.isPlayer = true;
				AfterTeamCreate(t, conf);

				return t;
			}

			[UsedImplicitly]
			[NotNull]
			public Fighter stub(string name, int level = DEFAULT_LEVEL, Vector2Int? pos = null)
			{
				var instance = new NanokinInstance(level, name);
				var inf      = new NanokinInfo(instance);

				foreach (LimbInstance limb in instance.Limbs)
					limb.Mastery = 3;

				Fighter fter = battle.AddFighter(
					inf,
					turns: false,
					auslot: true,
					islot: pos);

				proxy.InstantiateNanokin(fter, inf).Batch(loadingBatch);

				return fter;
			}

			public void team_random_demo(DynValue dsize, DynValue dlevel, DynValue dmastery, [CanBeNull] Table conf = null)
			{
				int size    = LuaAPI.int_or_range(dsize);
				int level   = LuaAPI.int_or_range(dlevel);
				int mastery = LuaAPI.int_or_range(dmastery);

				Team t = team(conf);

				for (int i = 0; i < size; i++)
				{
					Fighter fter = demo_nanokin(level, mastery);
					if (fter != null)
						t.AddFighter(fter);
				}
			}

			public void team_random(DynValue dsize, DynValue dlevel, DynValue dmastery, [CanBeNull] Table conf = null)
			{
				int size    = LuaAPI.int_or_range(dsize);
				int level   = LuaAPI.int_or_range(dlevel);
				int mastery = LuaAPI.int_or_range(dmastery);

				Team t = team(conf);

				for (int i = 0; i < size; i++)
				{
					t.AddFighter(any_nanokin(level, mastery));
				}
			}

			[UsedImplicitly]
			[CanBeNull]
			public Fighter nanokin([NotNull] Table limbs, int level = DEFAULT_LEVEL, int mastery = 3, Vector2Int? pos = null)
			{
				LimbInstance arm1 = new LimbInstance($"{limbs.Get("arm1").String}-arm1", mastery);
				LimbInstance arm2 = new LimbInstance($"{limbs.Get("arm2").String}-arm2", mastery);
				LimbInstance head = new LimbInstance($"{limbs.Get("head").String}-head", mastery);
				LimbInstance body = new LimbInstance($"{limbs.Get("body").String}-body", mastery);

				return nanokin(new NanokinInstance(level, head, body, arm1, arm2), level, pos);
			}

			[UsedImplicitly]
			[CanBeNull]
			public Fighter nanokin(string name, int level = DEFAULT_LEVEL, int mastery = 3, Vector2Int? pos = null)
			{
				if (!GameAssets.HasNanokin(name))
				{
					DebugLogger.LogError($"Couldn't find nanokin with ID: '{name}'", LogContext.Combat, LogPriority.High);
					return null;
				}

				return nanokin(new NanokinInstance(level, name), level, pos);
			}

			[UsedImplicitly]
			[CanBeNull]
			public Fighter nanokin([NotNull] NanokinInstance nano, int level = DEFAULT_LEVEL, Vector2Int? pos = null)
			{
				foreach (LimbInstance limb in nano.Limbs)
					limb.Mastery = 3;

				var inf = new NanokinInfo(nano);

				Fighter fter = battle.AddFighter(
					inf,
					auslot: true,
					islot: pos);

				proxy.InstantiateNanokin(fter, inf).Batch(loadingBatch);

				return fter;
			}

			public void generic(string prefabAddress, string infoAddress, Vector2Int? pos = null)
			{
				proxy.CreateGenericFighterFromAddresses(prefabAddress, infoAddress, pos).Batch(loadingBatch);

				//return fter;
			}

			[UsedImplicitly]
			[CanBeNull]
			public Fighter demo_nanokin(int level, int mastery)
			{
				List<NanokinAsset> tentative = ListPool<NanokinAsset>.Claim();

				try
				{
					string choose = GameAssets.Nanokins
						.Where(addr => Regex.Match(addr, Addresses.DemoNanokinRegex).Success)
						.Choose();

					return nanokin(choose, level, mastery);
				}
				finally
				{
					ListPool<NanokinAsset>.Release(tentative);
				}
			}

			[UsedImplicitly, NotNull]
			public Fighter any_nanokin(int level, int mastery)
			{
				return nanokin(GameAssets.Nanokins.Choose(), level, mastery) ?? throw new InvalidOperationException();
			}

			private void AfterTeamCreate(Team team, [CanBeNull] Table conf)
			{
				// Collect fighters in the team
				if (conf != null)
				{
					for (var i = 1; i <= conf.Length; i++)
					{
						Fighter fter = conf.Get(i).AsUserdata<Fighter>() ?? throw new InvalidOperationException();
						team.AddFighter(fter, false);
					}
				}

				team.AutoSlots(false, conf.TryGet<string>("shape"));

				if (conf.TryGet("autocoach", false) || team.isPlayer)
				{
					SetCoachCharactersAuto(team);
				}

				team.EnsureCoachPlots();
			}


			private static readonly string[] _standardCharacters =
			{
				"nas",
				"serio",
				"jatz",
				"peggie",
				"david",
				"neha"
			};

			private void SetCoachCharactersAuto(Team team)
			{
				for (var i = 0; i < team.fighters.Count; i++)
				{
					Fighter ft = team.fighters[i];

					var charEntry = new CharacterEntry(_standardCharacters.WrapGet(i))
					{
						Level = ft.info.Level,
					};

					if (ft.info is NanokinInfo nanokinInfo)
					{
						charEntry.nanokin = nanokinInfo.instance;
					}

					Coach coach = proxy.AddCharacterCoach(charEntry, ft);

					proxy.InstantiateCoach(coach).Batch(loadingBatch);
				}
			}
		}
	}
}
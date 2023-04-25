using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Assets.Nanokins;
using Combat.Components;
using Combat.Data;
using Combat.Entities;
using Combat.Entry;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Shops;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI.Extensions;
using Util.Addressable;
using Object = UnityEngine.Object;

namespace Combat.Startup
{
	[Serializable]
	public abstract class BattleRecipe
	{
		public const string NANOKIN_FIGHTER_ADDR      = "Combat/Nanokin Fighter";
		public const string GENERIC_FIGHTER_BASE_ADDR = "Combat/Generic Fighter Base";

		[NonSerialized] public BattleRunner runner;
		[NonSerialized] public UniTaskBatch loadingBatch;
		[NonSerialized] public bool         baked;

		private Func<bool> _funcBaked;

		[OdinSerialize]
		[HideInInspector]
		public BattleBrain overridePlayerBrain;

		[OdinSerialize]
		[HideInInspector]
		public BattleBrain overrideEnemyBrain;

		[OdinSerialize]
		[HideInInspector]
		public BattleBrain overrideBrain;

		[OdinSerialize]
		[HideInInspector]
		public GameObject sharedCoachPrefab;

		//public Arena        arena   => core.io.arena;
		//public BattleState  battle  => core.state;
		//public AsyncHandles handles => core.handles;

		public Arena        arena   => runner.io.arena;
		public Battle       battle  => runner.battle;
		public AsyncHandles handles => runner.handles;

		public virtual LootDropInfo loots => LootDropInfo.Empty();

		/// <summary>
		/// Used to await the baking process with UniTask.Until()
		/// </summary>
		[NotNull]
		public Func<bool> FuncBaked => _funcBaked ?? (_funcBaked = () => baked);

		public virtual async UniTask Bake()
		{
			loadingBatch = new UniTaskBatch();

			if (GameOptions.current.combat_fast_warmup)
			{
				await runner.Hook(new DebugWarmupChip());
			}

			await OnBake();

			baked = true;

			InitRecipeLoots();
			foreach (Fighter ft in battle.fighters)
			{
				if (ft.team?.isPlayer == false)
				{
					AddLoots(ft);
				}
			}

			await loadingBatch;
		}

		protected abstract UniTask OnBake();

		public virtual UniTask LoadAssets() => UniTask.CompletedTask;

		/// <summary>
		/// Add a team from a team recipe, controller and optional list of slots.
		/// </summary>
		public async UniTask<Team> AddTeam([NotNull] Team team, [NotNull] TeamRecipe teamRecipe, [CanBeNull] List<Slot> overrideSlots = null)
		{
			// int teamsize = teamRecipe.Monsters.Count;

			UniTaskBatch batch = UniTask2.Batch();

			for (var i = 0; i < teamRecipe.Monsters.Count; i++)
				batch.Add(AddFighter(team, teamRecipe.Monsters[i], overrideSlots));

			if (team.coach != null)
			{
				InstantiateCoach(team.coach, null).Batch(loadingBatch);
			}

			await batch;

			team.EnsureFighterHomes();
			team.EnsureCoachPlots();

			return team;
		}

		/// <summary>
		/// Create a fighter from a MonsterRecipe.
		/// Also creates its view.
		/// </summary>
		[CanBeNull]
		[ContractAnnotation("monster:null => null")]
		protected virtual async UniTask<Fighter> AddFighter(
			[NotNull]   Team          team,
			[CanBeNull] MonsterRecipe monster,
			[CanBeNull] List<Slot>    overrideSlots = null,
			int                       i             = -1,
			bool                      defaultAi     = true)
		{
			if (monster == null)
			{
				this.LogError("Skipping a null MonsterRecipe.");
				return default;
			}

			// Get info
			// ----------------------------------------
			FighterInfo fterInfo = monster.CreateInfo(handles);
			if (fterInfo == null)
				return default;

			// Get slot
			// ----------------------------------------
			SlotGrid grid = team.slots;
			Slot     slot;
			if (overrideSlots != null)
			{
				slot = overrideSlots[i];
			}
			else
			{
				slot = monster.slotcoord.HasValue
					? grid.GetSlotAt(monster.slotcoord.Value)
					: grid.GetDefaultSlot(i);
			}

			if (slot == null)
			{
				this.LogError("Could not obtain a slot for a fighter.");
				return default;
			}

			// Default AI
			// ----------------------------------------
			if (monster.brain == null && defaultAi && fterInfo.DefaultAI != null)
			{
				monster.brain = new UtilityAIBrain(fterInfo.DefaultAI);
			}

			// Create fighter
			// ----------------------------------------
			Fighter fter = battle.AddFighter(fterInfo,
				slot,
				team,
				monster.brain,
				turns: !monster.NoTurns);

			if (monster.stickers != null)
			{
				foreach (StickerAsset sticker in monster.stickers)
				{
					battle.AddSticker(fter, sticker);
				}
			}

			if (monster.ClaimsAllSlots)
			{
				for (int j = 0; j < battle.slots.Count; j++)
				{
					if (battle.slots[j].team == team)
					{
						battle.slots[j].owner = fter;
					}
				}
			}

			CharacterAsset character = null;

			if (monster.Character != null)
			{
				character = monster.Character;
			}
			else if (!monster.CharacterAddress.IsNullOrWhitespace())
			{
				character = await GameAssets.LoadAsset<CharacterAsset>(monster.CharacterAddress);
			}

			Coach coach;

			//Execute this block if only one coach is present per fighter (or no coach at all for other encounters)
			if (team.coach == null)
			{
				if (team.isPlayer)
				{
					coach = character
						? AddCoach(fter)
						: null;
				}
				else
				{
					coach = monster.CharacterPrefab
						? AddNanokeeperCoach(fter, monster.CharacterPrefab)
						: null;
				}
			}
			//Execute this block if all fighters on this team have the same coach
			else
			{
				fter.coach = team.coach;

				if (team.coach is NanokeeperCoach) //TODO: probably a better way to condense this block, but I don't have the time to look now
				{
					(team.coach as NanokeeperCoach).AddSummoned(fter);
				}

				coach = null;
			}

			// Create views
			// ----------------------------------------
			CreateView(monster, fter);

			if (coach != null)
			{
				if ((character != null) || (coach is NanokeeperCoach))
				{
					InstantiateCoach(coach, character).Batch(loadingBatch);
				}
			}

			return fter;
		}

		protected void CreateView(MonsterRecipe monster, Fighter fter)
		{
			FighterInfo fterInfo;
			if (monster is PrefabRecipe prefab)
			{
				CreateViewGenericPrefab(fter, prefab.Info.Info.ActorPrefab).Batch(loadingBatch);
				//CreateViewGenericPrefab(fter, prefab.Info.Info.ActorPrefabAddress).Batch(loadingBatch);

				//n-ocheckin
				//battle.AddState(fter, new State{life = -1, stats = { engineFlags = { EngineFlags.unmovable }}});
			}
			else
			{
				InstantiateNanokin(fter, (NanokinInfo)fter.info).Batch(loadingBatch);
			}
		}

	#region Stats & Data

		public void AddLoots([NotNull] Fighter fighter)
		{
			const float RANDOMIZATION = 0.02f; // +- 2% to make the number look cooler!

			runner.io.xpLoots   += (int)(fighter.info.XPLoot * (1 + RNG.FloatSigned * RANDOMIZATION));
			runner.io.rpLoots   += fighter.info.RPLoot;
			runner.io.itemLoots =  runner.io.itemLoots.CombineDropTables(fighter.info.ItemLoot);
		}

		public void InitRecipeLoots()
		{
			runner.io.itemLoots = LootDropInfo.Empty();
			runner.io.itemLoots = runner.io.itemLoots.CombineDropTables(loots);
		}

	#endregion

	#region Save Data

		public async UniTask AddRecipeOrSavedata([NotNull] Team team, [CanBeNull] TeamRecipe recipe)
		{
			if (recipe != null)
				await AddTeam(team, recipe);
			else
				await AddSavedata(team);
		}

		public async UniTask AddSavedata([NotNull] Team team)
		{
			SaveData save = await SaveManager.GetCurrentAsync();

			foreach (CharacterEntry entry in save.Party)
			{
				(Fighter fighter, NanokinInfo info) = AddCharacterFighter(entry, team);
				AddCharacterCoach(entry, fighter);

				InstantiateNanokin(fighter, info).Batch(loadingBatch);

				if (fighter.coach != null)
				{
					InstantiateCoach(fighter.coach).Batch(loadingBatch);
				}
			}

			team.EnsureFighterHomes();
			team.EnsureCoachPlots();
		}

		public (Fighter, NanokinInfo) AddCharacterFighter([NotNull] CharacterEntry character, [NotNull] Team team)
		{
			var  info = new NanokinInfo(character.nanokin);
			Slot slot = team.slots.GetSlotAt(character.FormationCoord);

			Fighter fighter = battle.AddFighter(info, slot, team);

			foreach (StickerInstance sticker in character.Stickers)
			{
				battle.AddSticker(fighter, sticker);
			}

			return (fighter, info);
		}

		[NotNull]
		public Coach AddCharacterCoach(CharacterEntry entry, [NotNull] Fighter fighter)
		{
			var coach = new Coach(fighter)
			{
				character = entry
			};

			fighter.coach = coach;
			return coach;
		}

		[NotNull]
		public Coach AddCoach([NotNull] Fighter fighter)
		{
			var coach = new Coach(fighter);

			fighter.coach = coach;
			return coach;
		}

		[NotNull]
		private NanokeeperCoach AddNanokeeperCoach([NotNull] Fighter fighter, GameObject prefab)
		{
			var coach = new NanokeeperCoach(fighter, prefab);

			fighter.coach = coach;
			return coach;
		}

	#endregion

	#region Views

		public async UniTask InstantiateCoach([NotNull] Coach coach, CharacterAsset asset = null)
		{
			GameObject prefab = null;

			if (coach is NanokeeperCoach)
			{
				prefab = (coach as NanokeeperCoach).Prefab;
			}
			else if (coach.character != null)
			{
				prefab = await handles.LoadAssetAsync(coach.character.asset.BattlePrefab);
			}
			else if (asset != null)
			{
				prefab = await handles.LoadAssetAsync(asset.BattlePrefab);
			}

			GameObject go = Object.Instantiate(prefab, coach.home.position, Quaternion.identity, null);
			SceneManager.MoveGameObjectToScene(go, runner.Scene);

			coach.actor = (!(coach is NanokeeperCoach) ? go.GetOrAddComponent<CoachActor>() : go.GetOrAddComponent<NanokeeperCoachActor>());

			runner.objects.Add(go);

			await UniTask.WaitUntil(FuncBaked);

			// After baking
			// ----------------------------------------

			coach.TeleportHome();
		}

		public async UniTask InstantiateNanokin([NotNull] Fighter fighter, [NotNull] NanokinInfo info)
		{
			await UniTask.SwitchToMainThread();
			GameObject obj = await Addressables2.InstantiateAsync(NANOKIN_FIGHTER_ADDR);

			NanokinFighterActor actor = obj.GetComponent<NanokinFighterActor>();
			await actor.ChangeNanokin(info.instance);

			runner.objects.Add(obj);
			fighter.set_actor(obj);


			await UniTask.WaitUntil(FuncBaked);

			// After baking
			// ----------------------------------------

			fighter.snap_home();

			if (fighter.team?.isPlayer == true)
				await StatusUI.AddFighter(fighter);
		}

		public async UniTask CreateGenericFighterFromAddresses(string prefabAddress, string infoAddress, Vector2Int? pos)
		{
			AsyncOperationHandle<GenericInfoAsset> infoAsset = await Addressables2.LoadHandleAsync<GenericInfoAsset>(infoAddress);

			Fighter fter = battle.AddFighter(infoAsset.Result.Info, auslot: true, islot: pos);

			await CreateViewGenericPrefab(fter, prefabAddress);
		}

		public async UniTask CreateViewGenericPrefab([NotNull] Fighter fighter, [NotNull] string address = GENERIC_FIGHTER_BASE_ADDR)
		{
			await UniTask.SwitchToMainThread();

			GameObject obj = await Addressables2.InstantiateAsync(address);

			GenericFighterActor actor = obj.GetComponent<GenericFighterActor>();
			actor.OnCreated();

			runner.objects.Add(obj);
			fighter.set_actor(obj);

			await UniTask.WaitUntil(FuncBaked);

			fighter.snap_home();

			if (fighter.team?.isPlayer == true)
				await StatusUI.AddFighter(fighter);
		}

		public async UniTask CreateViewGenericPrefab([NotNull] Fighter fighter, ComponentRef<GenericFighterActor> prefab)
		{
			await UniTask.SwitchToMainThread();

			GenericFighterActor actor = await prefab.InstantiateAsync();
			actor.OnCreated();

			runner.objects.Add(actor.gameObject);
			fighter.set_actor(actor.gameObject);

			await UniTask.WaitUntil(FuncBaked);

			fighter.snap_home();

			if (fighter.team?.isPlayer == true)
				await StatusUI.AddFighter(fighter);
		}

	#endregion

		[NotNull]
		public static BattleRecipe new_recipe([NotNull] Table config)
		{
			var recipe = new PlayerOpponent1V1();

			// Brain
			// ------------------------------

			if (config.TryGet("brain", out string brain))
			{
				switch (brain)
				{
					case "random":
						recipe.enemyBrain = new RandomBrain();
						break;
				}
			}

			//TODO: instantiate shared coach here

			// Monsters
			// ------------------------------

			var enemy = new TeamRecipe();
			recipe.EnemyTeamRecipe = enemy;

			for (var i = 1; i <= config.Length; i++)
			{
				MonsterRecipe monster = config.Get(i).AsObject<MonsterRecipe>();
				enemy.Monsters.Add(monster);
			}

			// Formation
			// ------------------------------

			if (config.TryGet("formation", out string formationString))
			{
				// Example:
				// formation = [[
				// 1##
				// #2#
				// 3##
				// ]]
				var y = 0;
				var x = 0;
				for (var i = 0; i < formationString.Length; i++)
				{
					char c = formationString[i];
					if (char.IsDigit(c))
					{
						var index = (int)char.GetNumericValue(c);
						enemy.Monsters[index - 1].slotcoord = new Vector2Int(x, y);
					}
					else if (c == '\n')
					{
						y++;
						x = 0;
					}

					if (!char.IsWhiteSpace(c))
					{
						x++;
					}
				}
			}

			return recipe;
		}
	}
}
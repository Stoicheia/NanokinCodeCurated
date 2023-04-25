using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Shops;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Collections;
using Util.Odin.Attributes;
using Combat.Startup;
using Combat;
using Data.Nanokin;
using Combat.Data;
using Combat.Entry;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.Utilities;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

#endif

namespace Combat.Startup
{
	[Serializable]
	public class NanokeeperEncounterRecipe : BattleRecipe
	{
		// TODO add infobox documentation about the weights
		//[FormerlySerializedAs("input")]
		//[RandomEncounterList]
		[DarkBox]
		[Inline]
		public RandomEncounterRecipe input;

		public RangeOrInt TeamSize = 1;

		public RangeOrInt Level = 1;

		public RangeOrInt Money = 200;

		public GameObject coachPrefab;

		public override LootDropInfo loots => input.PossibleEncounters[0].Drops;

		protected override async UniTask OnBake()
		{
			var loot = loots;
			loot.Money = Money;

			InitRecipeLoots();

			// Teams
			// ----------------------------------------
			Team playerTeam = battle.AddTeam(overridePlayerBrain ?? new PlayerBrain(), arena.GetSlotLayout("player"));
			Team enemyTeam  = battle.AddTeam(overrideEnemyBrain, arena.GetSlotLayout("enemy"));

			// Player team
			// ----------------------------------------
			UniTask playerTask = AddSavedata(playerTeam);
			playerTeam.isPlayer = true;

			// Enemy team
			// ----------------------------------------
			List<Slot> slots = RNG.Choose(enemyTeam.slots.all, TeamSize);

			for (int i = 0; i < TeamSize; i++)
			{
				SimpleNanokin bodyChoice = input.PossibleEncounters[0].Nanokins.Choose();
				SimpleNanokin headChoice = input.PossibleEncounters[0].Nanokins.Choose();
				SimpleNanokin arm1Choice = input.PossibleEncounters[0].Nanokins.Choose();
				SimpleNanokin arm2Choice = input.PossibleEncounters[0].Nanokins.Choose();

				ExistingNanokin nano = new ExistingNanokin(new NanokinInstance
				{
					Body  = new LimbInstance(bodyChoice.Nanokin.Body, input.Masteries),
					Head  = new LimbInstance(headChoice.Nanokin.Head, input.Masteries),
					Arm1  = new LimbInstance(arm1Choice.Nanokin.Arm1, input.Masteries),
					Arm2  = new LimbInstance(arm2Choice.Nanokin.Arm2, input.Masteries),
					Level = Level
				});

				await AddFighter(enemyTeam, nano, slots, i);
			}

			enemyTeam.EnsureFighterHomes();
			enemyTeam.EnsureCoachPlots();

			Debug.Log($"====================================");

			// Await
			// ----------------------------------------
			await playerTask;
		}

		[NotNull]
		private NanokeeperCoach AddNanokeeperCoach([NotNull] Fighter fighter)
		{
			var coach = new NanokeeperCoach(fighter, coachPrefab);

			fighter.coach = coach;
			return coach;
		}

		/// <summary>
		/// Create a fighter from a MonsterRecipe.
		/// Also creates its view.
		/// </summary>
		[CanBeNull]
		[ContractAnnotation("monster:null => null")]
		protected override async UniTask<Fighter> AddFighter(
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
			SlotGrid group = team.slots;
			Slot     slot;
			if (overrideSlots != null)
			{
				slot = overrideSlots[i];
			}
			else
			{
				slot = monster.slotcoord.HasValue
					? group.GetSlotAt(monster.slotcoord.Value)
					: group.GetDefaultSlot(i);
			}

			if (slot == null)
			{
				this.LogError("Could not obtain a slot for a fighter.");
				return default;
			}

			// Default AI
			// ----------------------------------------
			if (monster.brain == null && defaultAi)
			{
				monster.brain = new UtilityAIBrain(((fterInfo.DefaultAI != null) ? fterInfo.DefaultAI : "balanced"));
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

			if (team.isPlayer)
			{
				coach = character
					? AddCoach(fter)
					: null;
			}
			else
			{
				coach = AddNanokeeperCoach(fter);
			}

			// Create views
			// ----------------------------------------
			CreateView(monster, fter);

			if (coach != null)
				InstantiateCoach(coach, character).Batch(loadingBatch);


			return fter;
		}
	}
}
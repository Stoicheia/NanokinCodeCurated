using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using Data.Shops;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Collections;
using Util.Odin.Attributes;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

#endif

namespace Combat.Startup
{
	[Serializable]
	public class RandomEncounterRecipe : BattleRecipe
	{
		// TODO add infobox documentation about the weights
		[FormerlySerializedAs("possibleEncounters")]
		[RandomEncounterList]
		[DarkBox]
		[Inline]
		public List<Entry> PossibleEncounters = new List<Entry>();

		public RangeOrInt Masteries = 1;

		private static readonly WeightMap<Entry> _entries = new WeightMap<Entry>();

		private Entry _lastEntry = null;
		private Entry _pickedEntry;

		public override LootDropInfo loots => _pickedEntry.Drops;

		private Entry GetEntry()
		{
			foreach (Entry entry in PossibleEncounters)
				_entries.Add(entry, entry.Weight);

			Entry choice = _entries.Choose();

			_entries.Clear();
			_pickedEntry = choice;
			return choice;
		}

		protected override async UniTask OnBake()
		{
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
			Entry choice = runner.hasRestarted
				? _lastEntry
				: GetEntry();

			_lastEntry = choice;

			List<Slot> slots = RNG.Choose(enemyTeam.slots.all, choice.Nanokins.Count);
			//List<Slot> slots = new List<Slot>() { battle.GetSlot(new Vector2Int(4, 0)), battle.GetSlot(new Vector2Int(4, 1)), battle.GetSlot(new Vector2Int(4, 2)), battle.GetSlot(new Vector2Int(5, 0)), battle.GetSlot(new Vector2Int(5, 1)) };

			Debug.Log($"ENEMIES:");

			for (var i = 0; i < choice.Nanokins.Count; i++)
			{
				SimpleNanokin recipe = choice.Nanokins[i];
				recipe.Mastery = Masteries;

				Fighter fighter = await AddFighter(enemyTeam, recipe, slots, i);

				Debug.Log($"Add enemy: {recipe.Nanokin}, {fighter}");
			}

			Debug.Log($"====================================");

			// Await
			// ----------------------------------------
			await playerTask;
		}

		[Serializable]
		[Inline]
		[DarkBox(true)]
		public class Entry
		{
			[HorizontalGroup("horiz", 0.175f)]
			[VerticalGroup("horiz/vert")]
			[HideLabel]
			[TableColumnWidth(48)]
			public float Weight = 1;

			[HorizontalGroup("horiz")]
			[FormerlySerializedAs("nanokins"), HideReferenceObjectPicker]
			[ListDrawerSettings(AddCopiesLastElement = true)]
			public List<SimpleNanokin> Nanokins = new List<SimpleNanokin>();

			public LootDropInfo Drops;

#if UNITY_EDITOR
			[VerticalGroup("horiz/vert")]
			[HideLabel]
			[DisplayAsString]
			[ShowInInspector]
			[NonSerialized]
			public string chance;

#endif
		}
	}

	public class RandomEncounterListAttribute : Attribute { }

#if UNITY_EDITOR
	[UsedImplicitly]
	public class RandomEncounterListAttributeDrawer : OdinAttributeDrawer<RandomEncounterListAttribute, List<RandomEncounterRecipe.Entry>>
	{
		private bool _init;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!_init)
			{
				RefreshPercentage();
				_init = true;
			}


			EditorGUI.BeginChangeCheck();
			CallNextDrawer(label);
			if (EditorGUI.EndChangeCheck())
				RefreshPercentage();

			// foreach (InspectorProperty property in Property.Children)
			// foreach (InspectorProperty p2 in property.Children)
			// {
			// p2.Draw();
			// EditorGUILayout.Space(3);
			// }
		}

		private void RefreshPercentage()
		{
			float sum = ValueEntry.SmartValue.Sum(e => e.Weight);
			foreach (RandomEncounterRecipe.Entry entry in ValueEntry.SmartValue)
				entry.chance = (entry.Weight / sum).ToString("P2");
		}
	}

	// public class RandomEncounterListEntryAttributeDrawer : OdinAttributeDrawer<RandomEncounterListAttribute, RandomEncounterRecipe.Entry>, IDefinesGenericMenuItems
	// {
	// public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
	// {
	// genericMenu.AddItem(new GUIContent("Launch"), true, () => {
	// BattleLauncherEditor.Launch();
	// Debug.Log("todo!");
	// });
	// }
	// }
#endif
}
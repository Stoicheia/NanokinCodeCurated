using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Anjin.Actors;
using Anjin.Util;
using Assets.Nanokins;
using Data.Combat;
using Data.Nanokin;
using Data.Overworld;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Addressable;

namespace SaveFiles
{

	/// <summary>
	///	A data container containing all relevant information for the player's progress and the current game state.
	/// Can either be 'runtime' (not saved as a file, only exists while the game is running), or
	/// 'savable' (saved as a file on disk that can be reloaded).
	///
	/// Savable files are indentified by the 'ID' field not being null, and being set to
	/// a valid SaveFileID value (a name or numbered index larger than 0)
	///
	/// When savable, it is serialized as a single JSON file containing the entire structure.
	///
	/// Save files are not currently obfuscated, as there is not much point currently.
	/// </summary>
	[Serializable, JsonObject(MemberSerialization.OptIn)]
	public class SaveData : ISerializationCallbackReceiver
	{



		// ====================================================================================
		//		DATA
		// ====================================================================================


		// Meta Information
		// ----------------------------------------

		/// <summary>
		/// The play time this file was last saved at.
		/// Use TimeSpan.FromSeconds to work with this number.
		/// </summary>
		[JsonProperty]
		public float LastTimeSaved;

		/// <summary>
		/// The play time as a number of seconds.
		/// Use TimeSpan.FromSeconds to work with this number.
		/// </summary>
		[JsonProperty]
		public float PlayTime;


		// NOTE(C.L.): Progression should either be derived from flags, or stored as an enum
		/// <summary>
		/// Story progression indicator string that will be shown on the savefile entry.
		/// Useful to remember which save is which and gauge roughly the progress in them.
		/// "Chapter 44 - Return of Dark Nas and Robo-Serio"
		/// </summary>
		/*[JsonProperty]
		public string Progression;*/

		// Game State
		// ----------------------------------------

		/// <summary>
		/// Arbitrary flags used for quests.
		/// </summary>
		[JsonProperty, ShowInInspector]
		public Dictionary<string, object> FlagValues = new Dictionary<string, object>();

		/// <summary>
		/// Characters in the player's party.
		/// Includes the player's character (Nas, for 99% of the game)
		/// </summary>
		[SerializeField]
		[ListDrawerSettings(CustomAddFunction = "OnAddNanokin")]
		[JsonProperty]
		public List<CharacterEntry> Party = new List<CharacterEntry>();

		/// <summary>
		/// Quests started or finished by the player.
		/// </summary>
		[JsonProperty]
		public List<QuestEntry> Quests = new List<QuestEntry>();

		/// <summary>
		/// Areas (or, worlds in the park) visited by the player.
		/// </summary>
		[JsonProperty]
		public List<Areas> AreasVisited = new List<Areas>();

		/// <summary>
		/// Subareas (levels) visited by the player.
		/// </summary>
		[JsonProperty]
		public List<LevelID> LevelsVisited = new List<LevelID>();

		/// <summary>
		/// The number of party chats the player has seen.
		/// </summary>
		[JsonProperty]
		public List<string> PartyChatsSeen = new List<string>();

		// World Info
		// ----------------------------------------

		/// <summary>
		/// What part of the game generated the save?
		/// </summary>
		[JsonProperty]
		public SaveOrigin Origin = SaveOrigin.None;

		[JsonProperty, NotNull] public PlayerLocation Location_Current		 = new PlayerLocation();
		[JsonProperty, NotNull] public PlayerLocation Location_LastSavePoint = new PlayerLocation();
		[JsonProperty, NotNull] public PlayerLocation Location_Menu          = new PlayerLocation();

		/*/// <summary>
		/// Level that the player was last seen in.
		/// </summary>
		[JsonProperty]
		public string Level;

		/// <summary>
		/// Exact position that the player was standing at.
		/// </summary>
		[JsonProperty]
		public Vector3 Position;

		/// <summary>
		/// Last level where the player used a crystal.
		/// </summary>
		[JsonProperty]
		public string CrystalLevel;

		/// <summary>
		/// Last crystal that the player used. (within CrystalLevel)
		/// </summary>
		[JsonProperty]
		public string CrystalID;*/

		/// <summary>
		/// Crystal IDs discovered/activated by the player.
		/// </summary>
		[JsonProperty]
		public HashSet<string> DiscoveredSavepoints = new HashSet<string>(); // TODO rename to DiscoveredCrystals

		// Player Inventory
		// ----------------------------------------

		/// <summary>
		/// Amount of money the player has.
		/// </summary>
		[JsonProperty]
		public int Money;

		/// <summary>
		/// Limbs in the player's inventory.
		/// </summary>
		[ListDrawerSettings(CustomAddFunction = "OnAddLimb")]
		[JsonProperty]
		public List<LimbEntry> Limbs = new List<LimbEntry>();

		/// <summary>
		/// Stickers currently in the inventory.
		/// This only includes unequipped stickers.
		/// Equipped stickers are moved to CharacterEntry.Stickers
		/// and are re-added to this list when unequipped.
		/// </summary>
		[ListDrawerSettings(CustomAddFunction = "OnAddSticker")]
		[JsonProperty]
		public List<StickerEntry> Stickers = new List<StickerEntry>();

		/// <summary>
		/// Generic items in the player's inventory.
		/// They are generally key items with no function other
		/// than to serve as a completion requirement for quests.
		/// </summary>
		[JsonProperty]
		public List<ItemEntry> Items = new List<ItemEntry>();


		// ====================================================================================
		//		LOGIC
		// ====================================================================================

		[NonSerialized, ShowInInspector] public SaveFileID? ID;
		[NonSerialized, ShowInInspector] public string      filePath;

		//public string GetID() => isNamed ? name : index.Value.ToString();

		public SaveData()
		{
			//IsFile   = false;
			filePath = "";

			ID = null;

			/*index = null;
			name  = null;*/
		}

		SaveData([NotNull] string filePath) : this()
		{
			//IsFile = true;
			this.filePath = filePath;
		}

		public SaveData(SaveFileID id, [NotNull] string path) : this(path)
		{
			ID = id;
		}

		/*public SaveData(int index, [NotNull] string path) : this(path)
		{
			this.index = index;
		}


		public SaveData(string name, [NotNull] string path) : this(path)
		{
			this.name = name;
			isNamed   = true;
		}*/

		public void Reset()
		{
			FlagValues.Clear();
			Limbs.Clear();
			Stickers.Clear();
			Party.Clear();
			Quests.Clear();
			Items.Clear();
			AreasVisited.Clear();
			LevelsVisited.Clear();
			PartyChatsSeen.Clear();
		}

		[OnSerializing]  private void OnSerializing(StreamingContext  context) => OnBeforeSerialize();
		[OnDeserialized] private void OnDeserialized(StreamingContext context) => OnAfterDeserialize();

		public void OnBeforeSerialize()
		{
			foreach (LimbEntry limb in Limbs)			limb.ApplyInstance();
			foreach (StickerEntry sticker in Stickers)	sticker.ApplyInstance();
			foreach (CharacterEntry kid in Party)		kid.ApplyInstance();
		}

		public void OnAfterDeserialize()
		{
			foreach (LimbEntry limb in Limbs)			limb.UpdateInstance(true);
			foreach (StickerEntry sticker in Stickers)	sticker.UpdateInstance(true);
			foreach (CharacterEntry monster in Party)	monster.UpdateInstance(this, true);

			if (Location_Current == null)
				Location_Current = new PlayerLocation();
		}

		// ====================================================================================
		//		TYPES
		// ====================================================================================

		[Serializable]
		public class PlayerLocation {
			public Vector3? LastStableStandingPosition	= null;
			public Vector3? FacingDirection				= null;

			public LevelID  Level                = LevelID.None;

			[CanBeNull]
			public string MostRecentSavePointID = null;
		}


		public enum SaveOrigin {
			None		= 0,	// We don't know
			Savepoint	= 1,	// The player saved at a save point
			Menu		= 2,	// Saved in the save menu
			Autosave	= 3,	//
		}

		// ====================================================================================
		//		UTILITY
		// ====================================================================================

		public LimbEntry AddLimb(string address, int level)
		{
			LimbEntry limb = new LimbEntry(address, level);
			limb.UpdateInstance();

			if (Application.isPlaying && limb.Asset == null) {
				DebugLogger.LogError($"WARNING: Limb added does not have a valid address: {address}", LogContext.Data, LogPriority.High);
			}

			Limbs.Add(limb);

			return limb;
		}

		public void AddAllMonsterLimbs(string monster, int level)
		{
			AddLimb($"Limbs/{monster}-head", level);
			AddLimb($"Limbs/{monster}-body", level);
			AddLimb($"Limbs/{monster}-arm1", level);
			AddLimb($"Limbs/{monster}-arm2", level);
		}

		public CharacterEntry AddCharacter(int level, string address, string monster)
		{
			CharacterEntry entry = AddCharacter(
				level,
				address,
				$"Limbs/{monster}-head",
				$"Limbs/{monster}-body",
				$"Limbs/{monster}-arm1",
				$"Limbs/{monster}-arm2");

			entry.MonsterName = monster;

			return entry;
		}

		/// <summary>
		/// Add a sticker to the inventory.
		/// </summary>
		/// <param name="addr"></param>
		/// <param name="charges"></param>
		public StickerEntry AddSticker(string addr, int charges = -1)
		{
			var sticker = new StickerEntry
			{
				Address = addr,
				X       = -1,
				Y       = -1,
				Charges = charges
			};

			sticker.UpdateInstance();
			Stickers.Add(sticker);

			return sticker;
		}

		public CharacterEntry AddCharacter(
			int    level,
			string address,
			string head,
			string body,
			string arm1,
			string arm2)
		{
			var kid = new CharacterEntry(address)
			{
				Level   = level,
				Points  = Pointf.One
			};

			kid.UpdateInstance(this);
			kid.Head         = FindLimb(head)?.instance;
			kid.Body         = FindLimb(body)?.instance;
			kid.Arm1         = FindLimb(arm1)?.instance;
			kid.Arm2         = FindLimb(arm2)?.instance;
			kid.UpdateInstance(this);

			if(kid.asset)
				kid.nanokin.Name = kid.asset.Name;

			Party.Add(kid);

			return kid;
		}

		/// <summary>
		/// Heal the team's hp and sp to max.
		/// </summary>
		public void HealParty()
		{
			foreach (CharacterEntry party in Party)
			{
				party.Points         = Pointf.One;
				party.nanokin.Points = Pointf.One;
			}
		}

		/// <summary>
		/// Heal the team's hp and sp to max.
		/// </summary>
		public void RevivePartyAt(float p)
		{
			foreach (CharacterEntry party in Party)
			{
				party.Points         = Pointf.One * p;
				party.nanokin.Points = Pointf.One * p;
			}
		}


		/// <summary>
		/// Refill all sticker chargers. (both in inventory and equipped)
		/// </summary>
		public void RefillStickers()
		{
			// Refill equipped stickers
			foreach (StickerEntry sticker in Stickers)
			{
				sticker.Charges          = sticker.MaxCharges;
				sticker.instance.Charges = sticker.MaxCharges;
			}

			// Refill equipped stickers
			foreach (CharacterEntry grid in Party)
			foreach (StickerEntry sticker in grid.StickerEntries)
			{
				sticker.Charges          = sticker.MaxCharges;
				sticker.instance.Charges = sticker.MaxCharges;
			}
		}

		[CanBeNull]
		public LimbEntry GetLimb(string guid)
		{
			foreach (LimbEntry l in Limbs)
				if (l.GUID == guid)
					return l;
			return null;
		}

		public bool HasLimb(string addr) => FindLimb(addr) != null;

		public bool HasItem(string search, int quantity = 1)
		{
			int found = 0;
			for (var i = 0; i < Items.Count; i++)
			{
				ItemEntry item = Items[i];
				if (item.Address == search)
				{
					found++;
					if (found == quantity) return true;
				}
			}

			for (var i = 0; i < Items.Count; i++)
			{
				ItemEntry item = Items[i];
				if (item.Tags != null && item.Tags.Contains(search))
				{
					found++;
					if (found == quantity) return true;
				}
			}

			return false;
		}

		public bool LoseItem(string address, int quantity = 1)
		{
			var lost = 0;
			for (int i = Items.Count - 1; i >= 0; i--)
			{
				ItemEntry item = Items[i];
				if (item.Address == address)
				{
					Items.RemoveAt(i);

					lost++;
					if (lost == quantity)
						return true;
				}
			}

			return false;
		}


		[CanBeNull]
		public LimbEntry FindLimb(string addr)
		{
			foreach (LimbEntry x in Limbs)
				if (x.Address == addr)
					return x;

			return null;
		}

		private void AddNJS(int level = 1)
		{
			CharacterEntry nas   = AddCharacter(level, "Characters/nas", "beak-brigade");
			CharacterEntry jatz  = AddCharacter(level, "Characters/jatz", "hamrin-head");
			CharacterEntry serio = AddCharacter(level, "Characters/serio", "jellywup");

			serio.FormationCoord = new Vector2Int(1, 0);
			nas.FormationCoord   = new Vector2Int(1, 1);
			jatz.FormationCoord  = new Vector2Int(1, 2);
		}

		private void AddStarterLimbs(int rank = 1)
		{
			AddLimb("Limbs/beak-brigade-head", rank);
			AddLimb("Limbs/beak-brigade-body", rank);
			AddLimb("Limbs/beak-brigade-arm1", rank);
			AddLimb("Limbs/beak-brigade-arm2", rank);

			// Note: in the final game, only start with beak-brigade and nas
			AddLimb("Limbs/jellywup-head", rank);
			AddLimb("Limbs/jellywup-body", rank);
			AddLimb("Limbs/jellywup-arm1", rank);
			AddLimb("Limbs/jellywup-arm2", rank);

			AddLimb("Limbs/hamrin-head-head", rank);
			AddLimb("Limbs/hamrin-head-body", rank);
			AddLimb("Limbs/hamrin-head-arm1", rank);
			AddLimb("Limbs/hamrin-head-arm2", rank);
		}

		/// <summary>
		/// Sets the base data for a new save created through normal means.
		/// </summary>
		/// <param name="save"></param>
		/// <returns></returns>
		public void SetBaseData()
		{
			AddStarterLimbs();
			AddNJS(3);
		}

		/// <summary>
		/// Sets up combat related values for Ch0 Freeport.
		/// </summary>
		public void SetupPrologueFreeportCombat()
		{
			//AddLimb("Limbs/pangzoran-head", 2);
			//AddLimb("Limbs/pangzoran-body", 2);
			//AddLimb("Limbs/pangzoran-arm1", 2);
			//AddLimb("Limbs/pangzoran-arm2", 2);
			//AddLimb("Limbs/bigfoot-head", 2);
			//AddLimb("Limbs/bigfoot-body", 2);
			//AddLimb("Limbs/bigfoot-arm1", 2);
			//AddLimb("Limbs/bigfoot-arm2", 2);
			//AddLimb("Limbs/jellyfrank-head", 2);
			//AddLimb("Limbs/jellyfrank-body", 2);
			//AddLimb("Limbs/jellyfrank-arm1", 2);
			//AddLimb("Limbs/jellyfrank-arm2", 2);
			//AddLimb("Limbs/g-lanza-head", 2);
			//AddLimb("Limbs/g-lanza-body", 2);
			//AddLimb("Limbs/g-lanza-arm1", 2);
			//AddLimb("Limbs/g-lanza-arm2", 2);

			AddLimb("Limbs/beak-brigade-head", 2);
			AddLimb("Limbs/beak-brigade-body", 2);
			AddLimb("Limbs/beak-brigade-arm1", 2);
			AddLimb("Limbs/beak-brigade-arm2", 2);

			AddLimb("Limbs/jellywup-head", 2);
			AddLimb("Limbs/jellywup-body", 2);
			AddLimb("Limbs/jellywup-arm1", 2);
			AddLimb("Limbs/jellywup-arm2", 2);

			AddLimb("Limbs/hamrin-head-head", 2);
			AddLimb("Limbs/hamrin-head-body", 2);
			AddLimb("Limbs/hamrin-head-arm1", 2);
			AddLimb("Limbs/hamrin-head-arm2", 2);

			AddSticker("Stickers/sr-blast", 3);
			AddSticker("Stickers/sr-blast", 3);
			AddSticker("Stickers/sr-blast", 3);
			AddSticker("Stickers/podunk",   2);

			//CharacterEntry nas = AddCharacter(4, "Characters/nas", "bigfoot");
			//CharacterEntry nas = AddCharacter(4, "Characters/nas", "pangzoran");
			//CharacterEntry nas = AddCharacter(4, "Characters/nas", "jellyfrank");
			//CharacterEntry nas = AddCharacter(4, "Characters/nas", "g-lanza");

			CharacterEntry nas   = AddCharacter(4, "Characters/nas",   "beak-brigade");
			CharacterEntry jatz  = AddCharacter(4, "Characters/jatz",  "hamrin-head");
			CharacterEntry serio = AddCharacter(4, "Characters/serio", "jellywup");

			serio.FormationCoord = new Vector2Int(1, 0);
			nas.FormationCoord   = new Vector2Int(1, 1);
			jatz.FormationCoord  = new Vector2Int(1, 2);
		}

		/// <summary>
		/// Sets the base data for a new save created through normal means.
		/// </summary>
		/// <param name="save"></param>
		/// <returns></returns>
		public void SetupPrologueFreeport()
		{
			SetupPrologueFreeportCombat();

			Money = 3000;
			Location_Current = new PlayerLocation {
				Level = LevelID.Freeport,
			};

			FlagValues.Clear();

			FlagValues["publisher_build"] = true;
			FlagValues["fp_roc_active"]   = true;
		}

		/// <summary>
		/// Max out the save with as much of the game's unlockable.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public void SetMaxedData()
		{
			// Gain all limbs
			// ----------------------------------------
			Limbs.Clear();

			List<string> limbs = Addressables2.Find("Limbs/", notEndWith: ".spritesheet");
			foreach (string address in limbs)
			{
				AddLimb(address, StatConstants.MAX_MASTERY);
			}

			// Gain all party members
			// ----------------------------------------
			Party.Clear();

			AddNJS(StatConstants.MAX_LEVEL);

			FlagValues[PlayerActor.GFLAG_POGO]  = true;
			FlagValues[PlayerActor.GFLAG_GLIDE] = true;
			FlagValues[PlayerActor.GFLAG_SLASH] = true;

			Money = 1000000;
		}

		/// <summary>
		/// Ensure that the save file will result in a smooth gameplay experience no matter what.
		/// Anything is better than a game crash or errors that would require a full restart.
		/// This function is unlikely to be too useful in the final game unless something goes very wrong,
		/// however as a development feature it is very valuable.
		/// </summary>
		/// <param name="save"></param>
		/// <returns></returns>
		public void EnsurePlayable()
		{
			// Sanitize existing data
			// ----------------------------------------
			for (var i = 0; i < Limbs.Count; i++)
			{
				LimbEntry limb = Limbs[i];
				if (!GameAssets.HasLimb(limb.Address))
				{
					this.Log($"Removing invalid limb {limb.Address} from data.");
					Limbs.RemoveAt(i--);
				}
			}

			for (var i = 0; i < Party.Count; i++)
			{
				CharacterEntry entry = Party[i];
				if (!GameAssets.HasCharacter(entry.Address))
				{
					this.Log($"Removing invalid character {entry.Address} from data.");
					Party.RemoveAt(i--);
				}
			}

			for (var i = 0; i < Items.Count; i++)
			{
				ItemEntry item = Items[i];
				if (!GameAssets.HasItem(item.Address))
				{
					this.Log($"Removing invalid item {item.Address} from data.");
					Limbs.RemoveAt(i--);
				}
			}

			// Add missing data required for gameplay
			// ----------------------------------------

			if (Limbs.Count <= 0)
			{
				AddStarterLimbs();
			}
			else
			{
				EnsureLimbKind(LimbType.Head, "Limbs/beak-brigade-head");
				EnsureLimbKind(LimbType.Body, "Limbs/beak-brigade-body");
				EnsureLimbKind(LimbType.Arm1, "Limbs/beak-brigade-arm1");
				EnsureLimbKind(LimbType.Arm2, "Limbs/beak-brigade-arm2");
			}

			if (Party.Count == 0)
			{
				// Make sure we have at least one party member
				AddNJS();
			}

			foreach (CharacterEntry nano in Party)
			{
				if (string.IsNullOrEmpty(nano.MonsterName))
					nano.MonsterName = new[] {"oof", "Nanokin Man", "Kyle"}.Choose();

				try
				{
					foreach (LimbEntry currentLimb in Limbs)
					{
						// IDEA replace invalid limbs (bad GUID) with a default from the inventory (with the corresponding kind)
						if (nano.Head == null && currentLimb.Kind == Data.Nanokin.LimbType.Head) nano.Head = currentLimb.instance;
						if (nano.Body == null && currentLimb.Kind == Data.Nanokin.LimbType.Body) nano.Body = currentLimb.instance;
						if (nano.Arm1 == null && currentLimb.Kind == Data.Nanokin.LimbType.Arm1) nano.Arm1 = currentLimb.instance;
						if (nano.Arm2 == null && currentLimb.Kind == Data.Nanokin.LimbType.Arm2) nano.Arm2 = currentLimb.instance;

						if (nano.Body != null && nano.Head != null && nano.Arm1 != null && nano.Arm2 != null)
							break;
					}
				}
				catch (Exception e)
				{
					DebugLogger.LogException(e);
					throw;
				}
			}
		}

		/// <summary>
		/// Ensure that the sava data contains a limb of the specified kind.
		/// Otherwise, adds the fallback limb.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fallback"></param>
		private void EnsureLimbKind(LimbType type, string fallback)
		{
			var has = false;
			foreach (LimbEntry limb in Limbs)
			{
				if (limb.Asset.Kind == type)
				{
					has = true;
					break;
				}
			}

			if (!has)
			{
				this.Log($"SaveManager: awarding a {fallback} limb to compensate for lack of a {type} in the inventory.");
				AddLimb(fallback, 1);
			}
		}

#if UNITY_EDITOR
		[UsedImplicitly]
		private void OnAddLimb()
		{
			LimbEntry entry = new LimbEntry(); // LimbItem
			Limbs.Add(entry);
		}

		[UsedImplicitly]
		private void OnAddNanokin()
		{
			CharacterEntry item = new CharacterEntry(null);
			Party.Add(item);
		}

		[UsedImplicitly]
		private void OnAddSticker()
		{
			Stickers.Add(new StickerEntry
			{
				Address = "",
				Charges = 0,
				X       = -1,
				Y       = -1
			});
		}
#endif
	}

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anjin.Scripting;
using API.Spritesheet.Indexing;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Random = UnityEngine.Random;

namespace Data.Shops
{
	/// <summary>
	/// A loot that can be awarded or sold to the player
	/// </summary>
	[Serializable]
	[LuaUserdata(Descendants = true)]
	public class LootEntry
	{
		/// <summary>
		/// Type of the loot.
		/// </summary>
		public LootType Type = LootType.Item;

		/// <summary>
		/// Address of the loot.
		/// </summary>
		[AddressFilter(null, null, ".spritesheet")]
		[ShowIf("@ShowAddress()")]
		public string Address;

		/// <summary>
		/// If this is a limb, the mastery level of the limb.
		/// </summary>
		[LabelText("Mastery rank of the limb.")]
		[MinValue(1)]
		[MaxValue(StatConstants.MAX_MASTERY)]
		[ShowIf("@Type == LootType.Limb")]
		public int Mastery = 1;

		/// <summary>
		/// Quantity of instances that this loot represents.
		/// In a shop, this is a finite quantity that is available for purchase.
		/// -1 is an infinite quantity.
		/// </summary>
		[LabelText(@"
		Quantity of instances that this loot represents.
		In a shop, this is a finite quantity that is available for purchase.
		-1 is an infinite quantity. ")]
		[GUIColor("GetQuantityColor")]
		[MinValue(-1)]
		public int Quantity = -1;

		/// <summary>
		/// Price of the loot in a shop.
		/// Leave to -1 to use the default price.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("_price")]
		[GUIColor("GetPriceColor")]
		[MinValue(-1)]
		public int Price = -1;

		/// <summary>
		/// Arbitrary tags to track things, mostly used for items.
		/// </summary>
		[CanBeNull]
		public List<string> Tags;

		/// <summary>
		/// Arbitrary index to track this loot at runtime.
		/// Not serialized with the loot.
		/// </summary>
		[NonSerialized]
		public int index;

		public LootEntry() { }

		public LootEntry(LootType type, string address)
		{
			Type    = type;
			Address = address;
		}

		public static LootEntry new_money(int amount)
		{
			LootEntry entry = new LootEntry();
			entry.Type = LootType.Money;
			entry.Quantity = amount;
			return entry;
		}

		[LuaGlobalFunc]
		public static LootEntry new_sticker(string address, DynValue dynval)
		{
			LootEntry entry = new LootEntry(LootType.Sticker, address);
			if (dynval.AsTable(out Table tbl))
			{
				entry.ReadConf(tbl);
			}
			else if (dynval.AsInt(out int price))
			{
				entry.Price = price;
			}

			return entry;
		}

		[LuaGlobalFunc]
		public static LootEntry new_limb(string address, DynValue dynval)
		{
			LootEntry entry = new LootEntry(LootType.Limb, address);
			if (dynval.AsTable(out Table tbl))
			{
				entry.ReadConf(tbl);
			}
			else if (dynval.AsInt(out int price))
			{
				entry.Price = price;
			}

			return entry;
		}


		[LuaGlobalFunc]
		public static LootEntry new_item(string address, DynValue dynval)
		{
			LootEntry entry = new LootEntry(LootType.Item, address);
			if (dynval.AsTable(out Table tbl))
			{
				entry.ReadConf(tbl);
			}
			else if (dynval.AsInt(out int price))
			{
				entry.Price = price;
			}

			return entry;
		}

		[LuaGlobalFunc]
		public static LootEntry new_loot(DynValue dynval)
		{
			var entry = new LootEntry();
			if (dynval.AsTable(out Table tbl))
			{
				entry.ReadConf(tbl);
			}

			return entry;
		}

		public void ReadConf(Table tbl)
		{
			if (tbl.TryGet("item", out Address, Address)) Type         = LootType.Item;
			else if (tbl.TryGet("sticker", out Address, Address)) Type = LootType.Sticker;
			else if (tbl.TryGet("limb", out Address, Address)) Type    = LootType.Limb;

			tbl.TryGet("quantity", out Quantity, -1);
			tbl.TryGet("price", out Price, -1);
			tbl.TryGet("level", out Mastery, 1);

			if (tbl.TryGet("tag", out string tag))
			{
				Tags = Tags ?? new List<string>();
				Tags.Add(tag);
			}
			else if (tbl.TryGet("tags", out List<string> tags))
			{
				if (Tags == null)
					Tags = tags;
				else
					Tags.AddRange(tags);
			}
		}

		public bool IsValid()
		{
			switch (Type)
			{
				case LootType.Money:   return true;
				case LootType.Item:    return GameAssets.HasItem(Address);
				case LootType.Limb:    return GameAssets.HasLimb(Address);
				case LootType.Sticker: return GameAssets.HasSticker(Address);
				default:               return false;
			}
		}

		/// <summary>
		/// Get the
		/// </summary>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public int GetPrice()
		{
			if (Price > -1)
				return Price;

			switch (Type)
			{
				case LootType.Money:
					return 0;

				case LootType.Item:
					return GameAssets.GetItem(Address).DefaultPrice;

				case LootType.Limb:
					return 0; // No default price, if it changes throughout the game it's better to set the price in the individual shop scripts.

				case LootType.Sticker:
					return GameAssets.GetSticker(Address).Price;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Get the name of the loot for displaying.
		/// </summary>
		public string GetName()
		{
			switch (Type)
			{
				case LootType.Money:
					return $"{Quantity} Credits";
				case LootType.Item:
					return GameAssets.GetItem(Address)?.ItemName;
				case LootType.Limb:
					return GameAssets.GetLimb(Address)?.DisplayName;
				case LootType.Sticker:
					return GameAssets.GetSticker(Address)?.DisplayName;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return string.Empty;
		}

		/// <summary>
		/// Get the description of the loot for displaying.
		/// </summary>
		public string GetDescription()
		{
			if (!IsValid()) {
				return $"Invalid LootEntry: {Address}";
			}

			switch (Type)
			{
				case LootType.Money:   return string.Empty;
				case LootType.Item:    return GameAssets.GetItem(Address).Description;
				case LootType.Limb:    return "A limb for a Nanokin!";
				case LootType.Sticker: return GameAssets.GetSticker(Address).ShopDisplay;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return string.Empty;
		}

		/// <summary>
		/// Get the quantity of this loot present in the player's inventory. (SaveManager.current)
		/// </summary>
		public int GetOwnedCount()
		{
			switch (Type)
			{
				case LootType.Money:
					break;

				case LootType.Item:
				{
					var count = 0;

					for (var i = 0; i < SaveManager.current.Items.Count; i++)
					{
						ItemEntry entry = SaveManager.current.Items[i];
						if (entry.Address == Address) count++;
					}

					return count;
				}

				case LootType.Limb:
				{
					var count = 0;
					for (var i = 0; i < SaveManager.current.Limbs.Count; i++)
					{
						LimbEntry limb = SaveManager.current.Limbs[i];
						if (limb.Address == Address) count++;
					}

					return count;
				}

				case LootType.Sticker:
				{
					var count = 0;
					for (var i = 0; i < SaveManager.current.Stickers.Count; i++)
					{
						StickerEntry sticker = SaveManager.current.Stickers[i];
						if (sticker.Address == Address) count++;
					}

					return count;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}

			return 0;
		}

		/// <summary>
		/// Get the maximum that the player can hold of this loot.
		/// </summary>
		public virtual int GetMaxAllowed()
		{
			switch (Type)
			{
				case LootType.Money:
					break;

				case LootType.Item:
					return GameAssets.GetItem(Address).MaxQuantity;
					break;

				case LootType.Limb:
					return 1;

				case LootType.Sticker:
					return 9;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return 0;
		}

		/// <summary>
		/// Award all instances that this loot represents to the player. (As represented by Quantity)
		/// <returns>Whether or not everything awarded with success.</returns>
		/// </summary>
		public bool AwardAll()
		{
			var result = true;
			while (Quantity > 0 && result)
			{
				result = AwardOne();
			}

			return result;
		}

		/// <summary>
		/// Award one instance of this loot to the player. (Decrements Quantity by 1)
		/// <returns>Whether or not it was awarded with success.</returns>
		/// </summary>
		public bool AwardOne()
		{
			if (Quantity == 0) return false;

			switch (Type)
			{
				case LootType.Money:
					break;

				case LootType.Item:
					SaveManager.current.Items.Add(new ItemEntry
					{
						Address = Address,
						Tags    = Tags
					});
					break;

				case LootType.Limb:
					SaveData inventory = SaveManager.current;
					if (inventory.HasLimb(Address))
						return false;

					inventory.AddLimb(Address, Mastery);
					break;

				case LootType.Sticker:
					StickerAsset sticker = GameAssets.GetSticker(Address);
					SaveManager.current.AddSticker(Address, sticker.Charges);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			Quantity--;
			return true;
		}

		public (LootDisplayMethod displayMethod, string displayAddress, int spriteIndex) GetDisplay()
		{
			(LootDisplayMethod Prefab, string, int) ret = (LootDisplayMethod.None, "", -1);

			switch (Type)
			{
				case LootType.Money:
					ret = (LootDisplayMethod.None, null, -1);
					break;

				case LootType.Item:
					ItemAsset itemAsset = GameAssets.GetItem(Address);
					if (itemAsset!= null && itemAsset.DisplayPrefab != null)
						//ret = (LootDisplayMethod.Prefab, (string) itemAsset.DisplayPrefab.RuntimeKey, -1);
						ret = (LootDisplayMethod.Sprite, (string)itemAsset.DisplayIcon.RuntimeKey, -1);
					break;

					ret = (LootDisplayMethod.Sprite, (string) itemAsset.DisplayIcon.RuntimeKey, -1);
					break;

				case LootType.Limb:
					ret = (LootDisplayMethod.Spritesheet, GameAssets.GetLimb(Address).SpritesheetAddress, 0);
					break;

				case LootType.Sticker:
					ret = (LootDisplayMethod.Sprite, (string) GameAssets.GetSticker(Address).Sprite.RuntimeKey, -1);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (string.IsNullOrEmpty(ret.Item2))
			{
				ret.Item1 = LootDisplayMethod.None;
			}

			return ret;
		}

		public async UniTask<LootDisplayHandle> LoadDisplay()
		{
			(LootDisplayMethod displayMethod, string displayAddress, int spriteIndex) = GetDisplay();

			switch (displayMethod)
			{
				case LootDisplayMethod.None:
					break;

				case LootDisplayMethod.Sprite:
					return new LootDisplayHandle(await Addressables2.LoadHandleAsync<Sprite>(displayAddress));

				case LootDisplayMethod.Spritesheet:
					return new LootDisplayHandle(await Addressables2.LoadHandleAsync<IndexedSpritesheetAsset>(displayAddress), spriteIndex);

				case LootDisplayMethod.Prefab:
					return new LootDisplayHandle(await Addressables2.LoadHandleAsync<GameObject>(displayAddress));
			}

			return new LootDisplayHandle {method = LootDisplayMethod.None};
		}

		public override string ToString()
		{
			return $"LootEntry({Address})";
		}


#if UNITY_EDITOR
		[UsedImplicitly]
		public Color GetPriceColor() => Price == -1 ? Color.gray : Color.white;

		[UsedImplicitly]
		public Color GetQuantityColor() => Quantity == -1 ? Color.gray : Color.white;

		public bool ShowAddress() => Type != LootType.Money;

		public string AddressPrefix()
		{
			switch (Type) {
				case LootType.Money:   break;
				case LootType.Item:    return "Items/";
				case LootType.Limb:    return "Limbs/";
				case LootType.Sticker: return "Stickers/";
			}
			return "";
		}
#endif
	}

	public enum LootDisplayMethod
	{
		None,
		Sprite,
		Spritesheet,
		Prefab,
	}

	public struct LootDisplayHandle
	{
		public LootDisplayMethod                             method;
		public Sprite                                        sprite;
		public GameObject                                    prefab;
		public AsyncOperationHandle<Sprite>                  hndSprite;
		public AsyncOperationHandle<IndexedSpritesheetAsset> hndSpritesheet;
		public AsyncOperationHandle<GameObject>              hndPrefab;

		public LootDisplayHandle(AsyncOperationHandle<Sprite> handle)
		{
			method         = LootDisplayMethod.Sprite;
			sprite         = handle.Result;
			prefab         = null;
			hndSprite      = handle;
			hndSpritesheet = new AsyncOperationHandle<IndexedSpritesheetAsset>();
			hndPrefab      = new AsyncOperationHandle<GameObject>();
		}

		public LootDisplayHandle(AsyncOperationHandle<GameObject> handle)
		{
			method         = LootDisplayMethod.Prefab;
			sprite         = null;
			prefab         = handle.Result;
			hndSprite      = new AsyncOperationHandle<Sprite>();
			hndSpritesheet = new AsyncOperationHandle<IndexedSpritesheetAsset>();
			hndPrefab      = handle;
		}

		public LootDisplayHandle(AsyncOperationHandle<IndexedSpritesheetAsset> handle, int spriteIndex)
		{
			method         = LootDisplayMethod.Sprite;
			sprite         = handle.Result.spritesheet.Spritesheet[spriteIndex].Sprite;
			prefab         = null;
			hndSprite      = new AsyncOperationHandle<Sprite>();
			hndSpritesheet = handle;
			hndPrefab      = new AsyncOperationHandle<GameObject>();
		}

		public void ReleaseSafe()
		{
			method = LootDisplayMethod.None;
			sprite = null;
			prefab = null;
			Addressables2.ReleaseSafe(hndPrefab);
			Addressables2.ReleaseSafe(hndSprite);
			Addressables2.ReleaseSafe(hndSpritesheet);
		}
	}

	[Serializable]
	public struct LootDropInfo
	{
		[Serializable]
		public struct LootWeightPair
		{
			public LootEntry loot;
			[Range(0, 99)] public float weight;

			public LootWeightPair(LootEntry le, float w)
			{
				loot = le;
				weight = w;
			}
		}

		[Serializable]
		public struct TableInfo
		{
			public string tableId;
			public List<LootWeightPair> table;
			[Range01] public float probability;

			public TableInfo(List<LootWeightPair> lwps, float p, string id)
			{
				table = lwps;
				probability = p;
				tableId = id;
			}
		}

		[Tooltip("Inner list entries are weights, and choices are mutually exclusive. Outer list entries are probabilities.")]
		[SerializeField] public List<TableInfo> DropTables;

		public int Money;
		public int MaximumAllowedLoots;

		public List<LootEntry> GiveLoots()
		{
			const float MONEY_VARIANCE = 0.05f;
			if (MaximumAllowedLoots <= 0) MaximumAllowedLoots = int.MaxValue;
			var tables = DropTables;
			List<LootEntry> givenLoots = new List<LootEntry>();

			List<List<(LootEntry, float)>> givenTables = new List<List<(LootEntry, float)>>();

			foreach (var table in tables)
			{
				if (Random.value < table.probability)
				{
					List<(LootEntry, float)> thisTable = table.table.Select(x => (x.loot, x.weight)).ToList();
					givenTables.Add(thisTable);
				}
			}

			foreach (var table in givenTables)
			{
				if (table == null || table.Count == 0) continue;
				LootEntry thisLoot = WeightedRandom<LootEntry>.Choose(table);
				if(thisLoot.Type != LootType.None)
					givenLoots.Add(thisLoot);
				if (givenLoots.Count >= MaximumAllowedLoots) break;
			}
			if(Money > 0)
				givenLoots.Add(LootEntry.new_money((int)(Money * (1 + MONEY_VARIANCE))));
			return givenLoots;
		}

		public LootDropInfo(int maxLoots)
		{
			DropTables = new List<TableInfo>();
			List<LootWeightPair> emptyTable = new List<LootWeightPair>();
			DropTables.Add(new TableInfo(emptyTable, 1, "default"));
			MaximumAllowedLoots = maxLoots;
			Money = 0;
		}

		public static LootDropInfo Empty()
		{
			return new LootDropInfo(100);
		}



		public void Init()
		{
			DropTables = new List<TableInfo>();
		}
		public LootDropInfo CombineDropTables(LootDropInfo other)
		{
			if (DropTables == null || other.DropTables == null) return this;
			foreach (string id in other.DropTables.Select(x => x.tableId))
			{
				var otherTable = other.DropTables.Find(x => x.tableId == id);
				if (!DropTables.Contains(x => x.tableId == id))
				{
					List<LootWeightPair> emptyTable = new List<LootWeightPair>();
					DropTables.Add(new TableInfo(emptyTable, 0, id));
				}

				var myTable = DropTables.Find(x => x.tableId == id);

				float newProbability = 1 - (1 - myTable.probability) * (1 - otherTable.probability);

				myTable.table = myTable.table.AppendWith(otherTable.table).ToList();
				myTable.probability = newProbability;
			}

			return this;
		}
	}
}
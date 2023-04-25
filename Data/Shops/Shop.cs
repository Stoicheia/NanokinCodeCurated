using System;
using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Data.Shops
{
	[Serializable]
	public class Shop
	{
		public string Title;

		[ListDrawerSettings(HideAddButton = true),
		 Inline,
		 LabelText("Shop Products"),
		 PropertyOrder(2),
		 DarkBox]
		public List<LootEntry> Products = new List<LootEntry>();

		[NonSerialized] public int          boughtCount;
		[NonSerialized] public List<int>    boughtIndices;
		[NonSerialized] public List<string> boughtAddresses;
		[NonSerialized] public List<string> boughtTags;

		[LuaGlobalFunc, NotNull]
		public static Shop new_shop(string title)
		{
			return new Shop();
		}

		[LuaGlobalFunc, NotNull]
		public static Shop new_shop(Table tbl)
		{
			string title = tbl.Get(1).AsString("Untitled Shop"); 
			//string title = tbl.TryGet<string>("title", "Untitled Shop");

			var shop = new Shop
			{
				Title = title,
			};

			for (var i = 2; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);
				if (dv.AsUserdata(out LootEntry entry))
				{
					shop.Products.Add(entry);
					;
				}
			}

			return shop;
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class ShopProxy : LuaProxy<Shop>
		{
			public LootEntry add(DynValue tbl)
			{
				var loot = LootEntry.new_loot(tbl);
				proxy.Products.Add(loot);
				return loot;
			}

			[NotNull]
			public LootEntry sticker(string address)
			{
				var loot = new LootEntry();
				proxy.Products.Add(loot);
				return loot;
			}

			[NotNull]
			public LootEntry item(string address)
			{
				var loot = new LootEntry();
				proxy.Products.Add(loot);
				return loot;
			}

			[NotNull]
			public LootEntry limb(string address)
			{
				var loot = new LootEntry();
				proxy.Products.Add(loot);
				return loot;
			}

			public bool bought(string address)
			{
				return proxy.boughtAddresses.Contains(address) || proxy.boughtTags.Contains(address);
			}

			public bool bought_nothing()
			{
				return proxy.boughtCount == 0;
			}

			public bool bought_something()
			{
				return proxy.boughtCount >= 1;
			}
		}
	}
}
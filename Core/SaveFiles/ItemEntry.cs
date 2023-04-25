using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SaveFiles.Elements.Inventory.Items;

namespace SaveFiles
{
	[Serializable, JsonObject(MemberSerialization.OptIn)]
	public class ItemEntry
	{
		[JsonProperty]
		public string Address;

		[JsonProperty, CanBeNull]
		public List<string> Tags;

		[CanBeNull]
		public ItemAsset Asset => GameAssets.GetItem(Address);

		public bool HasTag(string tag)
		{
			if (Tags == null)
				return false;

			for (var i = 0; i < Tags.Count; i++)
			{
				string t = Tags[i];
				if (t == tag) return true;
			}

			return false;
		}
	}
}
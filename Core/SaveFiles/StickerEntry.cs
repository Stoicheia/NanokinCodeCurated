using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using Util.Addressable;

namespace SaveFiles
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class StickerEntry
	{
		[JsonProperty]
		[ValueDropdown("StickersDropdown", NumberOfItemsBeforeEnablingSearch = 0, SortDropdownItems = true)]
		public string Address;

		[JsonProperty]
		public int Charges;

		[JsonProperty]
		public int X;

		[JsonProperty]
		public int Y;

		[JsonProperty]
		public int Rotation; // Integer from 0 to 3

		[JsonProperty]
		public bool Favorited;

		[NonSerialized, ShowInInspector]
		public StickerInstance instance;

		public StickerAsset Asset => GameAssets.GetSticker(Address);

		public int MaxCharges => instance.MaxCharges;

		public void UpdateInstance(bool isDeserializing = false)
		{
			instance = instance ?? new StickerInstance();

			instance.Entry = this;
			instance.Charges = Charges;
			instance.Asset = GameAssets.GetSticker(Address);
			instance.Favorited = Favorited;
		}

		public void ApplyInstance()
		{
			if (instance != null && instance.Asset != null)
			{
				Charges = instance.Charges;
				Address = instance.Asset.Address;
				Favorited = instance.Favorited;
			}
		}

#if UNITY_EDITOR
		[UsedImplicitly]
		private ValueDropdownList<string> StickersDropdown()
		{
			var ret = new ValueDropdownList<string>();

			foreach (string address in Addressables2.FindInEditor("Stickers/"))
			{
				ret.Add(address);
			}

			return ret;
		}
#endif
	}
}
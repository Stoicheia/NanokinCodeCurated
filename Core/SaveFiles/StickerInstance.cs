using System;
using JetBrains.Annotations;
using SaveFiles.Elements.Inventory.Items;

namespace SaveFiles
{
	public class StickerInstance
	{
		/// <summary>
		/// Charges of the sticker.
		/// </summary>
		public int Charges;

		/// <summary>
		/// The sticker in question.
		/// </summary>
		public StickerAsset Asset;

		/// <summary>
		/// Save entry that this sticker instance is linked to.
		/// </summary>
		[CanBeNull]
		public StickerEntry Entry;

		public bool Favorited;

		public bool IsEquipment => Asset.IsEquipment;
		public bool IsConsumable => Asset.IsConsumable;

		public int MaxCharges => Asset.Charges;
	}
}
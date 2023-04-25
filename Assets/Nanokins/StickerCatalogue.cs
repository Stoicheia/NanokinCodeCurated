using SaveFiles.Elements.Inventory.Items;
using Util.Assets;

namespace Assets.Nanokins
{
	public class StickerCatalogue : AssetCatalogue<StickerAsset, StickerCatalogue>
	{
		public override string AddressPrefix    => "Sticker/";
		public override string AddressExcludes  => ".png";
		public override string AddressableLabel => "Stickers";

		protected override void OnAssetLoaded(StickerAsset asset)
		{

		}
	}
}
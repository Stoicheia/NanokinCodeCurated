using System;
using API.Spritesheet.Indexing;
using UnityEngine.AddressableAssets;

namespace Util.Addressable
{
	[Serializable]
	public class AssetReferenceIndexedSpritesheet : AssetReferenceT<IndexedSpritesheetAsset>
	{
		public AssetReferenceIndexedSpritesheet(string guid) : base(guid) { }
	}
}
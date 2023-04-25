using System;
using Assets.Nanokins;
using UnityEngine.AddressableAssets;

namespace Util.Addressable
{
	[Serializable]
	public class AssetReferenceNanokinLimb : AssetReferenceT<NanokinLimbAsset>
	{
		public AssetReferenceNanokinLimb(string guid) : base(guid) { }
	}
}
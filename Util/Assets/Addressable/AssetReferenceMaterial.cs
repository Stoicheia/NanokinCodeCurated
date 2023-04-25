using System;
using Assets.Nanokins;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Util.Addressable
{
	[Serializable]
	public class AssetReferenceMaterial : AssetReferenceT<Material>
	{
		public AssetReferenceMaterial(string guid) : base(guid) { }
	}

	[Serializable]
	public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
	{
		public AssetReferenceAudioClip(string guid) : base(guid) { }
	}

	[Serializable]
	public class AssetReferenceScreechSoundSet : AssetReferenceT<ScreechSoundSet>
	{
		public AssetReferenceScreechSoundSet(string guid) : base(guid) { }
	}
}
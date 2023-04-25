using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Addressable;

namespace Combat.Entry
{
	public class CharacterAsset : ScriptableObject, IAddressable
	{
		public string                   Name;
		public AssetReferenceGameObject ActorPrefab;
		public AssetReferenceGameObject BattlePrefab;
		public AssetReferenceSprite     Art;
		public AssetReferenceSprite     StatBarPortrait;
		public AssetReferenceGameObject Bust;
		public AssetReferenceMaterial   XPBarMaterial;

		public AnimationCurve XPCurve;

		public string Address { get; set; }

		public Vector3 SpliceMenuPosition;
		public Vector3 SpliceMenuScale;

		public Vector3 StickerMenuPosition;
		public Vector3 StickerMenuScale;
	}
}
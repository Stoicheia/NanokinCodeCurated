using System;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Util.Addressable;

namespace SaveFiles.Elements.Inventory.Items
{
	[Serializable, LuaUserdata]
	public class ItemAsset : SerializedScriptableObject, IAddressable
	{
		[SerializeField, FormerlySerializedAs("itemName")]
		public string ItemName;

		[SerializeField, FormerlySerializedAs("description")]
		public string Description;

		[SerializeField, FormerlySerializedAs("_defaultPrice")]
		public int DefaultPrice;

		public int MaxQuantity;

		[SerializeField]
		public AssetReferenceSprite DisplayIcon;

		[FormerlySerializedAs("displayPrefab")]
		public AssetReferenceGameObject DisplayPrefab;


		public override string ToString() => $"ItemAsset({ItemName})";

		public string Address { get; set; }
	}
}
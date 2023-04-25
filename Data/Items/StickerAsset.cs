using System;
using Anjin.Scripting;
using Combat.Scripting;
using Data.Combat;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;
#if UNITY_EDITOR
using Combat;
using UnityEditor;

#endif

namespace SaveFiles.Elements.Inventory.Items
{
	public class StickerAsset : SerializedScriptableObject, IAddressable, ILuaObject
	{
		[FormerlySerializedAs("displayName"), FormerlySerializedAs("name"), FormerlySerializedAs("stickerName"), Title("Info")]
		public string DisplayName;

		[FormerlySerializedAs("shopDisplay"), Multiline(5)]
		public string ShopDisplay;

		[FormerlySerializedAs("description"), Multiline(5)]
		public string Description;

		[FormerlySerializedAs("price")]
		public int Price;

		public AssetReferenceSprite Sprite;

		[FormerlySerializedAs("_columns"), Title("Shape")]
		[OnValueChanged("OnShapeResized")]
		[SerializeField]
		public int Columns;

		[FormerlySerializedAs("_rows"), OnValueChanged("OnShapeResized")]
		[SerializeField]
		public int Rows;

		[TableMatrix(DrawElementMethod = "DrawShapeCell", ResizableColumns = false)]
		[TableColumnWidth(32)]
		public bool[,] shape;

		public float UIScale = 1;

		[Title("Script")]
		[NonSerialized]
		[OdinSerialize]
		[Inline]
		[Optional]
		[DarkBox(true)]
		public LuaScriptPackage script = new LuaScriptPackage
		{
			Asset = null,
			Store = new ScriptStore()
		};

		[Title("Equipment")]
		public bool IsEquipment = false;

		[Title("Add", horizontalLine: false)]
		/*[DisableIf("@!IsEquipment")]*/ public Pointf PointGain;
		/*[DisableIf("@!IsEquipment")]*/ public Statf    StatGain;
		/*[DisableIf("@!IsEquipment")]*/ public Elementf EfficiencyGain;

		[Title("Scale", horizontalLine: false)]
		/*[DisableIf("@!IsEquipment")]*/ public Pointf PointScale = Pointf.One;
		/*[DisableIf("@!IsEquipment")]*/ public Statf    StatScale       = Statf.One;
		/*[DisableIf("@!IsEquipment")]*/ public Elementf EfficiencyScale = Elementf.One;

		// This field is needed only for the sticker menu. (or other parts of the game that could deal with LOTS of stickers)
		// We probably don't want to instantiate a SkillInstance for every single sticker when they may not even be needed,
		// so instead we will bake this before building the game and on deserialization in editor based on the contents
		// of the script
		[FormerlySerializedAs("isConsumable")]
		[HideInInspector]
		public bool IsConsumable;

		[MinValue(1), /*ShowIf("@IsConsumable")*/]
		public int Charges;

		public Vector2Int Dimensions => new Vector2Int(Columns, Rows);

		public string Address { get; set; }



		#if UNITY_EDITOR
		public void BakeConsumability()
		{
			LuaAsset lua = script.Asset;
			if (lua != null)
			{
				IsConsumable = lua.TranspiledText.Contains("function " + LuaEnv.FUNC_CONSUME);
			}
		}

		private void OnShapeResized()
		{
			// https: //stackoverflow.com/a/6552985
			var newArray = new bool[Columns, Rows];

			int xmin = Math.Min(shape.GetLength(0), newArray.GetLength(0));
			int ymin = Math.Min(shape.GetLength(1), newArray.GetLength(1));

			for (var i = 0; i < ymin; ++i)
			{
				Array.Copy(shape, i * shape.GetLength(0), newArray, i * newArray.GetLength(0), xmin);
			}

			shape = newArray;
		}

		[UsedImplicitly]
		private static bool DrawShapeCell(Rect rect, bool value)
		{
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				value       = !value;
				GUI.changed = true;
				Event.current.Use();
			}

			EditorGUI.DrawRect(
				rect.Padding(1),
				value
					? new Color(0.1f, 0.8f, 0.2f)
					: new Color(0, 0, 0, 0.5f)
			);

			return value;
		}

#endif
		public LuaAsset Script => script.Asset;

		public ScriptStore LuaStore
		{
			get => script.Store;
			set => script.Store = value;
		}

		public string[] Requires => LuaUtil.battleRequires;
	}

#if UNITY_EDITOR
	public class StickerPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			foreach (string assetpath in importedAssets)
			{
				StickerAsset sticker = AssetDatabase.LoadAssetAtPath<StickerAsset>(assetpath);
				if (sticker == null) continue;

				sticker.BakeConsumability();
			}
		}
	}
#endif
}
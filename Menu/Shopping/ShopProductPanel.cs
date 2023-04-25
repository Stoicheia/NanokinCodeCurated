using System;
using Anjin.Util;
using API.Spritesheet.Indexing;
using Cysharp.Threading.Tasks;
using Data.Shops;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.RenderingElements.Barrel;
using Util.RenderingElements.PanelUI.Graphics;

namespace Overworld.Shopping
{
	/// <summary>
	/// A single barrel card in the shop barrel menu.
	/// </summary>
	public class ShopProductPanel : ListPanel
	{
		[FormerlySerializedAs("_label"), SerializeField]
		private PanelLabel Label;
		[FormerlySerializedAs("_sprite"), SerializeField]
		private PanelSpriteAddon Sprite;
		[SerializeField] private Vector2   ReferenceSize;
		private                  LootEntry _loot;

		private LootDisplayHandle _lootDisplay;

		public LootEntry Loot => _loot;

		private void OnDestroy()
		{
			_lootDisplay.ReleaseSafe();
		}

		public async UniTask ShowProduct([CanBeNull] LootEntry product)
		{
			if (product == null)
			{
				_loot      = null;
				Label.Text    = "";
				Sprite.Sprite = null;

				_lootDisplay.ReleaseSafe();
			}
			else
			{
				_loot   = product;
				Label.Text = product.GetName();

				_lootDisplay = await product.LoadDisplay();

				// APPLY DISPLAY
				// ----------------------------------------
				switch (_lootDisplay.method)
				{
					case LootDisplayMethod.Sprite:
						Sprite.Sprite = _lootDisplay.sprite;
						if (Sprite.Sprite != null)
						{
							Vector2 spriteSize = Sprite.Sprite.size();
							Sprite.Scale = Mathf.Min(ReferenceSize.x / spriteSize.x, ReferenceSize.y / spriteSize.y);
						}

						break;

					case LootDisplayMethod.Prefab:
						break;
				}
			}
		}
	}
}
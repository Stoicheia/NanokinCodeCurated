using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using Data.Shops;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Util.RenderingElements.Barrel;

namespace Overworld.Shopping
{
	/// <summary>
	/// The scrolling barrel menu.
	/// </summary>
	public class ShopBarrel : BarrelMenu
	{
		private List<LootEntry> _barrelProducts = new List<LootEntry>();

		private void Awake()
		{
			// Create the looping panel wheel buffer. (all the panels)
			EnqueueLoopingBuffer<ShopProductPanel>();
			FlushAdd();

			foreach (ListPanel panel in AllPanels)
			{
				panel.MoveToEndPosition();
				panel.gameObject.SetActive(false); // Hide the panel initially.
			}
		}

		public List<LootEntry> Products
		{
			set
			{
				foreach (ListPanel panel in AllPanels)
				{
					panel.gameObject.SetActive(true);
					panel.MoveToEndPosition();
				}

				_barrelProducts.Clear();
				_barrelProducts.AddRange(value);

				if (value.Count == 0)
				{
					Debug.LogError("Trying to show shop with no products! Abort!");
					return;
				}

				UseSlots(value.Count);
				SelectAt(SmartSelectionIndex);
			}
		}

		public override void UpdateBarrelPanel(int index, ListPanel panel, bool isUsed)
		{
			ShopProductPanel shopPanel = (ShopProductPanel) panel;
			shopPanel.ShowProduct(isUsed ? _barrelProducts[index] : null).Forget();
		}

// 		private void Update()
// 		{
// #if UNITY_EDITOR
// 			if (GameInputs.IsPressed(Key.Numpad0))
// 			{
// 				LootEntry product = _startup.Products.Last();
//
// 				ShopProductPanel panel = Add<ShopProductPanel>();
// 				panel.ShowProduct(product);
//
// 				FlushAdd();
//
// 				Debug.Log($"There are now {AllPanels.Count} panels in the barrel.");
// 			}
// #endif
// 		}
	}
}
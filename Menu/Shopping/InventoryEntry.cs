using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data.Shops;
using TMPro;

namespace Overworld.Shopping
{
	public class InventoryEntry : MonoBehaviour
	{
		public LootEntry Loot => _loot;

		[SerializeField] private Color activeColor;
		[SerializeField] private Color inactiveColor;

		[SerializeField] private TMP_Text entryName;
		[SerializeField] private TMP_Text purchaseAmount;

		private bool soldOut;
		private bool canHold;
		private bool canAfford;

		private int index;

		private LootEntry _loot;

		private bool _isInvalid = false;

		public void Initialize(LootEntry loot, bool soldOut, bool canAfford, bool canHold)
		{
			_loot = loot;
			this.soldOut = soldOut;
			this.canAfford = canAfford;
			this.canHold = canHold;
			index = _loot.index;

			entryName.text = _loot.GetName();
			entryName.fontStyle = ((!soldOut && canAfford && canHold) ? FontStyles.Normal : FontStyles.Strikethrough);

			ShopMenu.OnSelectionChange += ToggleSelection;
		}

		public void ToggleSelection(params object[] args)
		{
			int currentSelection = (int)args[0];
			bool selected = (currentSelection == index);
			entryName.color = (!selected ? inactiveColor : activeColor);
			purchaseAmount.text = "";
		}

		public void UpdateEntryNameDisplay(bool soldOut, bool canAfford, bool canHold)
		{
			if (!soldOut)
			{
				if (!canAfford)
				{
					entryName.fontStyle = FontStyles.Strikethrough;
				}
				else
				{
					if (!canHold)
					{
						entryName.fontStyle = FontStyles.Strikethrough;
					}
					else
					{
						entryName.fontStyle = FontStyles.Normal;
					}
				}
			}
			else
			{
				entryName.fontStyle = FontStyles.Strikethrough;
			}
		}

		public void UpdatePurchaseAmountDisplay(bool soldOut, bool canAfford, bool canHold, int quantity, bool canMoveLeft, bool canMoveRight)
		{
			this.soldOut = soldOut;
			this.canAfford = canAfford;
			this.canHold = canHold;

			if (!soldOut)
			{
				if (!canAfford)
				{
					purchaseAmount.text = "";
					entryName.fontStyle = FontStyles.Strikethrough;
				}
				else
				{
					if (!canHold)
					{
						purchaseAmount.text = "";
						entryName.fontStyle = FontStyles.Strikethrough;
					}
					else
					{
						string toDisplay = quantity.ToString();
						toDisplay = string.Format("{0}{1}", (canMoveLeft ? "<" : ""), toDisplay);
						toDisplay = string.Format("{0}{1}", toDisplay, (canMoveRight ? ">" : ""));
						purchaseAmount.text = toDisplay;
						entryName.fontStyle = FontStyles.Normal;
					}
				}
			}
			else
			{
				purchaseAmount.text = "";
				entryName.fontStyle = FontStyles.Strikethrough;
			}
		}

		private void OnDestroy()
		{
			ShopMenu.OnSelectionChange -= ToggleSelection;
		}
	}
}

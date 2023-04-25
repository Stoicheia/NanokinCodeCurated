using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Data.Shops;
using Menu.Start;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using Util.Addressable;
using Util.Components.UI;
using Util.RenderingElements.PanelUI.Graphics;

namespace Overworld.Shopping
{
	/// <summary>
	/// Tthe shop UI. Presents a shop with all the panels and postfx and fancy vfx and WOWs.
	/// </summary>
	public class ShopMenu : StaticMenu<ShopMenu>
	{
		public delegate void MenuEvent(params object[] args);
		public static MenuEvent OnSelectionChange;

		public static Action OnShopMenuDisplayUI;

		public static Shop shop;

		[Title("References")]
		[SerializeField] private WorldToCanvasRaycast BarrelMenuWorldRaycast;
		[SerializeField] private ShopBarrel   Barrel;
		[SerializeField] private GameObject[] ActiveObjects;
		[SerializeField] public TMP_Text ShopTitle;
		[SerializeField] public TMP_Text DescriptionLabel;
		[SerializeField] public TMP_Text OwnedLabel;
		[SerializeField] public TMP_Text WalletLabel;
		[SerializeField] private TMP_Text PriceLabel;
		[SerializeField] private TMP_Text QuantityLabel; // TODO use label scroller with arrow icons
		[SerializeField] private PanelArrows  QuantityArrows;
		[SerializeField] private Transform InventoryDisplay;
		[SerializeField] private UnityEngine.UI.Image LootIcon;
		[SerializeField] private GameObject ScreenCanvas;
		[SerializeField] private GameObject MenuCanvas;

		[Title("Design")]
		[SerializeField] public PlayableDirector Director;
		[SerializeField] private PlayableAsset             ANIM_Enter;
		[SerializeField] private PlayableAsset             ANIM_Leave;
		//[SerializeField] private BarrelAnimationProperties AnimProperties;
		[Space]
		[SerializeField] private AudioDef SFX_Entrance;
		[SerializeField] private AudioDef SFX_PurchaseSuccess;
		[SerializeField] private AudioDef SFX_PurchaseFail;
		[SerializeField] private AudioDef SFX_IncreaseQuantity;
		[SerializeField] private AudioDef SFX_DecreaseQuantity;
		[SerializeField] private AudioDef SFX_Scroll;
		[SerializeField] private AudioDef SFX_PurchaseError;

		[Space]
		[SerializeField] private Vector3 entryEulers;

		[SerializeField] private GameObject EntryPrefab;

		[NonSerialized] public MenuInteractivity interactivity;

		private int _buyQuantity = 1;
		private int _maxCanBuy;
		private int _maxCanHold;
		private int _maxCanAfford;
		private int currentEntryIndex = 0;

		public bool CanBuy => _maxCanBuy > 0;

		private RectTransform iconTransform;

		private List<InventoryEntry> displayEntries;

		/// <summary>
		/// Set the transform of the object that the barrel menu will be centered around.
		/// </summary>
		public Transform MenuCenterObject
		{
			set => BarrelMenuWorldRaycast.SetWorldPos(value);
		}

		public Vector3 MenuCenterObjectOffset
		{
			set => BarrelMenuWorldRaycast.WorldTransformOffset = value;
		}

		public int BuyQuantity
		{
			get => _buyQuantity;
			set
			{
				_buyQuantity = value.Clamp(1, _maxCanBuy);
				RefreshPrice();
			}
		}


		protected override void OnAwake()
		{
			displayEntries = new List<InventoryEntry>();

			iconTransform = LootIcon.GetComponent<RectTransform>();

			new AsyncHandles();
			interactivity = MenuInteractivity.None;

			//AnimProperties.onUpdateMenuVisibility += SetVisible;
			//AnimProperties.onUpdateInteractivity += interactivity =>
			//{
			//	this.interactivity = interactivity;
			//};

			Barrel.ChangedSelection += RefreshUI;

			SetVisible(false);
		}

		protected override async UniTask enableMenu()
		{
			OnShopMenuDisplayUI?.Invoke();

			if (shop == null)
			{
				this.LogError("No input shop asset, using default shop asset.");
				menuActive = false;
				//MenuCanvas.SetActive(false);
				//ScreenCanvas.SetActive(false);
				SetVisible(false);
				return;
			}

			//MenuCanvas.SetActive(true);
			ShopTitle.text = shop.Title;

			shop.boughtCount     = 0;
			shop.boughtIndices   = new List<int>();
			shop.boughtAddresses = new List<string>();
			shop.boughtTags      = new List<string>();

			for (var i = 0; i < shop.Products.Count; i++)
			{
				LootEntry loot = shop.Products[i];
				loot.index = i;

				if (loot.IsValid()) {

					GameObject entryInstance = Instantiate(EntryPrefab);
					entryInstance.transform.SetParent(InventoryDisplay);
					entryInstance.transform.localScale = Vector3.one;
					entryInstance.transform.localEulerAngles = entryEulers;

					Vector3 localPosition = entryInstance.transform.localPosition;
					localPosition.z = 0;
					entryInstance.transform.localPosition = localPosition;

					// Update the maximum amount we can buy of this loot:
					_maxCanHold = loot.GetMaxAllowed() - loot.GetOwnedCount();
					_maxCanAfford = SaveManager.current.Money / loot.GetPrice();

					_maxCanBuy = Mathf.Min(_maxCanHold, _maxCanAfford);

					if (loot.Quantity > -1) // -1 is infinite quantity
						_maxCanBuy = Mathf.Min(_maxCanBuy, loot.Quantity);

					InventoryEntry inventoryEntry = entryInstance.GetComponent<InventoryEntry>();
					inventoryEntry.Initialize(loot, ((_maxCanBuy == 0) && (loot.Quantity != -1)), (_maxCanAfford > 0), (_maxCanHold > 0));
					displayEntries.Add(inventoryEntry);
				} else {

				}
			}

			currentEntryIndex = 0;
			OnSelectionChange?.Invoke(currentEntryIndex);

			ShowProduct().Forget();

			//OverworldHUD.ShowCredits().Forget();

			interactivity = MenuInteractivity.Navigate; // Allow choosing as soon as the menu is starting to show up.

			//Barrel.Products = shop.Products;
			GameSFX.PlayGlobal(SFX_Entrance, transform);
			Director.Play(ANIM_Enter);
			await UniTask.WaitUntil(() => Director.state == PlayState.Paused);

			interactivity = MenuInteractivity.Interact;
		}

		protected override async UniTask disableMenu()
		{
			interactivity = MenuInteractivity.None;
			Director.Play(ANIM_Leave);

			//OverworldHUD.HideCredits().Forget();

			while (InventoryDisplay.childCount > 0)
			{
				GameObject entry = InventoryDisplay.GetChild(0).gameObject;
				entry.transform.SetParent(null);
				Destroy(entry);
			}

			displayEntries.Clear();

			//MenuCanvas.SetActive(false);

			await UniTask.WaitUntil(() => Director.state == PlayState.Paused);
		}

		private void Update()
		{
			if (interactivity >= MenuInteractivity.Navigate)
			{
				if (GameInputs.move.up.IsPressed)
				{
					//if (Barrel.TryMoveSelection(-1))
					currentEntryIndex = (currentEntryIndex - 1).Wrap(0, (displayEntries.Count - 1));
					GameSFX.PlayGlobal(SFX_Scroll, transform, 0.985f);
					OnSelectionChange?.Invoke(currentEntryIndex);
					ShowProduct().Forget();
				}
				else if (GameInputs.move.down.IsPressed)
				{
					//if (Barrel.TryMoveSelection(1))
					currentEntryIndex = (currentEntryIndex + 1).Wrap(0, (displayEntries.Count - 1));
					GameSFX.PlayGlobal(SFX_Scroll, transform);
					OnSelectionChange?.Invoke(currentEntryIndex);
					ShowProduct().Forget();
				}

				if (CanBuy)
				{
					if (GameInputs.move.left.IsPressed)
					{
						int previousQuantity = BuyQuantity;
						BuyQuantity--;

						bool hasChanged = previousQuantity != BuyQuantity;
						if (hasChanged)
						{
							RefreshBuyQuantity();
							GameSFX.PlayGlobal(SFX_DecreaseQuantity);
							QuantityArrows.PushLeft();
						}
					}

					if (GameInputs.move.right.IsPressed)
					{
						int previousQuantity = BuyQuantity;
						BuyQuantity++;

						bool hasChanged = previousQuantity != BuyQuantity;
						if (hasChanged)
						{
							RefreshBuyQuantity();
							GameSFX.PlayGlobal(SFX_IncreaseQuantity);
							QuantityArrows.PushRight();
						}
					}
				}
			}

			if (interactivity >= MenuInteractivity.Interact)
			{
				if (GameInputs.confirm.IsPressed)
				{
					bool isSuccess = ConfirmPurchase();

					if (isSuccess)
						GameSFX.PlayGlobal(SFX_PurchaseSuccess, transform);
					else
						GameSFX.PlayGlobal(SFX_PurchaseFail, transform);
				}

				DoExitControls();
			}
		}

		private async UniTask ShowProduct()
		{
			LootEntry loot = SelectedLoot;
			if (loot == null) return;

			LootDisplayHandle handle = await loot.LoadDisplay();

			switch (handle.method)
			{
				case LootDisplayMethod.Sprite:
					LootIcon.sprite = handle.sprite;

					if (LootIcon.sprite != null)
					{
						Vector2 spriteSize = LootIcon.sprite.size();
						//float scale = Mathf.Min(Mathf.Min((182 / spriteSize.x), (182 / spriteSize.y)), 2);
						iconTransform.localScale = new Vector2(2, 2);
					}

					break;
				case LootDisplayMethod.Spritesheet:
					LootIcon.sprite = handle.sprite;
					iconTransform.localScale = new Vector2(4, 4);

					//if (LootIcon.sprite != null)
					//{
					//	Vector2 spriteSize = LootIcon.sprite.size();
					//	float scale = Mathf.Min(Mathf.Min((150 / spriteSize.x), (150 / spriteSize.y)), 2);
					//	iconTransform.localScale = new Vector2(scale, scale);
					//}

					break;
				case LootDisplayMethod.Prefab:
					break;

				default:
					break;
			}

			QuantityLabel.text = GetQuantityLabelText(loot);

			RefreshUI();
		}

		private bool ConfirmPurchase()
		{
			LootEntry loot = SelectedLoot;

			if (!CanBuy)
			{
				GameSFX.PlayGlobal(SFX_PurchaseError, this);
				return false;
			}

			int qty   = BuyQuantity;
			int total = qty * loot.GetPrice();

			for (var i = 0; i < qty; i++)
			{
				loot.AwardOne();

				shop.boughtCount++;
				shop.boughtIndices.Add(loot.index);
				shop.boughtAddresses.Add(loot.Address);
				if (loot.Tags != null)
					shop.boughtTags.AddRange(loot.Tags);
			}

			SaveManager.current.Money -= total;
			RefreshUI();
			RefreshEntryNameDisplays();
			return true;
		}

		private void SetVisible(bool state)
		{
			interactivity = MenuInteractivity.None;
			activator.Set(false);
			//MenuCanvas.SetActive(false);
		}

		private void RefreshUI()
		{
			LootEntry loot  = SelectedLoot;
			if (loot == null) return;

			BuyQuantity = 1;
			//ShopProductPanel panel = SelectedPanel;

			//OwnedLabel.text = loot.GetMaxAllowed() > -1
				//? $"{loot.GetOwnedCount()} / {loot.GetMaxAllowed()}"
				//: $"{loot.GetOwnedCount()}";
			RefreshBuyQuantity();
			RefreshPrice();

			//WalletLabel.text     = $"${SaveManager.current.Money}";
			DescriptionLabel.text = loot.GetDescription();
		}

		private void RefreshEntryNameDisplays()
		{
			for (var i = 0; i < shop.Products.Count; i++)
			{
				LootEntry loot = shop.Products[i];

				if (loot.IsValid())
				{
					// Update the maximum amount we can buy of this loot:
					_maxCanHold = loot.GetMaxAllowed() - loot.GetOwnedCount();
					_maxCanAfford = SaveManager.current.Money / loot.GetPrice();

					_maxCanBuy = Mathf.Min(_maxCanHold, _maxCanAfford);

					if (loot.Quantity > -1) // -1 is infinite quantity
						_maxCanBuy = Mathf.Min(_maxCanBuy, loot.Quantity);

					displayEntries[i].UpdateEntryNameDisplay(((_maxCanBuy == 0) && (loot.Quantity != -1)), (_maxCanAfford > 0), (_maxCanHold > 0));
				}
			}
		}

		//private LootEntry SelectedLoot => ((ShopProductPanel) Barrel.SelectedPanel).Loot;
		private LootEntry SelectedLoot => (currentEntryIndex >= 0 && currentEntryIndex < displayEntries.Count ? displayEntries[currentEntryIndex].Loot : null);

		//private ShopProductPanel SelectedPanel => (ShopProductPanel) Barrel.SelectedPanel;

		public void RefreshPrice()
		{
			if (SelectedLoot == null) return;
			int val = SelectedLoot.GetPrice() * BuyQuantity;
			PriceLabel.text = string.Format("{0}", val);
		}

		private void RefreshBuyQuantity()
		{
			LootEntry loot = SelectedLoot;
			if (loot == null) return;
			// Update the maximum amount we can buy of this loot:
			_maxCanHold   = loot.GetMaxAllowed() - loot.GetOwnedCount();
			_maxCanAfford = SaveManager.current.Money / loot.GetPrice();

			_maxCanBuy = Mathf.Min(_maxCanHold, _maxCanAfford);

			if (loot.Quantity > -1) // -1 is infinite quantity
				_maxCanBuy = Mathf.Min(_maxCanBuy, loot.Quantity);

			// Reset the input:
			BuyQuantity        = BuyQuantity.Maximum(_maxCanBuy);
			bool soldOut = ((_maxCanBuy == 0) && (loot.Quantity > -1));
			bool canMoveLeft = (CanBuy && (BuyQuantity > 1));
			bool canMoveRight = (CanBuy && (BuyQuantity < _maxCanBuy));

			displayEntries[currentEntryIndex].UpdatePurchaseAmountDisplay(soldOut, (_maxCanAfford > 0), (_maxCanHold > 0), BuyQuantity, canMoveLeft, canMoveRight);

			QuantityLabel.text = GetQuantityLabelText(loot);

			//QuantityArrows.SetVisible(CanBuy);
			//QuantityArrows.Left.gameObject.SetActive(CanBuy && BuyQuantity > 1);
			//QuantityArrows.Right.gameObject.SetActive(CanBuy && BuyQuantity < _maxCanBuy);
		}

		private string GetQuantityLabelText(LootEntry loot)
		{
			if (_maxCanBuy > 0)
			{
				return string.Format("STOCK: {0}", _maxCanBuy.ToString());
			}
			else
			{
				if (loot.Quantity == 0)
				{
					return "STOCK: SOLD OUT";
				}
				else
				{
					if (_maxCanAfford <= 0)
					{
						return "CAN'T AFFORD";
					}
					else
					{
						return "INVENTORY FULL";
					}
				}
			}
		}
	}
}
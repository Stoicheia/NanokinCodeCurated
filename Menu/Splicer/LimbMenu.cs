using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Anjin.Core.Flags;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using Overworld.Controllers;
using Overworld.UI;
using Puppets.Render;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util.Addressable;
using Util.Odin.Attributes;
using Debug = UnityEngine.Debug;
using MenuController = Anjin.Nanokin.MenuManager;

public class LimbMenu : StaticMenu<LimbMenu>, IMenuKeyboardInputReceiver
{
	[Title("Name Input")]
	public MenuKeyboardUI OnscreenKeyboard;

	public TMP_InputField MyInputFieldInteractible;
	public TMP_Text MyInputField;
	public TMP_Text NameField;

	// References
	// ----------------------------------------
	[Title("References")]
	[Title("Prefabs", HorizontalLine = false)]
	[FormerlySerializedAs("Prefab_LimbCell")]
	public LimbCard Prefab_LimbCard;
	[SerializeField] private GameObject TutorialScreen;

	[Title("UI Elements", HorizontalLine = false)]
	public SplicerTab[] UI_Tabs;
	public SplicerTab	UI_FavoritesTab;
	public NanokinInfoUI UI_MonsterPanel;
	public LimbInfoUI    UI_LimbPanel;
	public NanokinInfoUI UI_NanokinPanel;
	public ScrollRect    UI_LimbScrollRect;
	public InputButtonLabel SwitchTabLeftLabel;
	public InputButtonLabel SwitchTabRightLabel;
	public InputButtonLabel SwitchCharacterLeftLabel;
	public InputButtonLabel SwitchCharacterRightLabel;
	public InputButtonLabel ToggleFavoriteLabel;
	public InputButtonLabel ToggleRenameLabel;
	public InputButtonLabel ToggleViewLabel;
	public TextMeshProUGUI ToggleFavoritePrompt;
	public TextMeshProUGUI ToggleViewPrompt;
	public Button BackButton;

	[Title("Containers", HorizontalLine = false)]
	[FormerlySerializedAs("MainSubmenuRoot")]
	public RectTransform Root_LimbsPreviewGrid;

	[Title("SFX")]
	public AudioDef SFX_SelectCard;
	public AudioDef SFX_HoverCard;
	public AudioDef SFX_ChangeTab;
	public AudioDef SFX_EquipLimb;
	public AudioDef SFX_OpenSubmenu;
	public AudioDef SFX_ExitSubmenu;

	// Runtime
	// ----------------------------------------
	public static CharacterEntry input = null;
	public static int currentCharacterIndex = 0;

	[SerializeField] private float ConfirmationCooldown = 0.5f; //whatever
	private float _lastConfirmTime;

	[Title("Debugging")]
	private AsyncHandles _handles;
	[Space]
	[ShowInPlay] private CharacterEntry _selectedCharacter;
	[ShowInPlay] private int _selectedTab;
	[Space]
	[ShowInPlay] private List<LimbCard> _limbCards;
	[Space]
	[ShowInPlay] private List<LimbEntry> _ownedHeads;
	[ShowInPlay] private List<LimbEntry> _ownedBodies;
	[ShowInPlay] private List<LimbEntry> _ownedArms1;
	[ShowInPlay] private List<LimbEntry> _ownedArms2;

	private bool viewingFavorites;

	private LimbCard _equippedCard;

	private List<CharacterEntry> party;

	private States _state;

	private SaveData savedata;

	public enum States
	{
		Regular,
		SubMenu
	}

	private static readonly LimbType[] LimbTabOrder =
	{
		LimbType.Head,
		LimbType.Body,
		LimbType.Arm1,
		LimbType.Arm2
	};

	private LimbType SelectedLimbCategory => LimbTabOrder[_selectedTab];

	public override void Awake()
	{
		base.Awake();

		viewingFavorites = false;

		_handles     = new AsyncHandles();
		_limbCards   = new List<LimbCard>();
		_ownedHeads  = new List<LimbEntry>();
		_ownedBodies = new List<LimbEntry>();
		_ownedArms1  = new List<LimbEntry>();
		_ownedArms2  = new List<LimbEntry>();

		// SETUP the UI for grid tabs
		// ----------------------------------------
		for (var i = 0; i < UI_Tabs.Length; i++)
		{
			SplicerTab tab = UI_Tabs[i];

			tab.tabIndex = i;
			tab.OnClicked += (clickedTab, _) =>
			{
				Live.SelectGridTab(clickedTab.tabIndex);
			};
		}

		UI_FavoritesTab.OnClicked += (clickedTab, _) =>
		{
			Live.ToggleFavoriteViewing();
		};
		MyInputFieldInteractible.onSelect.AddListener(NameClickEvent);

		if (SwitchTabLeftLabel)
		{
			SwitchTabLeftLabel.Button = GameInputs.menuLeft;
		}

		if (SwitchTabRightLabel)
		{
			SwitchTabRightLabel.Button = GameInputs.menuRight;
		}

		if (SwitchCharacterLeftLabel)
		{
			SwitchCharacterLeftLabel.Button = GameInputs.menuLeft2;
		}

		if (SwitchCharacterRightLabel)
		{
			SwitchCharacterRightLabel.Button = GameInputs.menuRight2;
		}

		if (ToggleFavoriteLabel)
		{
			ToggleFavoriteLabel.Button = GameInputs.favoriteItem;
		}

		if (ToggleRenameLabel)
		{
			ToggleRenameLabel.Button = GameInputs.selectField;
		}

		if (ToggleViewLabel)
		{
			ToggleViewLabel.Button = GameInputs.detachItem;
		}

		BackButton.onClick.AddListener(OnExitStatic);
	}

	private void NameClickEvent(string _)
	{
		OnscreenKeyboard.Open(this, MyInputFieldInteractible);
		NameField.gameObject.SetActive(false);
		MyInputField.gameObject.SetActive(true);
	}

	public void ChangeMonster(CharacterEntry character)
	{
		_selectedCharacter = character;

		UI_MonsterPanel.ChangeCharacter(character);
		UI_MonsterPanel.ChangeMonster(character.nanokin);
	}

	protected override async UniTask enableMenu()
	{
		await GameController.TillIntialized();
		//await Lua.initTask;
		savedata = await SaveManager.GetCurrentAsync();
		party = savedata.Party;

		_limbCards.DestroyAll();
		_ownedBodies.Clear();
		_ownedHeads.Clear();
		_ownedArms1.Clear();
		_ownedArms2.Clear();

		_lastConfirmTime = Time.time;

		if (input == null)
		{
			if (party.Count == 0)
			{
				Debug.LogError("This menu cannot be used without a monster, and the savefile has no monsters.");
				return;
			}

			input = party[0];
		}

		ChangeMonster(input);

		GameInputs.mouseUnlocks.Set("limb_menu", true);

		ToggleViewPrompt.text = (!viewingFavorites ? "View Favorites" : "View All");

		// Gather & load all the limb items.
		// ----------------------------------------

		foreach (LimbEntry limb in savedata.Limbs)
		{
			NanokinLimbAsset asset = limb.instance.Asset;

			if (asset == null) continue;

			if ((asset.Kind == LimbType.Head) && !_ownedHeads.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedHeads.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Body) && !_ownedBodies.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedBodies.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Arm1) && !_ownedArms1.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedArms1.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Arm2) && !_ownedArms2.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedArms2.Add(limb);
				}
			}
		}

		await RefreshUI();

		if (!Flags.GetBool("tut_splice_menu"))
		{
			Flags.SetBool("tut_splice_menu", true);
			ShowSplashScreen("SplashScreens/Demo_SpliceMenu").Forget();
		}
	}

	protected override UniTask disableMenu()
	{
		//if (!SplicerHub.CanConfirm) return UniTask.CompletedTask;
		//SplicerHub.ResetConfirm();

		GameInputs.mouseUnlocks.Set("limb_menu", false);

		_handles.ReleaseAll();

		_limbCards.DestroyAll();
		_ownedBodies.Clear();
		_ownedHeads.Clear();
		_ownedArms1.Clear();
		_ownedArms2.Clear();
		_selectedTab = 0;

		return UniTask.CompletedTask;
	}

	private async UniTask ShowSplashScreen(string address)
	{
		GameSFX.PlayGlobal(SFX_OpenSubmenu, this);

		//OverworldHUD.HideCredits();

		//await GameEffects.FadeOut(ScreenFadeDuration);
		ChangeState(States.SubMenu);
		await MenuController.SetSplicerBG(true);

		await SplashScreens.ShowPrefabAsync(address, async () =>
		{
			GameSFX.PlayGlobal(SFX_ExitSubmenu, this);

			//OverworldHUD.ShowCredits();
			ChangeState(States.Regular);
		});
	}

	private void ChangeState(States newstate)
	{
		// Exit the old state
		States oldstate = _state;
		switch (oldstate)
		{
			case States.Regular:
				break;
			default:
				EnableMenu();

				break;
		}

		// Enter the new state
		_state = newstate;
		switch (_state)
		{
			case States.Regular:
				break;
			default:
				DisableMenu();

				break;
		}
	}

	public async UniTask RefreshUI()
	{
		NameField.gameObject.SetActive(true);
		MyInputField.gameObject.SetActive(false);
		var watch = Stopwatch.StartNew();

		// Get the limbs for the selected category
		List<LimbEntry> categoryLimbs = null;
		switch (SelectedLimbCategory)
		{
			case LimbType.Head:
				categoryLimbs = _ownedHeads;
				break;
			case LimbType.Body:
				categoryLimbs = _ownedBodies;
				break;
			case LimbType.Arm1:
				categoryLimbs = _ownedArms1;
				break;
			case LimbType.Arm2:
				categoryLimbs = _ownedArms2;
				break;
		}

		if (categoryLimbs == null)
		{
			this.LogError($"Couldn't get limb collection for category={SelectedLimbCategory}");
			return;
		}


		// Refresh tabs to match selected category
		// ----------------------------------------
		for (var i = 0; i < UI_Tabs.Length; i++)
		{
			SplicerTab tab = UI_Tabs[i];
			tab.SetActive(_selectedTab == i);
		}


		// Refresh cards to match selected category
		// ----------------------------------------
		var tasks = new List<UniTask>();

		_limbCards.DestroyAll();
		for (var i = 0; i < categoryLimbs.Count; i++)
		{
			LimbCard card = PrefabPool.Rent(Prefab_LimbCard, Root_LimbsPreviewGrid);

			card.onPointerEnter = OnHoverCard;
			card.onPointerExit = _ =>
			{
				if (EventSystem.current.currentSelectedGameObject != null
				    && EventSystem.current.currentSelectedGameObject.TryGetComponent(out LimbCard selected))
				{
					// Restore comparison to the selected card
					CompareLimb(selected);
					ToggleFavoritePrompt.text = (!card.Favorited ? "Favorite Limb" : "Un-Favorite Limb");
				}
				else
				{
					CompareLimb(null);
				}
			};
			card.onPointerDown = EquipLimb;

			_limbCards.Add(card);
		}

		for (var i = 0; i < _limbCards.Count; i++)
		{
			LimbCard  card = _limbCards[i];
			LimbEntry limb = categoryLimbs[i];

			tasks.Add(card.SetLimb(limb));

			if (card.limb == _selectedCharacter.Arm1 ||
			    card.limb == _selectedCharacter.Arm2 ||
			    card.limb == _selectedCharacter.Body ||
			    card.limb == _selectedCharacter.Head)
			{
				_equippedCard = card;
			}
		}

		await UniTask.WhenAll(tasks);

		// Setup keyboard/gamepad navigation
		// ----------------------------------------
		int nCards = categoryLimbs.Count;

		const int COLS = 4;

		for (var i = 0; i < nCards; i++)
		{
			var nav = new Navigation
			{
				mode = Navigation.Mode.Explicit
			};

			int row     = Mathf.FloorToInt(i / (float) COLS);
			int column  = i % COLS;
			int numRows = Mathf.CeilToInt(nCards / (float) COLS);

			// Left and right. Wrap around on same line.

			nav.selectOnLeft  = column == 0 ? _limbCards[Mathf.Min(i + 3, _limbCards.Count - 1)] : _limbCards[i - 1];
			nav.selectOnRight = column == 3 || i >= nCards - 1 ? _limbCards[Mathf.Max(i - 3, 0)] : _limbCards[i + 1];

			// First row
			if (row == 0)
			{
				// Should loop around to the last or next to last row
				int lastRowWidth = nCards % COLS;
				nav.selectOnUp = _limbCards[nCards - lastRowWidth + column + (column < lastRowWidth ? 0 : -COLS)];
			}
			else
			{
				nav.selectOnUp = _limbCards[(row - 1) * COLS + column];
			}

			// Last row
			if (row == numRows - 1)
			{
				nav.selectOnDown = nCards > COLS ? _limbCards[column] : null;
			}
			else
			{
				int next = (row + 1) * COLS + column;
				nav.selectOnDown = next >= nCards ? _limbCards[column] : _limbCards[(row + 1) * COLS + column];
			}

			_limbCards[i].onSelected = SelectCard;
			_limbCards[i].navigation = nav;
		}

		// Select the equipped limb.
		if (_equippedCard != null)
		{
			if (_equippedCard.limb != null)
			{
				UI_LimbPanel.ChangeLimb(_equippedCard.limb, _selectedCharacter.Level);
				_equippedCard.SetEquipped(true);
				SelectCard(_equippedCard);
			}
		}
		else
		{
			if (_limbCards.Count > 0)
			{
				SelectCard(_limbCards[0]);
			}
		}

		watch.Stop();
		Debug.Log($"Splice Submenu Update: {watch.ElapsedMilliseconds} ms, {watch.ElapsedTicks} ticks");
	}

	private void ChangeCharacter()
	{
		_selectedTab = 0;

		ChangeMonster(input);

		RefreshUI().Forget();
	}

	private void OnHoverCard(LimbCard obj)
	{
		GameSFX.PlayGlobal(SFX_HoverCard, this);
		CompareLimb(obj);

		if (obj != null)
		{
			ToggleFavoritePrompt.text = (!obj.Favorited ? "Favorite Limb" : "Un-Favorite Limb");
		}
	}

	public void SelectCard([NotNull] LimbCard card)
	{
		//if (!SplicerHub.CanConfirm) return;
		//SplicerHub.ResetConfirm();
		if (EventSystem.current.alreadySelecting)
			GameSFX.PlayGlobal(SFX_SelectCard, this);
		else
			EventSystem.current.SetSelectedGameObject(card.gameObject);

		ScrollTo(card.rect);
		CompareLimb(card);

		if (card != null)
		{
			ToggleFavoritePrompt.text = (!card.Favorited ? "Favorite Limb" : "Un-Favorite Limb");
		}
	}

	public void CompareLimb([CanBeNull] LimbCard card)
	{
		if (card == null || card == _equippedCard)
		{
			UI_NanokinPanel.CompareNone();
			UI_LimbPanel.CompareNone();
			return;
		}

		// LIMB COMPARISON
		// ----------------------------------------
		UI_LimbPanel.CompareWith(card.limb, _selectedCharacter.Level);

		// TOTAL COMPARISON
		// ----------------------------------------
		LimbInstance    compareLimb = card.limb;
		LimbType        type        = compareLimb.Asset.Kind;
		NanokinInstance nanokin     = _selectedCharacter.nanokin;
		LimbInstance    activeLimb  = nanokin[type];

		// Temporarily equip the limb so we can calculate the nanokin's stats with it
		nanokin[type] = compareLimb;

		Pointf   newPoints     = nanokin.MaxPoints;
		Statf    newStats      = nanokin.Stats;
		Elementf newEfficiency = nanokin.Efficiencies;

		nanokin[type] = activeLimb;

		// Send the results to the UI elements
		UI_NanokinPanel.CompareWith(newPoints, newStats, newEfficiency);
	}

	public void EquipLimb([NotNull] LimbCard card)
	{
		LimbInstance cardInstance = card.limb;
		LimbEntry    cardEntry    = card.limb.entry;

		if (_equippedCard != null) _equippedCard.SetEquipped(false);
		_equippedCard = card;
		_equippedCard.SetEquipped(true);

		_selectedCharacter[cardInstance.Asset.Kind] = cardEntry;

		card.SetEquipped(true);

		ScrollTo(card.rect);

		UI_NanokinPanel.OnLimbChange();
		UI_LimbPanel.ChangeLimb(cardInstance, _selectedCharacter.Level);

		GameSFX.PlayGlobal(SFX_EquipLimb, this);

		SaveManager.SaveCurrent();
		savedata = SaveManager.current;
	}

	public void ToggleLimbFavorability([NotNull] LimbCard card)
	{
		card.SetFavorited(!card.Favorited);

		SaveManager.SaveCurrent();
		savedata = SaveManager.current;

		if (!viewingFavorites)
		{
			ToggleFavoritePrompt.text = (!card.Favorited ? "Favorite Limb" : "Un-Favorite Limb");
		}
		else
		{
			_limbCards.DestroyAll();

			switch (SelectedLimbCategory)
			{
				case LimbType.Head:
					_ownedHeads.Clear();

					foreach (LimbEntry limb in savedata.Limbs)
					{
						NanokinLimbAsset asset = limb.instance.Asset;

						if (asset == null) continue;

						if ((asset.Kind == LimbType.Head) && !_ownedHeads.Contains(limb) && limb.Favorited)
						{
							_ownedHeads.Add(limb);
						}
					}

					break;
				case LimbType.Body:
					_ownedBodies.Clear();

					foreach (LimbEntry limb in savedata.Limbs)
					{
						NanokinLimbAsset asset = limb.instance.Asset;

						if (asset == null) continue;

						else if ((asset.Kind == LimbType.Body) && !_ownedBodies.Contains(limb) && limb.Favorited)
						{
							_ownedBodies.Add(limb);
						}
					}

					break;
				case LimbType.Arm1:
					_ownedArms1.Clear();

					foreach (LimbEntry limb in savedata.Limbs)
					{
						NanokinLimbAsset asset = limb.instance.Asset;

						if (asset == null) continue;

						else if ((asset.Kind == LimbType.Arm1) && !_ownedBodies.Contains(limb) && limb.Favorited)
						{
							_ownedBodies.Add(limb);
						}
					}

					break;
				case LimbType.Arm2:
					_ownedArms2.Clear();

					foreach (LimbEntry limb in savedata.Limbs)
					{
						NanokinLimbAsset asset = limb.instance.Asset;

						if (asset == null) continue;

						else if ((asset.Kind == LimbType.Arm2) && !_ownedBodies.Contains(limb) && limb.Favorited)
						{
							_ownedBodies.Add(limb);
						}
					}

					break;
			}

			RefreshUI().Forget();
		}
	}

	public void ScrollTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		UGUI.ScrollTo(target);
		UI_LimbScrollRect.ScrollTo(target);
	}

	private void OnDestroy()
	{
		_handles.ReleaseAll();
	}

	public void SelectGridTab(int index)
	{
		GameSFX.PlayGlobal(SFX_ChangeTab, this);
		_selectedTab = index;
		RefreshUI().Forget();
	}

	private void ToggleFavoriteViewing()
	{
		viewingFavorites = !viewingFavorites;
		UI_FavoritesTab.SetActive(viewingFavorites);

		ToggleViewPrompt.text = (!viewingFavorites ? "View Favorites" : "View All");

		_limbCards.DestroyAll();
		_ownedBodies.Clear();
		_ownedHeads.Clear();
		_ownedArms1.Clear();
		_ownedArms2.Clear();

		foreach (LimbEntry limb in savedata.Limbs)
		{
			NanokinLimbAsset asset = limb.instance.Asset;

			if (asset == null) continue;

			if ((asset.Kind == LimbType.Head) && !_ownedHeads.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedHeads.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Body) && !_ownedBodies.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedBodies.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Arm1) && !_ownedArms1.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedArms1.Add(limb);
				}
			}
			else if ((asset.Kind == LimbType.Arm2) && !_ownedArms2.Contains(limb))
			{
				if (!viewingFavorites || (viewingFavorites && limb.Favorited))
				{
					_ownedArms2.Add(limb);
				}
			}
		}

		RefreshUI().Forget();
	}

	private void Update()
	{
		if (_state != States.Regular)
			return;

		if (!MyInputFieldInteractible.isFocused)
		{
			if (GameInputs.selectField.IsPressed)
			{
				MyInputFieldInteractible.Select();
			}

			if (GameInputs.menuLeft.AbsorbPress(0.3f))
			{
				_selectedTab = (_selectedTab - 1).Wrap(4);
				RefreshUI().Forget();
			}
			else if (GameInputs.menuRight.AbsorbPress(0.3f))
			{
				_selectedTab = (_selectedTab + 1).Wrap(4);
				RefreshUI().Forget();
			}
			else if (GameInputs.menuLeft2.AbsorbPress(0.3f))
			{
				currentCharacterIndex = (currentCharacterIndex - 1).Wrap(party.Count);
				input = party[currentCharacterIndex];
				ChangeCharacter();
			}
			else if (GameInputs.menuRight2.AbsorbPress(0.3f))
			{
				currentCharacterIndex = (currentCharacterIndex + 1).Wrap(party.Count);
				input = party[currentCharacterIndex];
				ChangeCharacter();
			}


			// Handle no card being selected (e.g. when player clicks elsewhere on the screen)
			// We start back from the currently equipped limb
			if (GameInputs.menuNavigate.AnyPressed &&
			    EventSystem.current.currentSelectedGameObject == null)
			{
				_equippedCard.Select();

				MoveDirection move_dir = MoveDirection.None;

				if (GameInputs.menuNavigate.left.IsPressed) move_dir = MoveDirection.Left;
				if (GameInputs.menuNavigate.right.IsPressed) move_dir = MoveDirection.Right;
				if (GameInputs.menuNavigate.up.IsPressed) move_dir = MoveDirection.Up;
				if (GameInputs.menuNavigate.down.IsPressed) move_dir = MoveDirection.Down;

				ExecuteEvents.Execute(
					_equippedCard.gameObject,
					new AxisEventData(EventSystem.current) {moveDir = move_dir},
					ExecuteEvents.moveHandler);
			}

			if (GameInputs.confirm.IsPressed)
			{
				if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out LimbCard card))
				{
					EquipLimb(card);
				}
			}

			if (GameInputs.favoriteItem.IsPressed)
			{
				if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out LimbCard card))
				{
					ToggleLimbFavorability(card);
				}
			}

			if (GameInputs.detachItem.IsPressed)
			{
				ToggleFavoriteViewing();
			}

#if UNITY_EDITOR
			// DEBUG
			//------------------------------------------------
			if (GameInputs.IsPressed(Key.R))
			{
				_selectedCharacter[LimbType.Head] = _ownedHeads.Choose();
				_selectedCharacter[LimbType.Body] = _ownedBodies.Choose();
				_selectedCharacter[LimbType.Arm1] = _ownedArms1.Choose();
				_selectedCharacter[LimbType.Arm2] = _ownedArms2.Choose();

				UI_NanokinPanel.OnLimbChange();
				SaveManager.SaveCurrent(devWrite: true);
			}
#endif
			DoExitControls();
		}
	}

	private void RenameMonster(string newName)
	{
		_selectedCharacter.NanokinName = newName;

		if (newName.Length > 16)
		{
			_selectedCharacter.NanokinName = newName.Substring(0, 16);
		}

		RefreshUI();
	}
	public void ReceiveInput(string s)
	{
		RenameMonster(s);
	}

	public void CloseWithoutInput()
	{
		RefreshUI();
	}

	public string GetDefault() => _selectedCharacter.Body.Asset.DisplayName;
}
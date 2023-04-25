using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anjin.Core.Flags;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Combat;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Overworld.UI;
using Puppets.Render;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityUtilities;
using Util.Addressable;
using Util.Odin.Attributes;
using Util.UniTween.Value;
using MenuController = Anjin.Nanokin.MenuManager;
using DG.Tweening;
using TMPro;

namespace Menu.Sticker
{
	public enum EquipMenuState
	{
		GridSelect,
		CellSelect,
		StickerSelect,
		PlacementSelect
	}

	public enum UseMenuState
	{
		InventorySelect,
		StickerSelect,
		TargetSelect
	}

	/// <summary>
	/// The driver of the sticker menu in nanokin.
	/// </summary>
	public class StickerMenu : StaticMenu<StickerMenu>
	{
		public delegate void             StickerMenuEvent(params object[] args);
		public          StickerMenuEvent OnEquipMenuModeChanged;

		private class StickerDisplayInfo
		{
			public int                   TotalOwned   { get; set; }
			public string                DisplayName  { get; set; }
			public Sprite                DisplayImage { get; set; }
			public StickerAsset          Asset        { get; set; }
			public List<StickerInstance> Instances    { get; set; }
		}

		public static List<CharacterEntry> inputCharacters = null;
		public static CharacterEntry       inputCharacter  = null;

		private const int TAB_USE       = 0;
		private const int TAB_EQUIP     = 1;
		private const int TAB_FAVORITES = 2;

		[Title("References")]
		[SerializeField] private StickerEditor Editor;
		[SerializeField] private SplicerTabBar TabBar;
		[SerializeField] private StickerList   StickerList;
		[FormerlySerializedAs("GridPrefab"), SerializeField]
		private StickerGrid MonsterEditGridPrefab;
		[SerializeField] private PartyMemberColumn MonsterUseColumnPrefab;
		[SerializeField] private RectTransform     GridRoot;
		[SerializeField] private RectTransform     ColumnRoot;
		[SerializeField] private RectTransform     LoadingPane;
		[FormerlySerializedAs("DescriptionDisplay"), SerializeField]
		private StickerInfoUI DescriptionUI;
		[SerializeField] private NanokinInfoUI NanokinUI;

		[SerializeField] private GameObject TutorialScreen;
		[SerializeField] private GameObject GridSelectPromptContainer;
		[SerializeField] private GameObject CellSelectPromptContainer;
		[SerializeField] private GameObject StickerSelectPromptContainer;
		[SerializeField] private GameObject PlacementSelectPromptContainer;
		[SerializeField] private GameObject SelectGridDisplay;
		[SerializeField] private GameObject DeselectGridDisplay;
		[SerializeField] private GameObject ToggleEquipModeDisplay;
		[SerializeField] private GameObject DetachStickerDisplay;

		[SerializeField] private List<EquipMenuPrompt> equipMenuPrompts;

		[SerializeField] private InputButtonLabel LeftGridSelectIcon;
		[SerializeField] private InputButtonLabel RightGridSelectIcon;
		[SerializeField] private InputButtonLabel SelectGridIcon;
		[SerializeField] private InputButtonLabel DeselectGridIcon;
		[SerializeField] private InputButtonLabel ToggleEquipModeIcon;
		[SerializeField] private InputButtonLabel DetachStickerIcon;
		[SerializeField] private InputButtonLabel ToggleStickerFavoriteIcon;
		[SerializeField] private InputButtonLabel LeftStickerRotationIcon;
		[SerializeField] private InputButtonLabel RightStickerRotationIcon;

		[SerializeField] private TMP_Text EquipMenuPrompt;
		[SerializeField] private TMP_Text UseMenuPrompt;

		[SerializeField] private TMP_Text ToggleEquipModePrompt;
		[SerializeField] private TMP_Text ToggleFavoritePrompt;

		public Button BackButton;

		[Title("Animation")]
		[NonSerialized, OdinSerialize] private Vector2TweenTo ListIn;
		[NonSerialized, OdinSerialize] private Vector2TweenTo ListOut;
		[NonSerialized, OdinSerialize] private FloatTween     DescriptionIn;
		[NonSerialized, OdinSerialize] private FloatTween     DescriptionOut;
		[SerializeField, Range01]      private float          ScrollingSpeed;

		[Title("Sounds")]
		[SerializeField] private AudioDef AdInvalidNavigation;
		[SerializeField] private AudioDef AdSelectListEntry;
		[SerializeField] private AudioDef AdSelectListEntryPrevious;
		[SerializeField] private AudioDef AdReturnToList;
		[SerializeField] private AudioDef AdRemoveSticker;
		[SerializeField] private AudioDef SFX_OpenSubmenu;
		[SerializeField] private AudioDef SFX_ExitSubmenu;

		[SerializeField] private Color SelectedBustColor;
		[SerializeField] private Color UnselectedBustColor;
		[SerializeField] private Color SelectedPanelColor;
		[SerializeField] private Color UnselectedPanelColor;

		private bool                                 _loading;
		private AsyncHandles                         _handles;
		private List<Character>                      _characters;
		private List<Image>                          _columnPanels;
		private Dictionary<Character, List<Tweener>> _bustHighlightTweeners;
		private List<Tweener>                        _panelHighlightTweeners;
		private Dictionary<Character, List<Image>>   _bustImages;

		//private List<StickerInstance> _inventory;
		private bool _lockedCharacterForUse;

		private CanvasGroup _columnGroup;
		private int         _activeUseCharacter;
		private int         _activeUseListCharacter;
		private int         _activeEquipCharacter;

		private int _currentEquipListSticker;
		private int _currentUseListSticker;

		private ListSticker _useSticker;

		private States _state;

		private TweenableVector2        _listPosition;
		private TweenableFloat          _descriptionPosition;
		private Vector2                 _gridRootScrolling;
		private CancellationTokenSource _ctsSelectGrid;
		private ListSticker             _lastSelectedListEntry;

		private Character ActiveEquipCharacter   => _characters[_activeEquipCharacter];
		private Character ActiveUseCharacter     => _characters[_activeUseCharacter];
		private Character ActiveUseListCharacter => _characters[_activeUseCharacter];

		private Dictionary<string, StickerDisplayInfo> _stickerDatabase;

		private Dictionary<string, ListSticker> _listEntries;

		private EquipMenuState previousEquipMenuState;
		private EquipMenuState currentEquipMenuState;

		private UseMenuState currentUseMenuState;

		private bool viewingFavorites;

		private GridSticker browsedGridSticker;

		protected override void OnAwake()
		{
			viewingFavorites = false;

			browsedGridSticker = null;

			_currentEquipListSticker = 0;
			_currentUseListSticker   = 0;
			_activeEquipCharacter    = -1;
			_activeUseCharacter      = 0;
			_activeUseListCharacter  = 0;
			_lockedCharacterForUse   = false;
			_characters              = new List<Character>();
			//_inventory           = new List<StickerInstance>();
			_listPosition           = new TweenableVector2();
			_descriptionPosition    = new TweenableFloat();
			_stickerDatabase        = new Dictionary<string, StickerDisplayInfo>();
			_listEntries            = new Dictionary<string, ListSticker>();
			_bustImages             = new Dictionary<Character, List<Image>>();
			_useSticker             = null;
			_columnPanels           = new List<Image>();
			_panelHighlightTweeners = new List<Tweener>();
			_bustHighlightTweeners  = new Dictionary<Character, List<Tweener>>();

			_handles = new AsyncHandles();

			LeftGridSelectIcon.Button        = GameInputs.menuLeft2;
			RightGridSelectIcon.Button       = GameInputs.menuRight2;
			SelectGridIcon.Button            = GameInputs.confirm;
			DeselectGridIcon.Button          = GameInputs.cancel;
			ToggleEquipModeIcon.Button       = GameInputs.toggleMode;
			DetachStickerIcon.Button         = GameInputs.detachItem;
			ToggleStickerFavoriteIcon.Button = GameInputs.favoriteItem;
			LeftStickerRotationIcon.Button   = GameInputs.menuLeft;
			RightStickerRotationIcon.Button  = GameInputs.menuRight;

			TabBar.SelectionChanged += OnTabSelect;

			Editor.onStickerDetached    += OnStickerDetached;
			Editor.onPlacementConfirmed += OnStickerPlaced;
			Editor.onPlacementCanceled  += OnPlacementCanceled;

			BackButton.onClick.AddListener(OnExitStatic);

			for (int i = 0; i < equipMenuPrompts.Count; i++)
			{
				OnEquipMenuModeChanged += equipMenuPrompts[i].ToggleActivity;
			}
		}

		private void OnDestroy()
		{
			//_ctsSelectGrid?.Dispose();
			for (int i = 0; i < equipMenuPrompts.Count; i++)
			{
				if (equipMenuPrompts[i] != null)
				{
					OnEquipMenuModeChanged -= equipMenuPrompts[i].ToggleActivity;
				}
			}
		}

		protected override async UniTask enableMenu()
		{
			GameInputs.mouseUnlocks.Add("sticker_menu");

			_loading = true;
			var allTasks = new List<UniTask>();

			SaveData save = await SaveManager.GetCurrentAsync();

			inputCharacters = inputCharacters ?? save.Party;
			inputCharacter  = inputCharacters[0] ?? inputCharacters[0];

			// Activate everything so any instantiated objs get awake called
			ColumnRoot.gameObject.SetActive(true);
			GridRoot.gameObject.SetActive(true);
			NanokinUI.gameObject.SetActive(true);

			// Load character mirrors for use in the menu
			foreach (CharacterEntry kid in inputCharacters)
			{
				Character character = new Character { entry = kid };
				_characters.Add(character);
			}

			_columnGroup              = ColumnRoot.GetOrAddComponent<CanvasGroup>();
			_columnGroup.interactable = false;

			List<GridSticker> gridStickers = new List<GridSticker>();

			for (int i = 0; i < _characters.Count; i++)
			{
				Character character = _characters[i];

				//gridStickers.Clear();

				// Create column (use tab)
				// ----------------------------------------
				PartyMemberColumn column = Instantiate(MonsterUseColumnPrefab, ColumnRoot);
				column.onPointerDown  += DetermineClickAction;
				column.onPointerEnter += DetermineHoverAction;

				Image columnPanel = column.TMP_NanokinName.transform.parent.parent.GetComponent<Image>();
				columnPanel.color = UnselectedPanelColor;
				_columnPanels.Add(columnPanel);

				allTasks.Add(column.SetCharacter(character.entry));

				character.column = column;

				if (!_bustImages.ContainsKey(character))
				{
					CharacterBust bust   = character.column.gameObject.GetComponentInChildren<CharacterBust>();
					List<Image>   images = bust.gameObject.GetComponentsInChildren<Image>().ToList();

					for (int j = 0; j < images.Count; j++)
					{
						images[j].color = UnselectedBustColor;
					}

					_bustImages.Add(character, images);

					if (!_bustHighlightTweeners.ContainsKey(character))
					{
						_bustHighlightTweeners.Add(character, new List<Tweener>());

						foreach (Image image in images)
						{
							_bustHighlightTweeners[character].Add(null);
						}
					}
				}

				_panelHighlightTweeners.Add(null);


				// Create grid (equip tab)
				// ----------------------------------------
				StickerGrid grid = Instantiate(MonsterEditGridPrefab, GridRoot);

				grid.ID = i;

				grid.Dimensions.x = character.entry.GridWidth;
				grid.Dimensions.y = character.entry.GridHeight;
				grid.RefreshLayout();

				//grid.CellReticle.gameObject.SetActive(true);
				grid.CellReticle.anchoredPosition = grid.GetCellPosition(Vector2.zero);

				grid.ContentGroup.interactable   = false;
				grid.ContentGroup.blocksRaycasts = false;
				grid.CellGroup.interactable      = false;
				grid.CellGroup.blocksRaycasts    = false;

				grid.onCellHoverChanged += OnGridCellHovered;
				grid.onGridSelected     += OnGridClicked;

				grid.SetPortrait(character.entry.asset).Forget();

				character.grid = grid;

				foreach (StickerInstance instance in character.entry.Stickers)
				{
					GameObject obj = PrefabPool.Rent(grid.StickerPrefab);

					GridSticker sticker = obj.GetComponent<GridSticker>();
					sticker.cellSize   = grid.CellSize + grid.CellPadding;
					sticker.coordinate = new Vector2Int(instance.Entry.X, instance.Entry.Y);
					sticker.rotation   = instance.Entry.Rotation;

					allTasks.Add(sticker.SetSticker(instance));

					grid.AddSticker(sticker);

					gridStickers.Add(sticker);
					// LoadStats(sticker.asset);
				}
			}

			await UniTask.WhenAll(allTasks);

			//Load equipped stickers into the inventory, but don't add them to the running list of unused stickers
			foreach (GridSticker gridSticker in gridStickers)
			{
				string displayName = gridSticker.instance.Asset.DisplayName;

				if (!_stickerDatabase.ContainsKey(displayName))
				{
					StickerDisplayInfo info = new StickerDisplayInfo();
					info.DisplayName  = displayName;
					info.DisplayImage = gridSticker.Image.sprite;
					info.Asset        = gridSticker.asset;
					info.Instances    = new List<StickerInstance>(9);
					info.TotalOwned   = 1;

					_stickerDatabase.Add(displayName, info);
				}
			}

			// Load unequipped stickers in the inventory
			foreach (StickerEntry entry in save.Stickers)
			{
				string displayName = entry.instance.Asset.DisplayName;

				if (!_stickerDatabase.ContainsKey(displayName))
				{
					StickerDisplayInfo info = new StickerDisplayInfo();
					info.DisplayName = displayName;

					AsyncOperationHandle<Sprite> _spriteHandle = await Addressables2.LoadHandleAsync(entry.instance.Asset.Sprite);
					info.DisplayImage = _spriteHandle.Result;

					info.Instances = new List<StickerInstance>(9) { entry.instance };

					info.Asset = entry.instance.Asset;

					info.TotalOwned = 1;

					_stickerDatabase.Add(displayName, info);
				}
				else
				{
					_stickerDatabase[displayName].Instances.Add(entry.instance);
					++_stickerDatabase[displayName].TotalOwned;
				}
			}

			//Sort stickers by their charges left (descending order so that the auto-pick is always the first entry)
			foreach (var info in _stickerDatabase.Values)
			{
				info.Instances.Sort((x, y) => y.Charges.CompareTo(x.Charges));
			}

			TabBar.Select(TAB_EQUIP);

			SetEquipMenuState(EquipMenuState.GridSelect);

			//SelectGridDisplay.SetActive(true);
			//DeselectGridDisplay.SetActive(false);
			//ToggleEquipModeDisplay.SetActive(false);

			_listPosition.Set(ListIn);
			_descriptionPosition.Set(DescriptionIn);

			ChangeState(States.Regular);

			SelectGrid(inputCharacters.IndexOf(inputCharacter));

			_loading = false;

			if (!Flags.GetBool("tut_sticker_menu"))
			{
				Flags.SetBool("tut_sticker_menu", true);
				//TutorialScreen.SetActive(true);
				ShowSplashScreen("SplashScreens/tutorial_stickers").Forget();
			}
		}

		public void ToggleGridSelectors()
		{
			foreach (var character in _characters)
			{
				character.grid.OnGridLockToggled?.Invoke(Editor.GridLocked);
			}
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

		private void OnTabSelect(int prev, int next)
		{
			if (prev == TAB_USE)
			{
				UseMenuPrompt.gameObject.SetActive(false);

				if (_useSticker != null)
				{
					StopStickerUse();
				}

				for (int i = 0; i < _characters.Count; i++)
				{
					Character   character  = _characters[i];
					List<Image> bustImages = _bustImages[character];

					for (int j = 0; j < bustImages.Count; j++)
					{
						Image bustImage = bustImages[j];

						if (_bustHighlightTweeners[character][j] != null)
						{
							_bustHighlightTweeners[character][j].Kill();
							_bustHighlightTweeners[character][j] = null;
						}

						bustImage.color = UnselectedBustColor;
					}

					if (_panelHighlightTweeners[i] != null)
					{
						_panelHighlightTweeners[i].Kill();
						_panelHighlightTweeners[i] = null;
					}

					_columnPanels[i].color = UnselectedPanelColor;
				}

				OverworldNotifyUI.InstantlyHideNotificationPopup();

				_lockedCharacterForUse = false;
			}
			//else if (prev == TAB_EQUIP)
			//{
			//	if (Editor.IsPlacing)
			//	{
			//		Editor.CancelPlacement();
			//	}
			//}

			if ((next == TAB_EQUIP) || (next == TAB_FAVORITES))
			{
				SelectGrid(_activeEquipCharacter);
			}
			else if (next == TAB_USE)
			{
				UseMenuPrompt.gameObject.SetActive(true);

				currentUseMenuState = UseMenuState.InventorySelect;

				for (int i = 0; i < _characters.Count; i++)
				{
					Character character = _characters[i];

					if (ActiveUseCharacter != character)
					{
						for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
						{
							if (_bustHighlightTweeners[character][j] != null)
							{
								_bustHighlightTweeners[character][j].Kill();
								_bustHighlightTweeners[character][j] = null;
							}
						}

						foreach (Image image in _bustImages[character])
						{
							image.color = UnselectedBustColor;
						}
					}
					else
					{
						List<Image> bustImage = _bustImages[character];

						for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
						{
							if (_bustHighlightTweeners[character][j] == null)
							{
								_bustHighlightTweeners[character][j] = bustImage[j].DOColor(SelectedBustColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
							}
							//image.color = SelectedBustColor;
						}
					}

					_columnPanels[i].color = UnselectedPanelColor;
				}
			}

			ColumnRoot.gameObject.SetActive(next == TAB_USE);
			GridRoot.gameObject.SetActive(((next == TAB_EQUIP) || (next == TAB_FAVORITES)));
			NanokinUI.gameObject.SetActive(((next == TAB_EQUIP) || (next == TAB_FAVORITES)));

			RefreshList();
			RefreshUseStickerUI();
		}

	#region Placement Events

		private void AddStickerBackToCollection([NotNull] GridSticker gsticker)
		{
			string stickerName = gsticker.instance.Asset.DisplayName;

			if (_stickerDatabase.ContainsKey(stickerName))
			{
				StickerDisplayInfo info      = _stickerDatabase[stickerName];
				var                inventory = SaveManager.current.Stickers.FindAll(x => x.Asset.DisplayName == gsticker.asset.DisplayName);

				if ((inventory.Count == 0) || (inventory.Count + 1 <= info.TotalOwned))
				{
					SaveManager.current.Stickers.Add(gsticker.instance.Entry);
				}

				if (!_listEntries.ContainsKey(stickerName))
				{
					ListSticker listEntry = AddListSticker();
					listEntry.SetSticker(gsticker.instance).Forget();
					//UGUI.Select(listEntry, true);
					_listEntries.Add(stickerName, listEntry);
				}

				info.Instances.Add(gsticker.instance);
				info.Instances.Sort((x, y) => y.Charges.CompareTo(x.Charges));

				_listEntries[stickerName].Charges.text = $"x{info.Instances.Count}";

				PreviewNone();

				Debug.Log("Placement Cancelled: " + gsticker);
				// Add the sticker back into the list.

				StickerList.Sort();
			}
		}

		private void OnPlacementCanceled([NotNull] GridSticker gsticker)
		{
			AddStickerBackToCollection(gsticker);

			SetEquipMenuState(previousEquipMenuState);
		}

		private void OnGridClicked(int ID)
		{
			SelectGrid(ID);
			Editor.ToggleGridLock(true);
			SetEquipMenuState(EquipMenuState.CellSelect);
		}

		private void OnStickerRemoved([NotNull] GridSticker gsticker)
		{
			Debug.Log("Detach: " + gsticker);

			// Add the sticker back into the list.
			//_inventory.Add(gsticker.instance);
			string stickerName = gsticker.instance.Asset.DisplayName;

			gsticker.Image.raycastTarget = false;

			//StickerDisplayInfo info = _stickerDatabase[stickerName];
			//info.Instances.Add(gsticker.instance);
			//info.Instances.Sort((x, y) => y.Charges.CompareTo(x.Charges));

			ActiveEquipCharacter.entry.RemoveSticker(gsticker.instance);
			NanokinUI.RefreshUI();
		}

		private void OnStickerDetached([NotNull] GridSticker gsticker)
		{
			OnStickerRemoved(gsticker);

			SetEquipMenuState(EquipMenuState.PlacementSelect);
		}

		private void OnStickerPlaced([NotNull] GridSticker sticker)
		{
			//_inventory.Remove(sticker.instance); // Remove from the inventory
			string stickerName = sticker.instance.Asset.DisplayName;

			if (_stickerDatabase.ContainsKey(stickerName))
			{
				Debug.Log("Placed: " + sticker);

				NanokinUI.CompareNone();

				var found = SaveManager.current.Stickers.Find(x => x == sticker.instance.Entry);

				if (found != null)
				{
					SaveManager.current.Stickers.Remove(sticker.instance.Entry);

					StickerDisplayInfo info = _stickerDatabase[stickerName];

					if (_listEntries.ContainsKey(stickerName) && (info.Instances.Count == 0))
					{
						StickerList.Remove(_listEntries[stickerName]);
						_listEntries.Remove(stickerName);
					}
				}

				StickerEntry entry = sticker.instance.Entry;
				// ReSharper disable once PossibleInvalidOperationException
				entry.X        = sticker.coordinate.Value.x;
				entry.Y        = sticker.coordinate.Value.y;
				entry.Rotation = sticker.rotation;

				sticker.Image.raycastTarget = true;

				ActiveEquipCharacter.entry.AddSticker(sticker.instance);
				NanokinUI.RefreshUI();

				//UGUI.Select(sticker);
			}

			SetEquipMenuState(previousEquipMenuState);
		}

	#endregion

	#region Grid Cell Events

		private void OnGridCellHovered([CanBeNull] GridCell cell)
		{
			if (cell != null)
			{
				GridSticker sticker = ActiveEquipCharacter.grid.GetStickerAt(cell.coordinate);
				if (sticker != null)
				{
					DescriptionUI.ChangeSticker(sticker.instance);
				}
			}
			else
			{
				DescriptionUI.SetNone();
			}
		}

	#endregion

	#region List Events

		private void OnListStickerPointerEnter([NotNull] ListSticker sticker)
		{
			if ((_useSticker == null) && !Editor.IsPlacing)
			{
				PreviewSticker(sticker.instance);
			}
		}

		private void OnListStickerPointerExit([NotNull] ListSticker sticker)
		{
			if (Editor.IsPlacing)
				// We want to display the info for the sticker being placed
				return;

			NanokinUI.CompareNone();
			DescriptionUI.SetNone();
		}

		private void OnListStickerClicked(ListSticker sticker)
		{
			switch (TabBar.selectedIndex)
			{
				case TAB_USE:
					StartStickerUse(sticker);

					currentUseMenuState = UseMenuState.TargetSelect;

					RefreshUseStickerUI();

					break;

				case TAB_EQUIP:
				case TAB_FAVORITES:
					if (Editor.StartPlacementNew(out GridSticker gsticker))
					{
						if (!Editor.GridLocked)
						{
							Editor.ToggleGridLock(true);
						}

						SetEquipMenuState(EquipMenuState.PlacementSelect);

						Assert.IsNotNull(gsticker, nameof(gsticker) + " != null");
						gsticker.SetSticker(sticker.instance).Forget();

						_stickerDatabase[sticker.Label.text].Instances.Remove(sticker.instance);
						//RemoveListSticker(_listEntries[sticker.Label.text]);
						RefreshList();

						//StickerList.Remove(sticker);
					}

					break;
			}
		}

		private void OnListStickerSelected(ListSticker lsticker)
		{
			if (Editor.IsPlacing)
				// We want to display the info for the sticker being placed
				return;

			PreviewSticker(lsticker.instance);
			UGUI.ScrollTo(lsticker);
		}

		public void OnGridSectionHovered(BaseEventData eventData)
		{
			if ((TabBar.selectedIndex == TAB_EQUIP) && (currentEquipMenuState != EquipMenuState.GridSelect) && (currentEquipMenuState != EquipMenuState.PlacementSelect))
			{
				SetEquipMenuState(EquipMenuState.CellSelect);
			}
		}

		public void OnStickerSectionHovered(BaseEventData eventData)
		{
			if ((TabBar.selectedIndex == TAB_EQUIP) && (currentEquipMenuState != EquipMenuState.GridSelect) && (currentEquipMenuState != EquipMenuState.PlacementSelect))
			{
				SetEquipMenuState(EquipMenuState.StickerSelect);
			}
		}

	#endregion

	#region State
		private void SetEquipMenuState(EquipMenuState state)
		{
			previousEquipMenuState = currentEquipMenuState;
			currentEquipMenuState  = state;

			OnEquipMenuModeChanged?.Invoke(currentEquipMenuState);

			if (Editor.GridLocked)
			{

			}

			switch (state)
			{
				case EquipMenuState.GridSelect:
					EquipMenuPrompt.text = "Select grid";

					foreach (Character character in _characters)
					{
						if (character.grid != null)
						{
							character.grid.CellReticle.gameObject.SetActive(false);
						}
					}

					foreach (var sticker in _listEntries.Values)
					{
						sticker.interactable = false;

						sticker.onPointerClick = null;
						sticker.onPointerEnter = null;
						sticker.onPointerExit  = null;
						sticker.onSelected     = null;
						sticker.navigation     = new Navigation { mode = Navigation.Mode.None };

						sticker.OnDeselect(null);
					}

					DetachStickerDisplay.SetActive(false);

					break;
				case EquipMenuState.CellSelect:
					EquipMenuPrompt.text       = "Edit equipped stickers";
					ToggleEquipModePrompt.text = "Browse Stickers";

					ActiveEquipCharacter.grid.CellReticle.gameObject.SetActive(true);

					foreach (var sticker in _listEntries.Values)
					{
						sticker.interactable = true;

						sticker.onPointerClick = OnListStickerClicked;
						sticker.onPointerEnter = OnListStickerPointerEnter;
						sticker.onPointerExit  = null;
						sticker.onSelected     = OnListStickerSelected;
						sticker.navigation     = new Navigation { mode = Navigation.Mode.None };

						sticker.OnDeselect(null);
					}

					DetachStickerDisplay.SetActive(Editor.HasStickerAt(Editor.CurrentCellSelection, out _));

					break;
				case EquipMenuState.StickerSelect:
					EquipMenuPrompt.text       = "Select new sticker";
					ToggleEquipModePrompt.text = "Browse Grid";

					ActiveEquipCharacter.grid.CellReticle.gameObject.SetActive(false);

					foreach (var sticker in _listEntries.Values)
					{
						sticker.interactable = true;

						sticker.onPointerClick = OnListStickerClicked;
						sticker.onPointerEnter = OnListStickerPointerEnter;
						sticker.onPointerExit  = null;
						sticker.onSelected     = OnListStickerSelected;
						sticker.navigation     = new Navigation { mode = Navigation.Mode.Vertical };
					}

					StickerList.Select(_currentEquipListSticker);

					DetachStickerDisplay.SetActive(false);

					break;
				case EquipMenuState.PlacementSelect:
					EquipMenuPrompt.text = "Place sticker";

					ActiveEquipCharacter.grid.CellReticle.gameObject.SetActive(false);

					foreach (var sticker in _listEntries.Values)
					{
						sticker.interactable = false;

						sticker.onPointerClick = null;
						sticker.onPointerEnter = null;
						sticker.onPointerExit  = null;
						sticker.onSelected     = null;
						sticker.navigation     = new Navigation { mode = Navigation.Mode.None };

						sticker.OnDeselect(null);
					}

					DetachStickerDisplay.SetActive(false);

					break;
			}

			//GridSelectPromptContainer.SetActive(currentEquipMenuState == EquipMenuState.GridSelect);
			//CellSelectPromptContainer.SetActive(currentEquipMenuState == EquipMenuState.CellSelect);
			//StickerSelectPromptContainer.SetActive(currentEquipMenuState == EquipMenuState.StickerSelect);
			//PlacementSelectPromptContainer.SetActive(currentEquipMenuState == EquipMenuState.PlacementSelect);
		}
		void RefreshUseStickerUI()
		{
			switch (currentUseMenuState)
			{
				case UseMenuState.InventorySelect:
					UseMenuPrompt.text = "Select inventory";

					for (int i = 0; i < _panelHighlightTweeners.Count; i++)
					{
						if (_panelHighlightTweeners[i] != null)
						{
							_panelHighlightTweeners[i].Kill();
							_panelHighlightTweeners[i] = null;
						}

						_columnPanels[i].color = UnselectedPanelColor;
					}

					break;
				case UseMenuState.StickerSelect:
					UseMenuPrompt.text = "Select sticker";

					break;
				case UseMenuState.TargetSelect:
					UseMenuPrompt.text = "Select target";

					for (int i = 0; i < _panelHighlightTweeners.Count; i++)
					{
						if (i != _activeUseListCharacter)
						{
							if (_panelHighlightTweeners[i] != null)
							{
								_panelHighlightTweeners[i].Kill();
								_panelHighlightTweeners[i] = null;
							}

							_columnPanels[i].color = UnselectedPanelColor;
						}
						else
						{
							//_columnPanels[i].color = SelectedPanelColor;
							if (_panelHighlightTweeners[i] == null)
							{
								_panelHighlightTweeners[i] = _columnPanels[i].DOColor(SelectedPanelColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
							}
						}
					}

					break;
			}
		}

		private void SelectGrid(int index)
		{
			//_ctsSelectGrid?.Cancel();
			//_ctsSelectGrid = new CancellationTokenSource();

			foreach (var character in _characters)
			{
				character.grid.RefreshCells();
			}

			if (_activeEquipCharacter != index)
			{
				// LEAVE PREV GRID
				// ----------------------------------------
				if (_characters.ContainsIndex(_activeEquipCharacter))
				{
					StickerGrid previousGrid = ActiveEquipCharacter.grid;
					previousGrid.SetState(false);
					previousGrid.CellGroup.interactable      = false;
					previousGrid.CellGroup.blocksRaycasts    = false;
					previousGrid.ContentGroup.interactable   = false;
					previousGrid.ContentGroup.blocksRaycasts = false;
				}

				_activeEquipCharacter = index.Clamp(_characters);

				// ENTER NEXT GRID
				// ----------------------------------------
				if (_characters.ContainsIndex(_activeEquipCharacter))
				{
					CharacterEntry mirror = ActiveEquipCharacter.entry;
					StickerGrid    grid   = ActiveEquipCharacter.grid;

					Editor.SetGrid(grid);
					grid.SetState(true);

					NanokinUI.ChangeCharacter(mirror);
					NanokinUI.ChangeMonster(mirror.nanokin);

					//_ctsSelectGrid.Token.ThrowIfCancellationRequested();

					if (!Editor.IsPlacing)
					{
						if (TabBar.selectedIndex == TAB_USE)
						{
							// Usable items will have changed
							RefreshList();
						}

						grid.CellGroup.interactable      = false;
						grid.CellGroup.blocksRaycasts    = false;
						grid.ContentGroup.interactable   = true;
						grid.ContentGroup.blocksRaycasts = true;
					}
					else
					{
						grid.CellGroup.interactable      = true;
						grid.CellGroup.blocksRaycasts    = true;
						grid.ContentGroup.interactable   = false;
						grid.ContentGroup.blocksRaycasts = false;

						//if (grid.FindFirstFreeCell(out GridCell cell))
						//{
						//	Editor.Select(cell);
						//	previousGrid.RefreshCells();
						//}
					}

					if (grid != null)
					{
						grid.RefreshCells();
					}

					Editor.RefreshLayout();
				}
			}
		}

		// List
		// ----------------------------------------
		private void RefreshList()
		{
			StickerList.Clear();
			_listEntries.Clear();

			switch (TabBar.selectedIndex)
			{
				case TAB_USE:
					_loading = true;

					//foreach (Character grid in _characters)
					foreach (GridSticker sticker in ActiveUseCharacter.grid.stickers)
					{
						StickerAsset asset = sticker.asset;
						if (!asset.IsConsumable) continue;

						string stickerName = sticker.instance.Asset.DisplayName;

						ListSticker listEntry = AddListSticker();
						listEntry.SetSticker(sticker.instance).Forget();
						listEntry.interactable = ((listEntry.instance.Charges > 0) || !listEntry.instance.IsConsumable);
						listEntry.Charges.text = $"{listEntry.instance.Charges}/{listEntry.instance.MaxCharges}";
						_listEntries.Add(stickerName, listEntry);
					}

					_loading = false;
					break;

				case TAB_EQUIP:
					foreach (var info in _stickerDatabase.Values)
					{
						if (info.Instances.Count > 0)
						{
							ListSticker listEntry = AddListSticker();
							listEntry.SetSticker(info.Instances[0]).Forget();
							listEntry.interactable = ((listEntry.instance.Charges > 0) || !listEntry.instance.IsConsumable);
							listEntry.Charges.text = $"x{info.Instances.Count.ToString()}";
							listEntry.FavoriteIcon.SetActive(listEntry.Favorited);
							_listEntries.Add(listEntry.asset.DisplayName, listEntry);
						}
					}

					break;

				case TAB_FAVORITES:
					foreach (var info in _stickerDatabase.Values)
					{
						if ((info.Instances.Count > 0) && (info.Instances[0].Favorited))
						{
							ListSticker listEntry = AddListSticker();
							listEntry.SetSticker(info.Instances[0]).Forget();
							listEntry.interactable = ((listEntry.instance.Charges > 0) || !listEntry.instance.IsConsumable);
							listEntry.Charges.text = $"x{info.Instances.Count.ToString()}";
							_listEntries.Add(listEntry.asset.DisplayName, listEntry);
						}
					}

					break;
			}

			StickerList.Sort();

			//if (StickerList.ContentRoot.transform.childCount > 0)
			//{
			//	SelectFirstListSticker();
			//}
		}

		private ListSticker AddListSticker()
		{
			ListSticker lsticker = StickerList.Add();

			lsticker.onPointerClick = OnListStickerClicked;
			lsticker.onPointerEnter = OnListStickerPointerEnter;
			lsticker.onPointerExit  = OnListStickerPointerExit;
			lsticker.onSelected     = OnListStickerSelected;
			//lsticker.navigation     = new Navigation { mode = Navigation.Mode.Vertical };

			return lsticker;
		}

		private void RemoveListSticker(ListSticker lsticker)
		{
			StickerList.Remove(lsticker);
		}

		private void SelectFirstListSticker()
		{
			if (StickerList.ContentRoot.childCount > 0)
			{
				Transform first = StickerList.ContentRoot.transform.GetChild(0);
				UGUI.Select(first, true);
			}
			// GameSFX.PlayGlobal(AdSelectListEntry);
		}

		// USE
		// ----------------------------------------
		private void StartStickerUse([NotNull] ListSticker sticker)
		{
			//StickerDisplayInfo info = _stickerDatabase[sticker.Label.text];

			if (sticker.instance.Charges > 0)
			{
				//Get the entry with the most charges left
				//sticker.instance = info.Instances[0];

				//if (sticker.instance.Charges <= 0)
				//{
				//	Debug.Log("Cannot use the sticker since it doesn't have any charges left!");
				//	return;
				//}

				_useSticker               = sticker;
				_columnGroup.interactable = true;
				EventSystem.current.SetSelectedGameObject(_characters[0].column.gameObject);
			}
			else
			{
				Debug.Log("No available instances of this sticker to use.");
			}
		}

		private void DetermineClickAction([NotNull] PartyMemberColumn column)
		{
			if (TabBar.selectedIndex == (TAB_USE))
			{
				if (_useSticker != null)
				{
					ConfirmStickerUse(column);
				}
				else
				{
					_lockedCharacterForUse = true;

					List<Image> bustImages = _bustImages[ActiveUseCharacter];

					currentUseMenuState = UseMenuState.StickerSelect;

					for (int i = 0; i < _bustHighlightTweeners[ActiveUseCharacter].Count; i++)
					{
						if (_bustHighlightTweeners[ActiveUseCharacter][i] != null)
						{
							_bustHighlightTweeners[ActiveUseCharacter][i].Kill();
							_bustHighlightTweeners[ActiveUseCharacter][i] = null;
						}

						bustImages[i].color = SelectedBustColor;
					}

					//Character character = _characters.Find(x => x.entry == column.character);
					//_activeUseCharacter = _characters.IndexOf(character);
					//RefreshList();
					RefreshUseStickerUI();
				}
			}
		}

		private void DetermineHoverAction([NotNull] PartyMemberColumn column)
		{
			if (TabBar.selectedIndex == (TAB_USE))
			{
				if (_useSticker != null)
				{
					Character character = _characters.Find(x => x.entry == column.character);
					_activeUseListCharacter = _characters.IndexOf(character);

					for (int i = 0; i < _panelHighlightTweeners.Count; i++)
					{
						if (i != _activeUseListCharacter)
						{
							if (_panelHighlightTweeners[i] != null)
							{
								_panelHighlightTweeners[i].Kill();
								_panelHighlightTweeners[i] = null;
							}

							_columnPanels[i].color = UnselectedPanelColor;
						}
						else
						{
							//_columnPanels[i].color = SelectedPanelColor;
							if (_panelHighlightTweeners[i] == null)
							{
								_panelHighlightTweeners[i] = _columnPanels[i].DOColor(SelectedPanelColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
							}
						}
					}
				}
				else
				{
					if (!_lockedCharacterForUse)
					{
						Character hovered = _characters.Find(x => x.entry == column.character);
						_activeUseCharacter = _characters.IndexOf(hovered);

						for (int i = 0; i < _characters.Count; i++)
						{
							Character character = _characters[i];

							if (ActiveUseCharacter != character)
							{
								for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
								{
									if (_bustHighlightTweeners[character][j] != null)
									{
										_bustHighlightTweeners[character][j].Kill();
										_bustHighlightTweeners[character][j] = null;
									}
								}

								foreach (Image image in _bustImages[character])
								{
									image.color = UnselectedBustColor;
								}
							}
							else
							{
								List<Image> bustImage = _bustImages[character];

								for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
								{
									if (_bustHighlightTweeners[character][j] == null)
									{
										_bustHighlightTweeners[character][j] = bustImage[j].DOColor(SelectedBustColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
									}
									//image.color = SelectedBustColor;
								}
							}
						}
					}

					RefreshList();
				}
			}
		}

		private void ConfirmStickerUse([NotNull] PartyMemberColumn column)
		{
			Table script = Lua.NewScript(_useSticker.asset.script.Asset);

			var array = new object[] { column.character.nanokin };

			if (script.TryGet(LuaEnv.FUNC_USABLE_MENU, out DynValue val) && val.Type == DataType.Function)
			{
				DynValue is_usable = val.Function.Call(array);

				if (is_usable.AsBool(out bool ret, true) && !ret)
				{
					if (script.TryGet(LuaEnv.FUNC_GET_ERROR_MENU, out DynValue err) && err.Type == DataType.Function)
					{
						DynValue error = err.Function.Call(array);

						if (error.AsString(out string message, "") && !string.IsNullOrEmpty(message))
						{
							OverworldNotifyUI.DoGeneralNotificationPopup(message, 1.5f).Forget();
						}
					}

					return;
				}
			}

			Lua.Invoke(script, LuaEnv.FUNC_CONSUME, array); // TODO the user picks a monster

			_useSticker.instance.Charges--;
			//_useSticker.instance.Entry.Charges--;

			_useSticker.RefreshUI();

			if (_useSticker.instance.Charges <= 0)
			{
				_useSticker.interactable = false;
				StopStickerUse();
				currentUseMenuState = UseMenuState.StickerSelect;
				RefreshUseStickerUI();
			}

			column.RefreshUI();

			RefreshList();
		}

		private void StopStickerUse()
		{
			_useSticker               = null;
			_columnGroup.interactable = false;
			//UGUI.SelectNone();
		}

		private void PreviewNone()
		{
			DescriptionUI.SetNone();
			NanokinUI.CompareNone();
		}

		private void PreviewSticker(StickerInstance sticker)
		{
			NanokinInstance nanokin = ActiveEquipCharacter.entry.nanokin;

			DescriptionUI.ChangeSticker(sticker);
			//if (sticker.Asset.IsEquipment)
			//{
			nanokin.Stickers.Add(sticker);
			nanokin.RecalculateStats();

			Pointf   newpoints     = nanokin.MaxPoints;
			Statf    newstats      = nanokin.Stats;
			Elementf newefficiency = nanokin.Efficiencies;

			nanokin.Stickers.Remove(sticker);
			nanokin.RecalculateStats();

			NanokinUI.CompareWith(newpoints, newstats, newefficiency);
			//}
		}

	#endregion

		private void Update()
		{
			//LoadingPane.gameObject.SetActive(_loading);

			bool canExit = true;

			/*if (Keyboard.current.uKey.isPressed)
				Debug.Log(UGUI.SelectedObject.name, UGUI.SelectedObject);*/

			if (_loading || (_state != States.Regular))
				return;

			switch (TabBar.selectedIndex)
			{
				case TAB_USE:
					CheckUseModeInputs(ref canExit);
					break;
				case TAB_EQUIP:
				case TAB_FAVORITES:
					CheckEquipModeInputs(ref canExit);
					break;
			}

			if (canExit)
				DoExitControls();
		}

		private void CheckEquipModeInputs(ref bool canExit)
		{
			if (!Editor.GridLocked)
			{
				//Scroll through top-right tabs
				if (GameInputs.menuLeft.IsPressed)
				{
					TabBar.SelectPreviousAvailable();
					return;
				}
				else if (GameInputs.menuRight.IsPressed)
				{
					TabBar.SelectNextAvailable();
					return;
				}

				//Scroll through grids
				if (GameInputs.menuLeft2.AbsorbPress(0.3f) || GameInputs.menuNavigate.left.IsPressed)
				{
					SelectGrid(_activeEquipCharacter - 1);
				}
				else if (GameInputs.menuRight2.AbsorbPress(0.3f) || GameInputs.menuNavigate.right.IsPressed)
				{
					SelectGrid(_activeEquipCharacter + 1);
				}

				//Select a grid for modification
				if (GameInputs.confirm.IsPressed)
				{
					Editor.ToggleGridLock(true);

					SetEquipMenuState(EquipMenuState.CellSelect);
				}
			}
			else if (!Editor.IsPlacing)
			{
				//Toggle between selecting cells on a grid and stickers in the list
				if (GameInputs.toggleMode.AbsorbPress(0.25f))
				{
					SetEquipMenuState(((currentEquipMenuState == EquipMenuState.CellSelect) ? EquipMenuState.StickerSelect : EquipMenuState.CellSelect));
				}

				//Move a reticle around the grid if selecting a cell, or through the rightmost list if selecting a sticker
				if (GameInputs.menuNavigate.AnyPressed)
				{
					Vector2Int movement = Vector2Int.RoundToInt(GameInputs.menuNavigate.Value);
					movement.y *= -1;

					switch (currentEquipMenuState)
					{
						case EquipMenuState.CellSelect:
							//Debug.Log("Movement: " + movement);

							Editor.MoveCellSelection(movement);

							DetachStickerDisplay.SetActive(Editor.HasStickerAt(Editor.CurrentCellSelection, out GridSticker sticker));

							if (sticker == null && (browsedGridSticker != null))
							{
								browsedGridSticker.OnDeselect(null);
								browsedGridSticker = null;
							}
							else if ((sticker != null) && (browsedGridSticker == null))
							{
								browsedGridSticker = sticker;
								browsedGridSticker.OnSelect(null);
							}

							break;
						case EquipMenuState.StickerSelect:
							_currentEquipListSticker = Mathf.Clamp(_currentEquipListSticker + movement.y, 0, _listEntries.Count);

							StickerList.Select(_currentEquipListSticker);

							//if (UGUI.SelectedObject == null)
							//{
							//	SelectFirstListSticker();
							//	GameSFX.PlayGlobal(AdReturnToList);
							//}
							//else if (UGUI.GetSelected(out ListSticker lsticker))
							//{
							//	if (GameInputs.menuNavigate.left.IsPressed)
							//	{
							//		_lastSelectedListEntry = lsticker;
							//		// Move selection to grid stickers
							//		if (ActiveEquipCharacter.grid.stickers.Count > 0)
							//		{
							//			UGUI.Select(ActiveEquipCharacter.grid.stickers[0]);
							//			GameSFX.PlayGlobal(AdSelectListEntry);
							//		}
							//		else
							//		{
							//			GameSFX.PlayGlobal(AdInvalidNavigation);
							//		}
							//	}
							//}

							break;
					}

					// else if (GameInputs.move.down.IsPressed)
					// 	{
					// 		// Select next
					// 		UGUI.SelectNext(StickerList.ContentRoot, true);
					// 		GameSFX.PlayGlobal(AdSelectListEntry);
					// 	} else if (GameInputs.move.up.IsPressed)
					// 	{
					// 		UGUI.SelectPrevious(StickerList.ContentRoot, true);
					// 		GameSFX.PlayGlobal(AdSelectListEntryPrevious);
					// 	}
					// }
				}

				if (currentEquipMenuState == EquipMenuState.StickerSelect)
				{
					if (GameInputs.confirm.IsPressed)
					{
						canExit = false;

						if ((StickerList.Selected != null) && Editor.FindFirstFreeCell(out GridCell cell))
						{
							_lastSelectedListEntry = null;
							OnListStickerClicked(StickerList.Selected);
							Editor.Select(cell);

							GameSFX.PlayGlobal(AdSelectListEntry);
						}
						else
						{
							GameSFX.PlayGlobal(AdInvalidNavigation);
						}
					}
					else if (GameInputs.favoriteItem.IsPressed && (StickerList.Selected != null))
					{
						ToggleStickerFavorability(StickerList.Selected);
					}
				}

				if (currentEquipMenuState == EquipMenuState.CellSelect)
				{
					if (GameInputs.detachItem.IsPressed)
					{
						canExit = false;

						if (Editor.HasStickerAt(Editor.CurrentCellSelection, out GridSticker sticker))
						{
							// Move selection back to list
							//Vector3 origin = sticker.transform.position;

							ActiveEquipCharacter.grid.RemoveSticker(sticker);
							OnStickerRemoved(sticker);
							AddStickerBackToCollection(sticker);

							PrefabPool.DestroyOrReturn(sticker.gameObject);

							//if (!UGUI.SelectNearest(ActiveEquipCharacter.grid.ContentRoot, origin) && !UGUI.SelectTowards(origin, Vector3.right))
							//	SelectFirstListSticker();

							GameSFX.PlayGlobal(AdRemoveSticker);
						}
						else
						{
							GameSFX.PlayGlobal(AdInvalidNavigation);
						}
					}
				}

				//else if (GameInputs.rotateSticker.IsPressed && UGUI.GetSelected(out GridSticker current))
				//{
				//	current.rotation = ((current.rotation == 3) ? 0 : (current.rotation + 1));
				//}

				if (GameInputs.cancel.IsPressed)
				{
					//if (UGUI.GetSelected(out GridSticker _))
					//{
					//	Editor.CancelPlacement();

					//	SetEquipMenuState(previousEquipMenuState);

					//	// Move selection back to list
					//	if (_lastSelectedListEntry != null)
					//		UGUI.Select(_lastSelectedListEntry, true);
					//	else
					//		SelectFirstListSticker();

					//	GameSFX.PlayGlobal(AdReturnToList);

					//	canExit = false;
					//}

					if (Editor.GridLocked)
					{
						Editor.ToggleGridLock(false);

						SetEquipMenuState(EquipMenuState.GridSelect);

						canExit = false;
					}
				}
			}
			else
			{
				canExit = false;
			}
		}

		private void CheckUseModeInputs(ref bool canExit)
		{
			if (GameInputs.menuLeft.IsPressed)
			{
				TabBar.SelectPreviousAvailable();
				return;
			}
			else if (GameInputs.menuRight.IsPressed)
			{
				TabBar.SelectNextAvailable();
				return;
			}

			if (GameInputs.menuLeft2.IsPressed || GameInputs.menuNavigate.left.IsPressed)
			{
				if (_useSticker)
				{
					_activeUseListCharacter = (_activeUseListCharacter - 1).Wrap(_characters.Count);

					for (int i = 0; i < _panelHighlightTweeners.Count; i++)
					{
						if (i != _activeUseListCharacter)
						{
							if (_panelHighlightTweeners[i] != null)
							{
								_panelHighlightTweeners[i].Kill();
								_panelHighlightTweeners[i] = null;
							}

							_columnPanels[i].color = UnselectedPanelColor;
						}
						else
						{
							//_columnPanels[i].color = SelectedPanelColor;
							if (_panelHighlightTweeners[i] == null)
							{
								_panelHighlightTweeners[i] = _columnPanels[i].DOColor(SelectedPanelColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
							}
						}
					}
				}
				else if (!_lockedCharacterForUse)
				{
					_activeUseCharacter = (_activeUseCharacter - 1).Wrap(_characters.Count);

					for (int i = 0; i < _characters.Count; i++)
					{
						Character character = _characters[i];

						if (ActiveUseCharacter != character)
						{
							for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
							{
								if (_bustHighlightTweeners[character][j] != null)
								{
									_bustHighlightTweeners[character][j].Kill();
									_bustHighlightTweeners[character][j] = null;
								}
							}

							foreach (Image image in _bustImages[character])
							{
								image.color = UnselectedBustColor;
							}
						}
						else
						{
							List<Image> bustImage = _bustImages[character];

							for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
							{
								if (_bustHighlightTweeners[character][j] == null)
								{
									_bustHighlightTweeners[character][j] = bustImage[j].DOColor(SelectedBustColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
								}
								//image.color = SelectedBustColor;
							}
						}
					}

					RefreshList();
				}
			}

			if (GameInputs.menuRight2.IsPressed || GameInputs.menuNavigate.right.IsPressed)
			{
				if (_useSticker)
				{
					_activeUseListCharacter = (_activeUseListCharacter + 1).Wrap(_characters.Count);

					for (int i = 0; i < _panelHighlightTweeners.Count; i++)
					{
						if (i != _activeUseListCharacter)
						{
							if (_panelHighlightTweeners[i] != null)
							{
								_panelHighlightTweeners[i].Kill();
								_panelHighlightTweeners[i] = null;
							}

							_columnPanels[i].color = UnselectedPanelColor;
						}
						else
						{
							//_columnPanels[i].color = SelectedPanelColor;
							if (_panelHighlightTweeners[i] == null)
							{
								_panelHighlightTweeners[i] = _columnPanels[i].DOColor(SelectedPanelColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
							}
						}
					}
				}
				else if (!_lockedCharacterForUse)
				{
					_activeUseCharacter = (_activeUseCharacter + 1).Wrap(_characters.Count);

					for (int i = 0; i < _characters.Count; i++)
					{
						Character character = _characters[i];

						if (ActiveUseCharacter != character)
						{
							for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
							{
								if (_bustHighlightTweeners[character][j] != null)
								{
									_bustHighlightTweeners[character][j].Kill();
									_bustHighlightTweeners[character][j] = null;
								}
							}

							foreach (Image image in _bustImages[character])
							{
								image.color = UnselectedBustColor;
							}
						}
						else
						{
							List<Image> bustImage = _bustImages[character];

							for (int j = 0; j < _bustHighlightTweeners[character].Count; j++)
							{
								if (_bustHighlightTweeners[character][j] == null)
								{
									_bustHighlightTweeners[character][j] = bustImage[j].DOColor(SelectedBustColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
								}
								//image.color = SelectedBustColor;
							}
						}
					}

					RefreshList();
				}
			}

			if (_useSticker != null)
			{
				canExit = false;

				if (GameInputs.confirm.IsPressed)
				{
					ConfirmStickerUse(ActiveUseCharacter.column);
				}
				else if (GameInputs.cancel.IsPressed)
				{
					StopStickerUse();

					currentUseMenuState = UseMenuState.StickerSelect;

					RefreshUseStickerUI();
				}
			}
			else
			{
				if (GameInputs.cancel.IsPressed && _lockedCharacterForUse)
				{
					_lockedCharacterForUse = false;
					canExit                = false;

					List<Image> bustImages = _bustImages[ActiveUseCharacter];

					for (int i = 0; i < _bustHighlightTweeners[ActiveUseCharacter].Count; i++)
					{
						if (_bustHighlightTweeners[ActiveUseCharacter][i] != null)
						{
							_bustHighlightTweeners[ActiveUseCharacter][i].Kill();
							_bustHighlightTweeners[ActiveUseCharacter][i] = null;
						}

						bustImages[i].color = UnselectedBustColor;

						_bustHighlightTweeners[ActiveUseCharacter][i] = bustImages[i].DOColor(SelectedBustColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
					}

					currentUseMenuState = UseMenuState.InventorySelect;

					RefreshUseStickerUI();
				}
			}
		}

		private void LateUpdate()
		{
			if (!menuActive) return;

			// StickerList.ListRoot.anchoredPosition         = _listPosition.value * StickerList.ListRoot.sizeDelta;
			// DescriptionDisplay.transform.anchoredPosition = new Vector2(DescriptionDisplay.transform.anchoredPosition.x, _descriptionPosition.value * DescriptionDisplay.transform.sizeDelta.y);
			_gridRootScrolling        = UnityHelper.EasedLerpVector2(_gridRootScrolling, TargetScrolling, ScrollingSpeed);
			GridRoot.anchoredPosition = _gridRootScrolling;
		}

		private void ToggleStickerFavorability([NotNull] ListSticker sticker)
		{
			sticker.SetFavorited(!sticker.Favorited);

			SaveManager.SaveCurrent();

			if (!viewingFavorites)
			{
				ToggleFavoritePrompt.text = (!sticker.Favorited ? "Favorite Sticker" : "Un-Favorite Sticker");
			}
			else
			{
				RefreshList();
			}
		}

		protected override UniTask disableMenu()
		{
			GameInputs.mouseUnlocks.Remove("sticker_menu");

			OverworldNotifyUI.InstantlyHideNotificationPopup();

			for (int i = 0; i < _panelHighlightTweeners.Count; i++)
			{
				if (_panelHighlightTweeners[i] != null)
				{
					_panelHighlightTweeners[i].Kill();
					_panelHighlightTweeners[i] = null;
				}
			}

			_panelHighlightTweeners.Clear();

			foreach (var tweeners in _bustHighlightTweeners.Values)
			{
				for (int i = 0; i < tweeners.Count; i++)
				{
					if (tweeners[i] != null)
					{
						tweeners[i].Kill();
						tweeners[i] = null;
					}
				}
			}

			_bustHighlightTweeners.Clear();

			foreach (Character kid in _characters)
			{
				PrefabPool.DestroyOrReturn(kid.grid);
				PrefabPool.DestroyOrReturn(kid.column);
			}

			//_inventory.Clear();

			_stickerDatabase.Clear();
			_bustImages.Clear();

			_columnPanels.Clear();
			_characters.Clear();
			StickerList.Clear();

			_activeEquipCharacter   = -1;
			_activeUseCharacter     = 0;
			_activeUseListCharacter = 0;
			_lockedCharacterForUse  = false;

			SaveManager.SaveCurrent();

			return UniTask.CompletedTask;
		}

		private Vector2 TargetScrolling
		{
			get
			{
				if (_activeEquipCharacter == -1) return Vector2.zero;
				return ActiveEquipCharacter.grid.transform.anchoredPosition * new Vector2(-1, 1);
			}
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

		// private void Save()
		// {
		// 	SaveManager.current.Stickers.Clear();
		//
		// 	for (var i = 0; i < StickerList.ContentRoot.childCount; i++)
		// 	{
		// 		Transform   child       = StickerList.ContentRoot.GetChild(i);
		// 		ListSticker listSticker = child.GetComponent<ListSticker>();
		//
		// 		SaveManager.current.Stickers.Add(listSticker.asset.Address);
		// 	}
		// }

		public enum States
		{
			Regular,
			SubMenu
		}


		private class Character
		{
			public StickerGrid       grid;
			public CharacterEntry    entry;
			public PartyMemberColumn column;
		}
	}
}
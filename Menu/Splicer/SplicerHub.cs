using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Audio;
using Anjin.Core.Flags;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.UI;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Menu.Formation;
using Menu.LoadSave;
using Menu.Quest;
using Menu.Start;
using Menu.Sticker;
using Overworld.UI;
using Overworld.UI.Settings;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Util.Addressable;
using Util.RenderingElements.Barrel;
using MenuController = Anjin.Nanokin.MenuManager;

public class SplicerHub : StaticMenu<SplicerHub>
{
	public readonly string[] SUBMENU_STRINGS_MOUSE_UNLOCK_USE =
	{
		"quest_menu", "formation_menu", "limb_menu", "sticker_menu", "level_select_menu"
	};

	[Title("References")]
	[SerializeField] private Canvas Canvas;
	[SerializeField] private Camera            Cam;
	[SerializeField] private Animator          Animator;
	[SerializeField] private PartyMemberColumn PrefabColumn;
	[SerializeField] private FlowLayoutGroup   ColumnFlow;
	[SerializeField] private RawImage          BarrelDisplayImage;
	[SerializeField] private GameObject		   TutorialScreen;

	[Title("Design")]
	[SerializeField] private Vector2[] ColumnSpacingForCount;
	[SerializeField] private float        ScreenFadeDuration = 0.25f;
	[SerializeField] private AudioDef     SFX_OpenHub;
	[SerializeField] private AudioDef     SFX_ExitHub;
	[SerializeField] private AudioDef     SFX_OpenSubmenu;
	[SerializeField] private AudioDef     SFX_ExitSubmenu;
	[SerializeField] private AudioDef     SFX_BeginMonsterSelection;
	[SerializeField] private AudioDef     SFX_CancelMonsterSelection;
	[SerializeField] private AudioDef     SFX_ConfirmMonsterSelection;
	[SerializeField] private AudioDef	  SFX_OpenSubmenuError;
	[SerializeField] private List<Sprite> _barrelIcons;
	public RectTransform BarrelDisabledMessage;
	public Button BackButton;
	private static readonly float ConfirmationCooldown = 0.25f; //whatever
	private static float _lastConfirmTime;
	public static bool Lock;

	private bool _supressInputs;

	public static bool CanConfirm => Time.time >= _lastConfirmTime + ConfirmationCooldown;
	public static void ResetConfirm()
	{
		_lastConfirmTime = Time.time;
	}

	private RectTransform     _raycastImage;
	private PartyMemberColumn _selectedColumn;

	public AudioClip SplicerMusic;

	private                   AsyncHandles            _handles;
	private                   Vector2                 _defaultFlowSpacing;
	//private                 List<PartyMemberColumn> _columns;
	private					  Dictionary<string, PartyMemberColumn> _columns;
	private					  List<CharacterEntry>	  _party;
	private                   AudioZone               _menuMusicZone;
	[ShowInInspector] private States                  _state;
	private AudioManager _audio;

	public StartBarrel Barrel => SplicerBarrel.Live.Barrel;

	private int currentColumn;


	public static bool ShouldShowCredits => menuActive && Live != null && Live._state != States.SubMenu;

	protected override void OnAwake()
	{
		base.OnAwake();

		ResetData();
		_handles            = new AsyncHandles();

		_menuMusicZone                                    = AudioZone.CreateMusic(SplicerMusic, priority: 500);
		_menuMusicZone.OverrideTrack.Config.InitialVolume = 1;
		_audio = FindObjectOfType<AudioManager>();

		BackButton.onClick.AddListener(() => ChangeState(States.MenuSelection));

		Cam.gameObject.SetActive(false);
	}

	public void ResetData()
	{
		currentColumn = 0;

		//_columns            = new List<PartyMemberColumn>();
		_columns			= new Dictionary<string, PartyMemberColumn>();
		_defaultFlowSpacing = new Vector2(ColumnFlow.SpacingX, ColumnFlow.SpacingY);
	}

	protected override async UniTask enableMenu()
	{
		GameInputs.mouseUnlocks.Add("splicer");


		GameController.Live.State_WorldPause = GameController.WorldPauseState.FullPaused;

		GameSFX.PlayGlobal(SFX_OpenHub, this);

		_lastConfirmTime = Time.time;

		// Prepare the scenes
		// ----------------------------------------
		await MenuController.LoadMenu(Menus.SplicerBarrel);
		await SplicerBarrel.EnableMenu(BarrelDisabledMessage);
		await MenuController.SetSplicerBG(true);

		_raycastImage = BarrelDisplayImage.GetComponent<RectTransform>();

		//OverworldHUD.ShowCredits();

		AudioManager.AddZone(_menuMusicZone);
		AudioManager.ambienceLayer.Stop();
		_audio.MixerGroup_Ambient.audioMixer.SetFloat("Ambient_Volume", -80); //sorry, couldn't find a better fix

		// Create party member columns
		// ----------------------------------------
		SaveData save = await SaveManager.GetCurrentAsync();
		_party = save.Party;

		var tasks = new List<UniTask>();
		if (_columns.Count == 0)
		{
			StickerMenu.inputCharacters = _party;

			async UniTask LoadKid(CharacterEntry character)
			{
				PartyMemberColumn column = Instantiate(PrefabColumn, ColumnFlow.transform, false);
				column.ResetHandles();
				await column.SetCharacter(character);

				column.onPointerClick += OnColumnPointerClick;
				column.onPointerEnter += OnColumnPointerEnter;
				column.onSelected     += OnColumnSelect;

				//column.OnNameClick += OnColumnNameClick;

				if (!_columns.ContainsKey(character.Name))
				{
					//_columns.Add(column);
					_columns.Add(character.Name, column);
				}
			}

			tasks.AddRange(_party.Select(LoadKid));
		}
		else
		{
			//foreach (PartyMemberColumn column in _columns)
			foreach (PartyMemberColumn column in _columns.Values)
			{
				column.RefreshPuppet().ForgetWithErrors();
				column.RefreshUI();
			}
		}

		await UniTask.WhenAll(tasks);
		Cam.gameObject.SetActive(true);
		await ChangeState(States.MenuSelection);

		if (!Flags.GetBool("tut_splicer_hub"))
		{
			Flags.SetBool("tut_splicer_hub", true);
			//TutorialScreen.SetActive(true);
			ShowSplashScreen("SplashScreens/Demo_SplicerHub").Forget();
		}
	}

	private void OnColumnPointerEnter(PartyMemberColumn col)
	{
		col.Select();
	}

	private async void OnColumnPointerClick(PartyMemberColumn col)
	{
		if (_state == States.MonsterSelection)
		{
			//currentColumn = _columns.IndexOf(col);
			currentColumn = _party.IndexOf(col.character);
			_selectedColumn = col;
			LimbMenu.input = col.character;
			LimbMenu.currentCharacterIndex = Mathf.Max(_party.FindIndex(x => (x.asset.Name == col.character.asset.Name)), 0);
			//GameSFX.PlayGlobal(SFX_ConfirmMonsterSelection, this);
			await ShowLimbMenu();
		}
	}

	private void OnColumnSelect(PartyMemberColumn col)
	{
		//currentColumn = _columns.IndexOf(col);
		currentColumn = _party.IndexOf(col.character);
		_selectedColumn = col;
	}

	/*private void OnColumnNameClick(PartyMemberColumn col)
	{
		OnScreenKeyboard.Open(col, col.NameField);
	}*/

	private async UniTask ChangeState(States state)
	{
		Canvas.gameObject.SetActive(state != States.None && state != States.SubMenu);

		//foreach (PartyMemberColumn col in _columns)
		foreach (PartyMemberColumn col in _columns.Values)
		{
			col.gameObject.SetActive(state != States.SubMenu);
			col.interactable = state == States.MonsterSelection;
		//	col.NameField.enabled = state != States.MonsterSelection;
		}

		switch (state)
		{
			case States.None:
				await SplicerBarrel.DisableMenu();
				break;

			case States.MenuSelection:
				// Show and configure the barrel
				// ----------------------------------------
				// await MenuController.EnableMenu(Menus.SplicerBarrel);
				await ShowBarrel();
				break;

			case States.MonsterSelection:
				await SplicerBarrel.DisableMenu(BarrelDisabledMessage);

				SplicerBarrel.interactivity = MenuInteractivity.None;
				CharacterEntry character = _party[currentColumn];
				//PartyMemberColumn col = _columns[currentColumn];
				PartyMemberColumn col = _columns[character.Name];
				col.Select();

				//EventSystem.current.SetSelectedGameObject(_selectedColumn != null
				//	? _selectedColumn.gameObject
				//	: _columns[0].gameObject);

				GameInputs.confirm.AbsorbPress(0.5f);
				break;

			case States.SubMenu:
				await SplicerBarrel.DisableMenu();
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(state), state, null);
		}

		_state = state;
	}

	private void Update()
	{
		//_supressInputs = OnScreenKeyboard.isActiveAndEnabled;
		if (_state == States.None)
			return;

		switch (_state)
		{

			case States.MenuSelection:
				if (CanConfirm)
				{
					if(CheckConfirm(Barrel.DoMouseInputs(Cam, _raycastImage)) || CheckConfirm(Barrel.DoKeyboardInputs()))
					{
						ResetConfirm();
					}
				}

				if (GameInputs.cancel.AbsorbPress(1) || GameInputs.splicer.AbsorbPress(1))
				{
					SplicerBarrel.interactivity = MenuInteractivity.None;
					OnExitStatic();
				}

				return;

			case States.MonsterSelection:
				// KEYBOARD/GAMEPAD
				// ----------------------------------------
				if ((GameInputs.menuNavigate.left.IsDown || GameInputs.menuLeft2.AbsorbPress(0.25f)) /*&& EventSystem.current.currentSelectedGameObject == null */ && CanConfirm)
				{
					//currentColumn = (currentColumn - 1).Wrap(_columns.Count);
					currentColumn = (currentColumn - 1).Wrap(_party.Count);
					Debug.Log("CURRENT COLUMN IS: " + currentColumn);
					//PartyMemberColumn defaultSel = _selectedColumn ? _selectedColumn : _columns[0];
					//PartyMemberColumn defaultSel = _columns[currentColumn];
					CharacterEntry character = _party[currentColumn];
					PartyMemberColumn defaultSel = _columns[character.Name];
					defaultSel.Select();
					ResetConfirm();
				}
				else if ((GameInputs.menuNavigate.right.IsDown || GameInputs.menuRight2.AbsorbPress(0.25f)) /*&& EventSystem.current.currentSelectedGameObject == null */ && CanConfirm)
				{
					//currentColumn = (currentColumn + 1).Wrap(_columns.Count);
					currentColumn = (currentColumn + 1).Wrap(_party.Count);
					Debug.Log("CURRENT COLUMN IS: " + currentColumn);
					//PartyMemberColumn defaultSel = _selectedColumn ? _selectedColumn : _columns[0];
					//PartyMemberColumn defaultSel = _columns[currentColumn];
					CharacterEntry character = _party[currentColumn];
					PartyMemberColumn defaultSel = _columns[character.Name];
					defaultSel.Select();
					ResetConfirm();
				}

				if (GameInputs.confirm.AbsorbPress(1) && CanConfirm)
				{
					LimbMenu.input = _selectedColumn.character;
					GameSFX.PlayGlobal(SFX_ConfirmMonsterSelection, this);
					ShowLimbMenu().Forget();
					ResetConfirm();
				}
				else if ((GameInputs.cancel.AbsorbPress(1) || Mouse.current.rightButton.wasPressedThisFrame) && CanConfirm)
				{
					GameSFX.PlayGlobal(SFX_CancelMonsterSelection, this);
					ChangeState(States.MenuSelection).Forget();
					ResetConfirm();
				}

				break;
		}

		// Update the columnflow spacing depending
		// on how many sub transforms for a better fit
		// ------------------------------------------------------------
		if (ColumnFlow.transform.childCount == 0 || ColumnFlow.transform.childCount >= ColumnSpacingForCount.Length)
		{
			ColumnFlow.SpacingX = _defaultFlowSpacing.x;
			ColumnFlow.SpacingY = _defaultFlowSpacing.y;
		}
		else
		{
			ColumnFlow.SpacingX = ColumnSpacingForCount[ColumnFlow.transform.childCount - 1].x;
			ColumnFlow.SpacingY = ColumnSpacingForCount[ColumnFlow.transform.childCount - 1].y;
		}
	}

	private static bool CheckConfirm((bool, ListPanel) clicked)
	{
		if (clicked.Item1)
		{
			var item = (BarrelItem)clicked.Item2.Value;

			if (item != null)
			{
				item.onConfirmed?.Invoke();
				return true;
			}
		}

		return false;
	}

	protected override async UniTask disableMenu()
	{
		ChangeState(States.None).Forget();

		MenuController.DisableMenu(Menus.SplicerHub);
		GameInputs.mouseUnlocks.Remove("splicer");
		foreach (var s in SUBMENU_STRINGS_MOUSE_UNLOCK_USE)
		{
			GameInputs.mouseUnlocks.Remove(s);
		}

		//OverworldHUD.HideCredits();

		Cam.gameObject.SetActive(false);
		_handles.ReleaseAll();

		await SplicerBarrel.DisableMenu();
		await MenuController.SetSplicerBG(false);

		GameSFX.PlayGlobal(SFX_ExitHub, this);

		AudioManager.RemoveZone(_menuMusicZone);
		AudioManager.ambienceLayer.Start();
		_audio.MixerGroup_Ambient.audioMixer.SetFloat("Ambient_Volume", -4);

		GameController.Live.State_WorldPause = GameController.WorldPauseState.Running;
		//OnScreenKeyboard.Close(false);

		SaveManager.SaveCurrent();

		/*for (var i = 0; i < ColumnFlow.transform.childCount; i++)
		{
			Transform child = ColumnFlow.transform.GetChild(i);
			Destroy(child.gameObject);
		}*/
	}

	private async UniTask ShowBarrel()
	{
		await SplicerBarrel.EnableMenu(BarrelDisabledMessage);

		// This needs to happen -after- the menu has been enabled.
		var entries = new List<BarrelItem>
		{
			new BarrelItem(_barrelIcons[0], "Splice", true, () =>
			{
				GameSFX.PlayGlobal(SFX_BeginMonsterSelection, this);
				Live.ChangeState(States.MonsterSelection).Forget();
			}),

			new BarrelItem(_barrelIcons[1], "Stickers", true, () =>
			{
				ShowSubmenu<StickerMenu>(Menus.Sticker).Forget();
			}),

			new BarrelItem(_barrelIcons[2], "Formation", true, () =>
			{
				ShowSubmenu<FormationMenu>(Menus.Formation).Forget();
			}),

			new BarrelItem(_barrelIcons[3], "Quests", true, () =>
			{
				ShowSubmenu<QuestMenu>(Menus.Quests).Forget();
			}),

			new BarrelItem(_barrelIcons[4], "Nanopedia", false, () => //TODO: CHANGE THIS TO TRUE WHEN NANOPEDIA GETS IMPLEMENTED
			{
				Debug.Log("This menu is not implemented yet.");
				GameSFX.PlayGlobal(SFX_OpenSubmenuError, this);
			}),

			new BarrelItem(_barrelIcons[8], "Save Data", GameController.CanSave, () =>
			{
				if (GameController.CanSave)
				{
					ShowSubmenu<LoadSaveMenu>(Menus.Save).Forget();
				}
				else
				{
					GameSFX.PlayGlobal(SFX_OpenSubmenuError, this);
				}
			}),

			new BarrelItem(_barrelIcons[9], "Load Data", true, () =>
			{
				ShowSubmenu<LoadSaveMenu>(Menus.Load).Forget();
			}),

			new BarrelItem(_barrelIcons[5], "Settings", true, () =>
			{
				ShowSubmenu<SettingsMenu>(Menus.Settings).Forget();
			}),

			new BarrelItem(_barrelIcons[6], "Tutorial", true, () =>
			{
				//ShowSplashScreen("SplashScreens/Demo_Tutorial").Forget();
				ShowSubmenu<TutorialMenu>(Menus.Tutorial).Forget();
			}),

			new BarrelItem(_barrelIcons[7], "Resume Game", true, () =>
			{
				OnExitStatic();
			})
		};

		Barrel.ChangeEntries(entries);
	}

	private async UniTask ShowSubmenu<TMenu>(Menus menu, States onReturn = States.MenuSelection)
		where TMenu : StaticMenu<TMenu>
	{
		if (!CanConfirm || Lock) return;
		Lock = true;
		ResetConfirm();
		GameSFX.PlayGlobal(SFX_OpenSubmenu, this);

		//OverworldHUD.HideCredits();

		try
		{
			await GameEffects.FadeOut(ScreenFadeDuration);
			await ChangeState(States.SubMenu);
			await MenuController.SetSplicerBG(true);
			await MenuController.SetMenu(menu);
			await GameEffects.FadeIn(ScreenFadeDuration);
		}
		finally
		{
			Lock = false;
		}

		async void ExitHandler(TMenu self)
		{
			if (Lock) return;
			Lock = true;
			GameSFX.PlayGlobal(SFX_ExitSubmenu, this);

			try
			{
				await GameEffects.FadeOut(ScreenFadeDuration);
				{
					//for (var i = 0; i < _columns.Count; i++)
					//	_columns[i].RefreshPuppet().Forget();

					for (var i = 0; i < _party.Count; i++)
					{
						CharacterEntry character = _party[i];
						_columns[character.Name].RefreshPuppet().Forget();
					}

					await MenuController.SetMenu(menu, false);
					await ChangeState(onReturn);
					//OverworldHUD.ShowCredits();
				}
				await GameEffects.FadeIn(ScreenFadeDuration);
			}
			finally
			{
				Lock = false;
			}

			//foreach (PartyMemberColumn col in _columns)
			foreach (PartyMemberColumn col in _columns.Values)
			{
				col.RefreshUI();
			}
		}

		StaticMenu<TMenu>.exitHandler = ExitHandler;
	}

	private async UniTask ShowSplashScreen(string address)
	{
		GameSFX.PlayGlobal(SFX_OpenSubmenu, this);

		//OverworldHUD.HideCredits();

		//await GameEffects.FadeOut(ScreenFadeDuration);
		await ChangeState(States.SubMenu);
		await MenuController.SetSplicerBG(true);

		await SplashScreens.ShowPrefabAsync(address, async () =>
		{
			GameSFX.PlayGlobal(SFX_ExitSubmenu, this);

			//OverworldHUD.ShowCredits();
			await ChangeState(States.MenuSelection);

			//foreach (PartyMemberColumn col in _columns)
			foreach (PartyMemberColumn col in _columns.Values)
				col.RefreshUI();
		});
	}

	private UniTask ShowLimbMenu()
	{
		return ShowSubmenu<LimbMenu>(Menus.EquipLimb, States.MonsterSelection);
	}

	public enum States
	{
		None,
		MenuSelection,
		MonsterSelection,
		SubMenu
	}
}
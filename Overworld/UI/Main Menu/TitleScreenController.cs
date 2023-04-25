using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Menu.LoadSave;
using Overworld.UI;
using Overworld.UI.Settings;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util.Extensions;
using Util.Odin.Attributes;
using Util.UniTween.Value;

public class TitleScreenController : SerializedMonoBehaviour
{
	public enum MenuScreen
	{
		None      = -1,
		Init		= 0,
		Beginning,	/*= 1,*/
		Animation,
		Main,
		NewGame,
		LoadGame,
		Options,
		Changelog,
		Quit,
		StartingGame
	}

	public enum Mode
	{
		Standard,
		Presentation,
	}

	public bool ChangelogOnStart = true;

	public  Camera    BackgroundCamera;
	public  Transform BackgroundCameraAnchor;
	public  float     AspectRatioScalingFactor;
	public  Vector3   BackCamAnchorAspectRatioDir;
	public  float     BackCamAnchorAspectRatioScalingFactor;
	private Vector3   BackCamAnchorAspectBasePos;

	public Animation   BackgroundAnimator;
	public Animation   MenuAnimator;
	public Animation   LSAnimator;
	public Animation   LSBackgroundAnimator;
	public Animation   BackButtonAnimator;
	public Animation   DeletionPromptAnimator;

	public AudioSource MusicSource;
	public float       MusicDelay;

	[FormerlySerializedAs("time")]
	public float IntroDuration; // Duration before the intro is considered finished and we can interact with the menu items
	public float ScrollDelay = 0.3f;

	public Mode                mode;
	public MenuScreen          StartScreen;

	public AudioDef			   SFX_InvalidChoice;
	public AudioDef			   SFX_Scroll;
	public AudioDef			   SFX_Confirm;

	public InputButtonLabel	   FileDeleteButtonLabel;
	public InputButtonLabel	   DeleteConfirmButtonLabel;
	public InputButtonLabel    DeleteCancelButtonLabel;

	public AnimationClip       BackgroundIntro;
	public AnimationClip	   BackgroundFadeIn;
	public AnimationClip       BackgroundFadeOut;
	public AnimationClip	   MenuFadeIn;
	public AnimationClip	   MenuFadeOut;
	public AnimationClip	   LSFadeIn;
	public AnimationClip	   LSFadeOut;
	public AnimationClip	   LSBackgroundFadeIn;
	public AnimationClip	   LSBackgroundFadeOut;
	public AnimationClip	   BackButtonFadeIn;
	public AnimationClip	   BackButtonFadeOut;
	public AnimationClip	   DeletionPromptFadeIn;
	public AnimationClip       DeletionPromptFadeOut;

	public UGUISetupListInputs MainListSetup;
	public MenuScreen          CurrentScreen;

	public Color			   SelectedTextColor;
	public Color			   UnselectedTextColor;
	public Color			   ButtonHighlightColor;
	public Color			   ButtonUnhighlightColor;

	public TextMeshProUGUI	   YesButton;
	public TextMeshProUGUI	   NoButton;

	public Button    Template_MainMenuOption;
	public Transform Root_MainMenuOptions;

	[NonSerialized, ShowInPlay]
	List<Button> _mainMenuOptions;

	[Title("UI References")]
	[Title("Main Menu", horizontalLine: false)]
	public GameObject MainMenuRoot;

	public GameObject LoadGameRoot;

	//public GameObject OptionsRoot;
	public GameObject QuitRoot;

	public GameObject SoftMask;

	public Transform MainMenuButtons;

	[Space]
	public Button BackButton;

	public Button NewGameButton;

	[NonSerialized, ShowInPlay]
	private Button continueButton;

	//    IWO Con stuff
	//--------------------------------
	public GameObject     IWORoot;
	public Image          BigLogo;
	public TweenableFloat BigLogoAlpha;
	public AnimationCurve BigLogoCurve;


	[Title("Save Menu", horizontalLine: false)]
	public GameObject SaveEntriesRoot;

	public ScrollRect SaveEntriesScrollRect;
	public Transform  DeleteConfirmPopupRoot;
	public GameObject DeleteConfirmPopupYes;

	[Title("Prefabs")]
	public SaveDataPanel SaveEntryPrefab;

	//private List<string>        _savePaths;
	private List<SaveDataPanel> _savePanels;
	private float               _remainingIntroTime;
	private SaveDataPanel       _selectedPanel;

	public LevelManifest FreeportManifest;

	private int _currentFileSlot;
	private int _previousMainMenuButton;
	private int _currentMainMenuButton;

	private bool _canScroll;
	private bool _loadingLSMenu;

	private ScrollRectLerper scrollLerper;

	private void Awake()
	{
		CurrentScreen = MenuScreen.Init;

		//_savePaths  = new List<string>();
		_savePanels = new List<SaveDataPanel>();
		_selectedPanel = null;
		scrollLerper = SaveEntriesScrollRect.GetComponent<ScrollRectLerper>();
		_previousMainMenuButton = 0;
		_currentMainMenuButton = 0;

		_mainMenuOptions = new List<Button>();


		AddOption("New Game",			() => ChangeScreen(MenuScreen.NewGame));
		AddOption("Load Game",			() => ChangeScreen(MenuScreen.LoadGame));
		continueButton = AddOption("Netplay (TODO)", () => {});
		AddOption("Options",			() => ChangeScreen(MenuScreen.Options));
		AddOption("Changelog",			() => ChangeScreen(MenuScreen.Changelog));
		AddOption("Quit",				() =>  ChangeScreen(MenuScreen.Quit));

		Button AddOption(GameText text, Action OnClick)
		{
			Button option = Template_MainMenuOption.InstantiateNew();

			option.gameObject.SetActive(true);
			option.transform.SetParent(Root_MainMenuOptions);
			option.transform.localScale = Vector3.one;

			option.SetButtonText(text);
			option.onClick.AddListener(()=> {OnClick();});
			_mainMenuOptions.Add(option);
			return option;
		}

		UGUI.SetupListNavigation(_mainMenuOptions, AxisDirection.Vertical);

		Template_MainMenuOption.gameObject.SetActive(false);
	}

	private async void Start()
	{
		GameEffects.FadeOut(0);

		// TODO: FILTHY
		GameController.Live.StateApp = GameController.AppState.Menu;

		await GameController.TillIntialized();

		BackCamAnchorAspectBasePos = BackgroundCameraAnchor.transform.position;
		BigLogoAlpha               = new TweenableFloat(0);

		if (mode == Mode.Standard)
		{
			ChangeScreen(StartScreen);
			IWORoot.SetActive(false);
		}
		else if (mode == Mode.Presentation)
		{
			ChangeScreen(MenuScreen.None);
			//await GameController.TillIntialized();
			//await Lua.initTask;
			IWORoot.SetActive(true);
			BigLogo.color = new Color(1, 1, 1, 0);
			var curve = new CurveTo();
			curve.curve = BigLogoCurve;
			BigLogoAlpha.FromTo(0, 1, curve);
			BackgroundAnimator.Play(PlayMode.StopAll);
		}

		FileDeleteButtonLabel.Button = GameInputs.favoriteItem;
		DeleteConfirmButtonLabel.Button = GameInputs.confirm;
		DeleteCancelButtonLabel.Button = GameInputs.cancel;

		MainMenuRoot.SetActive(false);
	}

	private void OnEnable()
	{
		GameInputs.mouseUnlocks.Add("main_menu");
		DebugSystem.onLayout += OnLayout;
	}

	private void OnDisable()
	{
		GameInputs.mouseUnlocks.Remove("main_menu");
		DebugSystem.onLayout -= OnLayout;
	}

	public void ProcessLoadSaveEntrance()
	{
		MainMenuRoot.SetActive(false);
		//BackButton.gameObject.SetActive(true);
		SoftMask.SetActive(true);

		LSBackgroundAnimator.clip = LSBackgroundFadeIn;
		LSAnimator.clip = LSFadeIn;
		BackButtonAnimator.clip = BackButtonFadeIn;
		DeletionPromptAnimator.clip = DeletionPromptFadeIn;

		LSBackgroundAnimator.Play();
		LSAnimator.Play();
		BackButtonAnimator.Play();
		DeletionPromptAnimator.Play();
	}

	public void ProcessLSMenuLoad()
	{
		_loadingLSMenu = false;
	}

	public void ProcessLoadSaveExit()
	{
		foreach (SaveDataPanel panel in _savePanels)
		{
			Destroy(panel.gameObject);
		}

		_savePanels.Clear();

		_selectedPanel = null;

		SoftMask.SetActive(false);
		LoadGameRoot.SetActive(false);
		//BackButton.gameObject.SetActive(false);

		BackgroundAnimator.clip = BackgroundFadeIn;
		MenuAnimator.clip = MenuFadeIn;

		BackgroundAnimator.Play();
		MenuAnimator.Play();
	}

	private IEnumerator ImplementScrollDelay()
	{
		_canScroll = false;

		yield return new WaitForSeconds(ScrollDelay);

		_canScroll = true;
	}

	private void ChangePanels(SaveDataPanel p)
	{
		if (p != _selectedPanel)
		{
			if (_canScroll)
			{
				StartCoroutine(ImplementScrollDelay());

				if (_selectedPanel != null)
				{
					_selectedPanel.ToggleColor(false);
					_selectedPanel.TogglePosition(false);
				}

				_selectedPanel = p;
				_selectedPanel.ToggleColor(true);
				_selectedPanel.TogglePosition(true);
				_currentFileSlot = p.index;

				SaveEntriesScrollRect.ScrollToWithLerp(_selectedPanel.GetComponent<RectTransform>());
			}
		}
	}

	[ShowInPlay]
	public void ChangeScreen(MenuScreen newScreen)
	{
		// CLEANUP PREVIOUS SCREEN
		// ----------------------------------------
		switch (CurrentScreen)
		{
			case MenuScreen.NewGame:
				BackButtonAnimator.SetToEnd(BackButtonFadeOut);
				break;

			case MenuScreen.Quit:
				QuitRoot.gameObject.SetActive(false);
				BackButtonAnimator.SetToEnd(BackButtonFadeOut);
				break;

			case MenuScreen.LoadGame:
				LoadGameRoot.SetActive(false);
				break;

			case MenuScreen.Main:
				MainMenuRoot.SetActive(false);
				break;

			/*case MenuScreen.Options:
				SettingsMenuOld.Close();
				break;*/
		}

		CurrentScreen = newScreen;

		switch (newScreen)
		{
			case MenuScreen.Animation:
				//_savePaths = SaveManager.EnumerateSaves();

				//BackButton.gameObject.SetActive(newScreen > MenuScreen.Main && newScreen != MenuScreen.Quit && newScreen != MenuScreen.StartingGame && newScreen != MenuScreen.Options);
				// SETUP NEW SCREEN
				// ----------------------------------------
				MainMenuRoot.SetActive(false);
				LoadGameRoot.SetActive(false);
				//OptionsRoot.SetActive(newScreen == MenuScreen.Options);
				QuitRoot.SetActive(false);

				MusicSource.PlayDelayed(MusicDelay);
				BackgroundAnimator.Play(PlayMode.StopAll);
				MenuAnimator.SetToEnd(MenuFadeOut);

				_remainingIntroTime = IntroDuration;
				break;

			case MenuScreen.Main:
				// SETUP NEW SCREEN
				// ----------------------------------------
				MainMenuRoot.SetActive(true);
				QuitRoot.SetActive(false);

				if (!MusicSource.isPlaying) MusicSource.Play();

				//int numSaves = _savePaths.Count;

				//continueButton.gameObject.SetActive(SaveManager.SaveDataCount > 0);

				for (int i = 0; i < MainMenuButtons.childCount; i++)
				{
					Transform buttonObject = MainMenuButtons.GetChild(i);

					if (_currentMainMenuButton != i)
					{
						buttonObject.GetComponent<EventTrigger>().OnDeselect(null);
					}
					else
					{
						buttonObject.GetComponent<EventTrigger>().OnSelect(null);
					}
				}

				//NewGameButton.GetComponent<LayoutElement>().minHeight = numSaves > 0 ? 70 : 90; // Scales up New Game when Continuing isn't available. Incompatible with on button hovered scaling without additional logic.
				//EventSystem.current.SetSelectedGameObject(numSaves > 0 ? ContinueButton.gameObject : NewGameButton.gameObject);
				break;

			case MenuScreen.NewGame:
				// SETUP NEW SCREEN
				// ----------------------------------------
				MainMenuRoot.SetActive(false);
				LoadGameRoot.SetActive(false);
				QuitRoot.SetActive(false);

				BackButtonAnimator.SetToEnd(BackButtonFadeIn);

				break;

			case MenuScreen.Options:
				// SETUP NEW SCREEN
				// ----------------------------------------
				MainMenuRoot.SetActive(false);
				LoadGameRoot.SetActive(false);
				QuitRoot.SetActive(false);

				SettingsMenu.exitHandler = delegate (SettingsMenu menu) {
					SettingsMenu.DisableMenu();
					ChangeScreen(MenuScreen.Main);
				};

				SettingsMenu.EnableMenu();
				break;

			case MenuScreen.LoadGame: {

				async void showLoadMenu()
				{
					await GameEffects.FadeOut(0.25f);
					await SplashScreens.ShowPrefabAsync("SplashScreens/Title_LoadBackground");

					await MenuManager.LoadMenu(Menus.Load);

					LoadSaveMenu.Live.Setup(new LoadSaveMenu.Settings {
						post_process = false,
						graident     = false,
					});

					await MenuManager.SetMenu(Menus.Load);
					await GameEffects.FadeIn(0.25f);

					LoadSaveMenu.exitHandler = ExitHandler;
					LoadSaveMenu.Live.OnLoad = () => {
						SplashScreens.Hide();
						SceneManager.UnloadSceneAsync(gameObject.scene);
					};

					async void ExitHandler(LoadSaveMenu self)
					{
						//GameSFX.PlayGlobal(SFX_ExitSubmenu, this);
						await GameEffects.FadeOut(0.25f);
						await SplashScreens.Hide();
						await MenuManager.SetMenu(Menus.Load, false);
						ChangeScreen(MenuScreen.Main);
						/*{
							for (var i = 0; i < _columns.Count; i++)
								_columns[i].RefreshPuppet().Forget();

							await MenuController.SetMenu(menu, false);
							await ChangeState(onReturn);
							OverworldHUD.ShowCredits();
						}*/
						await GameEffects.FadeIn(0.25f);
					}
				}

				showLoadMenu();

				break;
			}

			case MenuScreen.Changelog: {

			} break;

			case MenuScreen.Quit: {
				QuitRoot.gameObject.SetActive(true);
			} break;
		}

		MainListSetup.Recalculate();
	}

	private void Update()
	{
		float aspect = Mathf.Max(0, ((float) Screen.width / (float) Screen.height) - (16f / 9f));
		BackgroundCamera.orthographicSize = Mathf.Max(4.0f, 24.5f - (AspectRatioScalingFactor * aspect));

		BackgroundCameraAnchor.transform.position = BackCamAnchorAspectBasePos +
		                                            BackCamAnchorAspectRatioDir.normalized * Mathf.Min(14f, BackCamAnchorAspectRatioScalingFactor * aspect);

		if (mode == Mode.Presentation)
		{
			var c = BigLogo.color;
			c.a           = BigLogoAlpha;
			BigLogo.color = c;
		}

		switch (CurrentScreen)
		{
			case MenuScreen.Init:
				break;

			case MenuScreen.Beginning:

				GameEffects.FadeOut(0);

				if (!GameController.Live.AnyLoading) {
					ChangeScreen(MenuScreen.Animation);
					GameEffects.FadeIn(0.5f);
				}

				break;

			case MenuScreen.Animation:
				if (_remainingIntroTime > 0)
				{
					_remainingIntroTime -= Time.deltaTime;
					if (_remainingIntroTime <= 0)
					{
						if(ChangelogOnStart)
							ChangeScreen(MenuScreen.Changelog);
						else
							ChangeScreen(MenuScreen.Main);

						MenuAnimator.clip = MenuFadeIn;
						MenuAnimator.Play(PlayMode.StopAll);
					}
				}

				if (GameInputs.confirm.IsPressed || GameInputs.cancel.IsPressed)
				{
					BackgroundAnimator.SetToEnd(BackgroundIntro);
					MenuAnimator.SetToEnd(MenuFadeIn);

					if(ChangelogOnStart)
						ChangeScreen(MenuScreen.Changelog);
					else
						ChangeScreen(MenuScreen.Main);
				}

				break;

			case MenuScreen.Main:
				//Debug.Log("Current menu selection: " + _currentMainMenuButton);

				if (GameInputs.menuNavigate.down.IsPressed)
				{
					_previousMainMenuButton = _currentMainMenuButton;
					_currentMainMenuButton = Mathf.Clamp((++_currentMainMenuButton), 0, (MainMenuButtons.childCount - 1));

					for (int i = 0; i < MainMenuButtons.childCount; i++)
					{
						Transform buttonObject = MainMenuButtons.GetChild(i);

						if (_currentMainMenuButton != i)
						{
							buttonObject.GetComponent<EventTrigger>().OnDeselect(null);
						}
						else
						{
							buttonObject.GetComponent<EventTrigger>().OnSelect(null);
						}
					}

					if (_currentMainMenuButton != _previousMainMenuButton)
					{
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}

				if (GameInputs.menuNavigate.up.IsPressed)
				{
					_previousMainMenuButton = _currentMainMenuButton;
					_currentMainMenuButton = Mathf.Clamp((--_currentMainMenuButton), 0, (MainMenuButtons.childCount - 1));

					for (int i = 0; i < MainMenuButtons.childCount; i++)
					{
						Transform buttonObject = MainMenuButtons.GetChild(i);

						if (_currentMainMenuButton != i)
						{
							buttonObject.GetComponent<EventTrigger>().OnDeselect(null);
						}
						else
						{
							buttonObject.GetComponent<EventTrigger>().OnSelect(null);
						}
					}

					if (_currentMainMenuButton != _previousMainMenuButton)
					{
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}

				if (GameInputs.confirm.IsPressed && (_currentMainMenuButton >= 0))
				{
					MainMenuButtons.GetChild(_currentMainMenuButton).GetComponent<Button>().onClick?.Invoke();
					GameSFX.PlayGlobal(SFX_Confirm, transform, 1, -1);
				}

				break;

			case MenuScreen.LoadGame:

				break;
		}
	}

	private async UniTaskVoid LoadDebugSave()
	{
		//(bool ok, SaveData data) result = await SaveManager.Load(SaveManager.DEBUG_FILE_NAME);

		//if(SaveManager.TryLoad())

		/*if (result.ok)
		{
			// TODO
		}*/
	}

	private void StartNewGame_PubBuild(SaveFileID saveID, bool cutscene)
	{
		ChangeScreen(MenuScreen.StartingGame);
		GameController.NewGame_PubBuild(cutscene, saveID);
		SceneManager.UnloadSceneAsync(gameObject.scene);
	}

	private void StartNewGame_DebugBase(SaveFileID saveID)
	{
		ChangeScreen(MenuScreen.StartingGame);
		GameController.NewGame_DebugBase(saveID);
		SceneManager.UnloadSceneAsync(gameObject.scene);
	}


	private void StartNewGame_DebugMaxed(SaveFileID saveID)
	{
		ChangeScreen(MenuScreen.StartingGame);
		GameController.NewGame_DebugMaxed(saveID);
		SceneManager.UnloadSceneAsync(gameObject.scene);
	}



	public void OnHovered(TMP_Text ButtonText)
	{
		int target = 0;

		for (int i = 0; i < MainMenuButtons.childCount; i++)
		{
			Transform buttonObject = MainMenuButtons.GetChild(i);
			Button buttonComponent = buttonObject.GetComponent<Button>();

			if (buttonComponent.targetGraphic == ButtonText)
			{
				target = i;
			}

			buttonObject.GetComponent<EventTrigger>().OnDeselect(null);
		}

		ButtonText.fontSize = 75;

		Button button = ButtonText.transform.parent.GetComponent<Button>();
		ColorBlock colorBlock = button.colors;

		colorBlock.normalColor = ButtonHighlightColor;
		colorBlock.pressedColor = ButtonHighlightColor;
		colorBlock.selectedColor = ButtonHighlightColor;
		colorBlock.disabledColor = ButtonHighlightColor;
		colorBlock.highlightedColor = ButtonHighlightColor;

		button.colors = colorBlock;

		_previousMainMenuButton = _currentMainMenuButton;
		_currentMainMenuButton = target;

		if (_currentMainMenuButton != _previousMainMenuButton)
		{
			GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
		}
	}

	public void OnUnhovered(TMP_Text ButtonText)
	{
		ButtonText.fontSize = 70;

		Button button = ButtonText.transform.parent.GetComponent<Button>();
		ColorBlock colorBlock = button.colors;

		colorBlock.normalColor = ButtonUnhighlightColor;
		colorBlock.pressedColor = ButtonUnhighlightColor;
		colorBlock.selectedColor = ButtonUnhighlightColor;
		colorBlock.disabledColor = ButtonUnhighlightColor;
		colorBlock.highlightedColor = ButtonUnhighlightColor;

		button.colors = colorBlock;

		_currentMainMenuButton = -1;
	}

	public void OnSelected(TMP_Text ButtonText)
	{
		//EventSystem.current.SetSelectedGameObject(ButtonText.gameObject);

		ButtonText.fontSize = 75;

		Button button = ButtonText.transform.parent.GetComponent<Button>();
		ColorBlock colorBlock = button.colors;

		colorBlock.normalColor = ButtonHighlightColor;
		colorBlock.pressedColor = ButtonHighlightColor;
		colorBlock.selectedColor = ButtonHighlightColor;
		colorBlock.disabledColor = ButtonHighlightColor;
		colorBlock.highlightedColor = ButtonHighlightColor;

		button.colors = colorBlock;
	}

	public void OnDeselected(TMP_Text ButtonText)
	{
		//EventSystem.current.SetSelectedGameObject(null);

		ButtonText.fontSize = 70;

		Button button = ButtonText.transform.parent.GetComponent<Button>();
		ColorBlock colorBlock = button.colors;

		colorBlock.normalColor = ButtonUnhighlightColor;
		colorBlock.pressedColor = ButtonUnhighlightColor;
		colorBlock.selectedColor = ButtonUnhighlightColor;
		colorBlock.disabledColor = ButtonUnhighlightColor;
		colorBlock.highlightedColor = ButtonUnhighlightColor;

		button.colors = colorBlock;
	}

#region Callbacks

	public void CALLBACK_OnQuitConfirm()
	{
		#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

	public void ToggleYesColor(bool selected)
	{
		YesButton.color = (selected ? SelectedTextColor : UnselectedTextColor);
	}

	public void ToggleNoColor(bool selected)
	{
		NoButton.color = (selected ? SelectedTextColor : UnselectedTextColor);
	}

	public void CALLBACK_OnBackPressed()
	{
		ChangeScreen(MenuScreen.Main);
	}

	public void CALLBACK_OnDeletePressed()
	{
		DeleteConfirmPopupRoot.gameObject.SetActive(true);
	}

	public void CALLBACK_OnDeleteConfirm()
	{
		Assert.IsNotNull(_selectedPanel, "Trying to delete save but didn't set any save to delete first. How?!");
		//Assert.IsFalse(_selectedPanel.save == SaveManager.current, "Trying to delete the save currently in use which is undefined behavior.");

		// Delete the save
		SaveManager.DeleteFile(/*_selectedPanel.index,*/ _selectedPanel.save);

		//_savePaths.Remove(path);

		_selectedPanel.SetSave(null, _selectedPanel.index);

		// Remove the panel
		//int index = _savePanels.IndexOf(_selectedPanel);
		//_savePanels.RemoveAt(index);
		//Destroy(_selectedPanel.gameObject);

		//_selectedPanel = null;

		// Refresh UI
		// ----------------------------------------
		DeleteConfirmPopupRoot.SetActive(false);

		if (SaveManager.SaveDataCount == 0)
		{
			CALLBACK_OnBackPressed();
		}
	}

	public void CALLBACK_OnDeleteCancel()
	{
		DeleteConfirmPopupRoot.gameObject.SetActive(false);
		//EventSystem.current.SetSelectedGameObject(_selectedPanel.gameObject);

		//_selectedPanel = null;
	}

#endregion

#region Debug

	private SaveFileID _saveID = new SaveFileID {
		isNamed = false,
		name	= SaveManager.DEBUG_FILE_NAME,
		index   = -1
	};

	private void OnLayout(ref DebugSystem.State state)
	{
		// New game menu

		if (CurrentScreen == MenuScreen.NewGame)
		{
			ImGui.SetNextWindowPos(new Vector2(Screen.width / 2, Screen.height / 2), ImGuiCond.Always, Vector2.one * 0.5f);
			ImGui.SetNextWindowSize(new Vector2(380, 156), ImGuiCond.Always);
			//ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.Always);

			if (ImGui.Begin("New Game", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
			{
				ImGui.Text("Start New Game:");
				ImGui.Separator();
				ImGui.Text("Save ID:");
				ImGui.SameLine();
				ImGui.Checkbox("Is Named", ref _saveID.isNamed);

				ImGui.SameLine();
				if (_saveID.isNamed)
					ImGui.InputText("", ref _saveID.name, 64);
				else
					ImGui.InputInt("", ref _saveID.index);

				ImGui.Spacing();

				AImgui.Text("Prologue (Freeport):", ColorsXNA.Goldenrod);

				ImGui.SameLine();
				if (ImGui.Button("Normal"))
					StartNewGame_PubBuild(_saveID, true);

				ImGui.SameLine();
				if (ImGui.Button("No Intro Cutscene"))
					StartNewGame_PubBuild(_saveID, false);

				ImGui.Spacing();
				AImgui.Text("Debug:", ColorsXNA.Goldenrod);

				ImGui.SameLine();
				if (ImGui.Button("Base Stats"))
					StartNewGame_DebugBase(_saveID);

				ImGui.SameLine();
				if (ImGui.Button("Maxed Stats"))
					StartNewGame_DebugMaxed(_saveID);


				/*if (ImGui.BeginChild("controls", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()))) {

				}
				ImGui.EndChild();


				if (ImGui.Button("Start")) {
					StartNewGame();
				}*/

				ImGui.End();
			}
		} else if (CurrentScreen == MenuScreen.Changelog) {

			float w = Screen.width;
			float h = Screen.height;

			float mw = w / 1.25f;
			float mh = h / 1.5f;

			ImGui.SetNextWindowPos(new Vector2((w / 2) - mw / 2, (h / 2) - mh / 2), ImGuiCond.Always);
			ImGui.SetNextWindowSize(new Vector2(mw, mh), ImGuiCond.Always);

			if (ImGui.Begin("Changelog", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)) {

				if (GameAssets.Live.Changelog_Internal) {
					ImGui.BeginChild("message", new Vector2(0, mh - 48), false);
					foreach (string line in GameAssets.Live.ChangelogLines) {
						ImGui.TextWrapped(line);
					}
					//ImGui.TextUnformatted(GameAssets.Live.Changelog_Internal.text);
					ImGui.EndChild();
				} else {
					ImGui.Text("No Changelog Asset Set in GameAssets");
				}

				if (ImGui.Button("Close (Esc/A)") || Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame)
					ChangeScreen(MenuScreen.Main);

				ImGui.End();
			}
		}
	}

#endregion
}
using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using SaveFiles;
using UnityEngine;
using UnityEngine.UI;
using Anjin.EditorUtility;
using TMPro;
using System.Collections;
using Anjin.Util;
using UnityEngine.Rendering.PostProcessing;

namespace Menu.LoadSave
{
	public class LoadSaveMenu : StaticMenu<LoadSaveMenu>
	{
		private enum DataMode
		{
			Load,
			Save
		}

		public struct Settings {
			public bool post_process;
			public bool graident;
		}

		[SerializeField] private float ScrollDelay = 0.3f;

		[SerializeField] private Color SelectedTextColor;
		[SerializeField] private Color UnselectedTextColor;

		[SerializeField] private GameObject SaveEntryDisplay;
		[SerializeField] private GameObject SaveConfirmationPrompt;

		[SerializeField] private TextMeshProUGUI SaveYesButtonLabel;
		[SerializeField] private TextMeshProUGUI SaveNoButtonLabel;

		[SerializeField] InputButtonLabel SaveConfirmButtonLabel;
		[SerializeField] InputButtonLabel SaveCancelButtonLabel;

		[SerializeField] private Button BackButton;

		[SerializeField] private ScrollRect SaveEntryScrollRect;

		[SerializeField] private SaveDataPanel SaveEntryPrefab;

		[SerializeField] private List<GameObject> GradientObjects;

		[SerializeField] private List<TextMeshProUGUI> MenuTitles;

		[SerializeField] AudioDef SFX_InvalidChoice;
		[SerializeField] AudioDef SFX_Scroll;
		[SerializeField] AudioDef SFX_Confirm;
		[SerializeField] AudioDef SFX_Cancel;
		[SerializeField] AudioDef SFX_Save;

		[SerializeField] LevelManifest FreeportManifest;

		public PostProcessVolume PostProcess;

		public Action OnLoad;

		private static DataMode _currentMode;

		private SaveDataPanel _selectedPanel;

		private int _currentFileSlot;

		private bool _canScroll;
		private bool _loadingMenu;
		private bool _wheelScrolling;

		private ScrollRectLerper scrollLerper;

		private List<SaveDataPanel> _savePanels;

		private List<SaveData> _loadedSaves;

		private Settings  _defaultSettings;
		private Settings? _settings;

		public static void PrepMenu(bool saving)
		{
			_currentMode = (saving ? DataMode.Save : DataMode.Load);
		}

		protected override void OnAwake()
		{
			BackButton.onClick.AddListener(OnExitStatic);

			_currentMode = DataMode.Save;
			_savePanels = new List<SaveDataPanel>();
			scrollLerper = SaveEntryScrollRect.GetComponent<ScrollRectLerper>();

			_loadedSaves = new List<SaveData>();

			SaveConfirmButtonLabel.Button = GameInputs.confirm;
			SaveCancelButtonLabel.Button = GameInputs.cancel;

			_defaultSettings = new Settings {
				graident     = true,
				post_process = true,
			};
		}

		public void ToggleSaveYesColor(bool selected)
		{
			SaveYesButtonLabel.color = (selected ? SelectedTextColor : UnselectedTextColor);
		}

		public void ToggleSaveNoColor(bool selected)
		{
			SaveNoButtonLabel.color = (selected ? SelectedTextColor : UnselectedTextColor);
		}

		public void PerformSavePromptAction(bool save)
		{
			if (save)
			{
				GameSFX.PlayGlobal(SFX_Save, transform, 1, -1);

				SaveManager.UpdateSaveWithGlobalData(SaveManager.current);
				SaveManager.CopySaveWithNewID(_selectedPanel.index, SaveManager.current, out SaveData newData, true);
				SaveManager.Set(newData, SetSaveAction.CopyCurrent);

				_selectedPanel.SetSave(newData, _selectedPanel.index);
			}
			else
			{
				GameSFX.PlayGlobal(SFX_Cancel, transform, 1, -1);
			}

			SaveConfirmationPrompt.SetActive(false);
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

					SaveEntryScrollRect.ScrollToWithLerp(_selectedPanel.GetComponent<RectTransform>());
				}
			}
		}

		public void Setup(Settings settings) => _settings = settings;

		protected override async UniTask enableMenu()
		{
			Settings settings = _settings ?? _defaultSettings;

			_wheelScrolling = false;
			_canScroll = true;
			_currentFileSlot = 0;
			_loadingMenu = true;

			SaveConfirmationPrompt.SetActive(false);

			for (int i = 0; i < MenuTitles.Count; i++)
			{
				TextMeshProUGUI menuTitle = MenuTitles[i];
				menuTitle.text = (_currentMode == DataMode.Save ? "Save Data" : "Load Data");
				menuTitle.gameObject.SetActive(true);
			}

			for (int i = 0; i < GradientObjects.Count; i++)
			{
				GradientObjects[i].SetActive(settings.graident);
			}

			PostProcess.weight = settings.post_process ? 1 : 0;

			SaveManager.RefreshIDsOnDisk();
			_loadedSaves.Clear();

			for (int i = 0; i < SaveManager.MAX_SAVE_SLOTS; i++) {
				_loadedSaves.Add(null);

				if (SaveManager.NumberedFilesOnDisk.TryGetValue(i, out SaveFileID save)) {
					if (SaveManager.TryLoad(save, out SaveData data))
						_loadedSaves[i] = data;
				}
			}

			for (int i = 0; i < SaveManager.MAX_SAVE_SLOTS; i++)
			{
				SaveDataPanel panel = Instantiate(SaveEntryPrefab, Vector3.zero, Quaternion.identity, SaveEntryDisplay.transform);

				Vector3 panelPosition = panel.GetComponent<RectTransform>().localPosition;
				panelPosition.z = 0;
				panel.GetComponent<RectTransform>().localPosition = panelPosition;

				panel.onSelected = p =>
				{
					if (!_loadingMenu)
					{
						ChangePanels(p);
					}
				};

				SaveData save = _loadedSaves.SafeGet(i);
				panel.SetSave(save, i);

				panel.onConfirmed = p =>
				{
					if (_currentMode == DataMode.Save)
					{
						if (!SaveManager.LoadedFilesOnDisk.TryGet(p.index, out SaveData data)) {
							GameSFX.PlayGlobal(SFX_Save, transform, 1, -1);

							SaveManager.UpdateSaveWithGlobalData(SaveManager.current);
							SaveManager.CopySaveWithNewID(p.index, SaveManager.current, out SaveData newData, true);
							SaveManager.Set(newData);
							panel.SetSave(newData, p.index);
						}
						else
						{
							GameSFX.PlayGlobal(SFX_Confirm, transform, 1, -1);
							SaveConfirmationPrompt.SetActive(true);
						}
					}
					else
					{
						if (p.save != null) {
							if (!GameController.Live.LoadGameFromSaveData(p.save)) {
								this.LogError("Could not load save");
							}

							GameSFX.PlayGlobal(SFX_Confirm, transform, 1, -1);

							OnLoad?.Invoke();
							OnLoad = null;

							//OnExitStatic();
							DisableMenu();
							SplicerHub.DisableMenu();
							MenuManager.activeMenus.Clear();

							//SaveManager.Set(p.save);
							//GameController.Live.ChangeLevel(FreeportManifest, 999999);
						}
						else
						{
							GameSFX.PlayGlobal(SFX_InvalidChoice);
						}
					}
				};

				panel.onDelete = p =>
				{
					//_savePendingDelete = p;

					//DeleteConfirmPopupRoot.gameObject.SetActive(true);
					//EventSystem.current.SetSelectedGameObject(DeleteConfirmPopupYes);
				};

				_savePanels.Add(panel);
			}

			SaveEntryScrollRect.content.anchoredPosition = Vector2.zero;
			SaveEntryScrollRect.verticalNormalizedPosition = 1;

			_selectedPanel = _savePanels[0];
			_selectedPanel.ImmediatelySelect();

			BackButton.gameObject.SetActive(true);

			StartCoroutine(WaitToSignifyFinish());

			await UniTask.CompletedTask;
		}

		protected override async UniTask disableMenu()
		{
			_loadedSaves.Clear();

			OnLoad = null;

			scrollLerper.StopLerping();

			while (SaveEntryDisplay.transform.childCount > 0)
			{
				Transform entry = SaveEntryDisplay.transform.GetChild(0);
				entry.SetParent(null);
				Destroy(entry.gameObject);
			}

			for (int i = 0; i < MenuTitles.Count; i++)
			{
				MenuTitles[i].gameObject.SetActive(false);
			}

			for (int i = 0; i < GradientObjects.Count; i++)
			{
				GradientObjects[i].SetActive(false);
			}

			_savePanels.Clear();

			BackButton.gameObject.SetActive(false);

			_settings = null;

			await UniTask.CompletedTask;
		}

		private IEnumerator ImplementScrollDelay()
		{
			_canScroll = false;

			yield return new WaitForSeconds(ScrollDelay);

			_canScroll = true;
		}

		private IEnumerator WaitToSignifyFinish()
		{
			yield return new WaitForSeconds(1f);

			_loadingMenu = false;
		}

		private void Update()
		{
			float scrollAmount = GameInputs.scrollWheel.Vertical;

			if (scrollAmount == 0)
			{
				_wheelScrolling = false;

				if (GameInputs.menuNavigate.down.IsPressed || GameInputs.menuNavigate.down.IsHeld(0.3f))
				{
					int newIndex = Mathf.Clamp(_currentFileSlot + 1, 0, 9);

					if (newIndex != _currentFileSlot)
					{
						_savePanels[newIndex].onSelected?.Invoke(_savePanels[newIndex]);
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}
				else if (GameInputs.menuNavigate.up.IsPressed || GameInputs.menuNavigate.up.IsHeld(0.3f))
				{
					int newIndex = Mathf.Clamp(_currentFileSlot - 1, 0, 9);

					if (newIndex != _currentFileSlot)
					{
						_savePanels[newIndex].onSelected?.Invoke(_savePanels[newIndex]);
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}
			}
			else
			{
				if (!_wheelScrolling)
				{
					_wheelScrolling = true;
					int newIndex;

					if (scrollAmount < 0)
					{
						newIndex = Mathf.Clamp(_currentFileSlot + 1, 0, 9);
					}
					else
					{
						newIndex = Mathf.Clamp(_currentFileSlot - 1, 0, 9);
					}

					if (newIndex != _currentFileSlot)
					{
						_savePanels[newIndex].onSelected?.Invoke(_savePanels[newIndex]);
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}
			}

			if (GameInputs.confirm.IsPressed)
			{
				if (_currentMode == DataMode.Load)
				{
					_selectedPanel.onConfirmed?.Invoke(_selectedPanel);
				}
				else
				{
					if (SaveConfirmationPrompt.gameObject.activeSelf)
					{
						PerformSavePromptAction(true);
					}
					else
					{
						_selectedPanel.onConfirmed?.Invoke(_selectedPanel);
					}
				}
			}

			if (GameInputs.cancel.IsPressed)
			{
				if (_currentMode == DataMode.Load)
				{
					OnExitStatic();
				}
				else
				{
					if (SaveConfirmationPrompt.gameObject.activeSelf)
					{
						PerformSavePromptAction(false);
					}
					else
					{
						OnExitStatic();
					}
				}
			}
		}
	}
}

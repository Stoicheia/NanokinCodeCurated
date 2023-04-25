using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Cysharp.Threading.Tasks;
using Overworld.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MenuController = Anjin.Nanokin.MenuManager;

public struct TutorialEntryInfo
{
	public string Title;
	public string Flag;
	public string Address;
}

public class TutorialMenu : StaticMenu<TutorialMenu>
{
	public enum States
	{
		Regular,
		SubMenu
	}

	[SerializeField] private float ScrollDelay = 0.3f;

	[SerializeField] private GameObject TutorialEntryDisplay;

	[SerializeField] private Button BackButton;

	[SerializeField] private ScrollRect TutorialEntryScrollRect;

	[SerializeField] private TutorialPanel TutorialEntryPrefab;

	[SerializeField] private AudioDef SFX_InvalidChoice;
	[SerializeField] private AudioDef SFX_Scroll;
	[SerializeField] private AudioDef SFX_OpenSubmenu;
	[SerializeField] private AudioDef SFX_ExitSubmenu;

	[SerializeField] private List<TutorialEntryInfo> tutorialEntries;

	private bool _canScroll;
	private bool _loadingMenu;
	private bool _wheelScrolling;

	private int _currentTutorialSlot;

	private States _state;

	private ScrollRectLerper scrollLerper;

	private TutorialPanel _selectedPanel;

	private List<TutorialPanel> _tutorialPanels;

	protected override void OnAwake()
	{
		BackButton.onClick.AddListener(OnExitStatic);

		_tutorialPanels = new List<TutorialPanel>();
		scrollLerper = TutorialEntryScrollRect.GetComponent<ScrollRectLerper>();
	}

	protected override async UniTask enableMenu()
	{
		_wheelScrolling = false;
		_canScroll = true;
		_currentTutorialSlot = 0;
		_loadingMenu = true;

		for (int i = 0; i < tutorialEntries.Count; i++)
		{
			TutorialPanel panel = Instantiate(TutorialEntryPrefab, Vector3.zero, Quaternion.identity, TutorialEntryDisplay.transform);

			Vector3 panelPosition = panel.GetComponent<RectTransform>().localPosition;
			panelPosition.z = 0;
			panel.GetComponent<RectTransform>().localPosition = panelPosition;

			TutorialEntryInfo info = tutorialEntries[i];
			panel.Initialize(i, info);

			panel.onSelected = p =>
			{
				if (!_loadingMenu)
				{
					ChangePanels(p);
				}
			};

			panel.onConfirmed = p =>
			{
				if (p.Viewed)
				{
					GameSFX.PlayGlobal(SFX_OpenSubmenu, transform, 1, -1);
					ShowSplashScreen(p.Address).Forget();
				}
				else
				{
					GameSFX.PlayGlobal(SFX_InvalidChoice);
				}
			};

			_tutorialPanels.Add(panel);
		}

		TutorialEntryScrollRect.content.anchoredPosition = Vector2.zero;
		TutorialEntryScrollRect.verticalNormalizedPosition = 1;

		_selectedPanel = _tutorialPanels[0];
		_selectedPanel.ImmediatelySelect();

		BackButton.gameObject.SetActive(true);

		StartCoroutine(WaitToSignifyFinish());

		if (_state != States.Regular)
		{
			ChangeState(States.Regular);
		}

		await UniTask.CompletedTask;
	}

	protected override async UniTask disableMenu()
	{
		scrollLerper.StopLerping();

		while (TutorialEntryDisplay.transform.childCount > 0)
		{
			Transform entry = TutorialEntryDisplay.transform.GetChild(0);
			entry.SetParent(null);
			Destroy(entry.gameObject);
		}

		_selectedPanel = null;
		_tutorialPanels.Clear();

		await UniTask.CompletedTask;
	}

	private async UniTask ShowSplashScreen(string address)
	{
		GameSFX.PlayGlobal(SFX_OpenSubmenu, this);

		//OverworldHUD.HideCredits();

		ChangeState(States.SubMenu);

		await MenuController.SetSplicerBG(true);

		await SplashScreens.ShowPrefabAsync(address, async () =>
		{
			GameSFX.PlayGlobal(SFX_ExitSubmenu, this);

			//OverworldHUD.ShowCredits();
			ChangeState(States.Regular);
		});
	}

	private void ChangePanels(TutorialPanel p)
	{
		if (p != _selectedPanel)
		{
			if (_canScroll)
			{
				StartCoroutine(ImplementScrollDelay());

				if (_selectedPanel != null)
				{
					_selectedPanel.ToggleColor(false);
					//_selectedPanel.TogglePosition(false);
				}

				_selectedPanel = p;
				_selectedPanel.ToggleColor(true);
				//_selectedPanel.TogglePosition(true);
				_currentTutorialSlot = p.ID;

				TutorialEntryScrollRect.ScrollToWithLerp(_selectedPanel.GetComponent<RectTransform>());
			}
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

	private void Update()
	{
		float scrollAmount = GameInputs.scrollWheel.Vertical;

		if (scrollAmount == 0)
		{
			_wheelScrolling = false;

			if ((GameInputs.menuNavigate.down.IsPressed || GameInputs.menuNavigate.down.IsHeld(0.3f)) && (_currentTutorialSlot < (tutorialEntries.Count - 1)))
			{
				int newIndex = Mathf.Clamp(_currentTutorialSlot + 1, 0, (tutorialEntries.Count - 1));

				if (newIndex != _currentTutorialSlot)
				{
					_tutorialPanels[newIndex].onSelected?.Invoke(_tutorialPanels[newIndex]);
					GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
				}
			}
			else if ((GameInputs.menuNavigate.up.IsPressed || GameInputs.menuNavigate.up.IsHeld(0.3f)) && (_currentTutorialSlot > 0))
			{
				int newIndex = Mathf.Clamp(_currentTutorialSlot - 1, 0, (tutorialEntries.Count - 1));

				if (newIndex != _currentTutorialSlot)
				{
					_tutorialPanels[newIndex].onSelected?.Invoke(_tutorialPanels[newIndex]);
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
					if (_currentTutorialSlot < (tutorialEntries.Count - 1))
					{
						newIndex = Mathf.Clamp(_currentTutorialSlot + 1, 0, (tutorialEntries.Count - 1));
						_tutorialPanels[newIndex].onSelected?.Invoke(_tutorialPanels[newIndex]);
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}
				else
				{
					if (_currentTutorialSlot > 0)
					{
						newIndex = Mathf.Clamp(_currentTutorialSlot - 1, 0, (tutorialEntries.Count - 1));
						_tutorialPanels[newIndex].onSelected?.Invoke(_tutorialPanels[newIndex]);
						GameSFX.PlayGlobal(SFX_Scroll, transform, 1, -1);
					}
				}
			}
		}

		if (GameInputs.confirm.IsPressed)
		{
			_selectedPanel.onConfirmed?.Invoke(_selectedPanel);
		}

		if (GameInputs.cancel.IsPressed)
		{
			OnExitStatic();
		}
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
}

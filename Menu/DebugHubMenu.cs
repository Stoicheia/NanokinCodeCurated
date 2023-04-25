using System;
using Anjin.Utils;
using Combat;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Anjin.Nanokin
{
	public class DebugHubMenu : SerializedMonoBehaviour
	{
		[SerializeField] private GameObject _uiContainer;
		[SerializeField] private Button     _btnOverworldLevelMenu;
		[SerializeField] private Button     _btnSplicerMenu;
		[SerializeField] private Button     _btnBattleNetworkingMenu;

		[SerializeField] private SceneReference _hubScene;
		[SerializeField] private SceneReference _overworldLevelPickerScene;
		[SerializeField] private SceneReference _battleNetworkScene;

		private void Start()
		{
			_btnOverworldLevelMenu.onClick.AddListener(OnOverworldLevelMenuButton);
			_btnSplicerMenu.onClick.AddListener(OnSplicerMenuButton);
			_btnBattleNetworkingMenu.onClick.AddListener(OnBattleNetworkingMenuButton);
		}

		private void OnSplicerMenuButton()
		{
			_uiContainer.SetActive(false);

			MenuManager.LoadMenu(Menus.SplicerBarrel);
			MenuManager.SetMenu(Menus.SplicerBarrel, true);
			// SplicerMenu.Live.OpenMenu();
		}

		private void OnOverworldLevelMenuButton()
		{
			CloseDebugMenu(() =>
			{
				SceneLoader.Load(_overworldLevelPickerScene);
			});
		}

		private void OnBattleNetworkingMenuButton()
		{
			CloseDebugMenu(() =>
			{
				SceneLoader.Load(_battleNetworkScene).OnDriver<BattleClient>(client =>
					{
						client.onExit += () => SceneLoader.Load(_hubScene);
					}
				);
			});
		}

		private void CloseDebugMenu(Action action)
		{
			SceneLoader.Unload(gameObject.scene).OnDone(action);
		}
	}
}
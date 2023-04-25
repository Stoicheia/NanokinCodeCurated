using System;
using System.Collections.Generic;
using Anjin.Util;
using Anjin.Utils;
using Cysharp.Threading.Tasks;
using Menu.Formation;
using Menu.LoadSave;
using Menu.Quest;
using Menu.Start;
using Menu.Sticker;
using Overworld.UI.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Nanokin
{
	public class MenuManager : StaticBoy<MenuManager>
	{
		/// <summary>
		/// When this set has values, the player cannot move in the overworld.
		/// A simple integer could be used, but a set of named strings can facilitate debugging.
		/// </summary>
		public static HashSet<Menus> activeMenus = new HashSet<Menus>();

		public static List<IMenuComponent> activeMenuComponents = new List<IMenuComponent>();

		[Obsolete, SerializeField] private SceneReference SplicerBackgroundScene;
		[Obsolete, SerializeField] private SceneReference SplicerHubScene;
		[Obsolete, SerializeField] private SceneReference SplicerBarrelScene;
		[Obsolete, SerializeField] private SceneReference LimbMenuScene;
		[Obsolete, SerializeField] private SceneReference StickerMenuScene;
		[Obsolete, SerializeField] private SceneReference FormationMenuScene;
		[Obsolete, SerializeField] private SceneReference QuestMenuScene;

		private static Scene _splicerBackgroundScene;
		private static Scene _gameMenuScene;
		private static Scene _limbScene, _stickerScene, _splicerScene, _formationScene, _questScene, _loadSaveScene, _tutorialScene; // splicer menus

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			activeMenus.Clear();
			activeMenuComponents.Clear();
		}

		public static async UniTask LoadSplicerBackground()
		{
			if (_splicerBackgroundScene.isLoaded)
				// Already loaded.
				return;

			Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_UI_Splicer_BG);
			_splicerBackgroundScene = scene;
		}


		public static async UniTask SetSplicerBG(bool state)
		{
			await LoadSplicerBackground();
			// ReSharper disable once PossibleNullReferenceException
			_splicerBackgroundScene.Set(state && GameOptions.current.splicer_hub_backdrop);
		}

		public static async UniTask LoadMenu(Menus menu)
		{
			switch (menu)
			{
				case Menus.System:
				{
					break;
				}

				case Menus.SplicerHub:
					_splicerScene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Splicer_Hub);
					break;

				case Menus.SplicerBarrel:
				{
					_gameMenuScene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_UI_Splicer_Barrel);
					break;
				}

				case Menus.EquipLimb:
				{
					if (_limbScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Limb);
					_limbScene = scene;

					break;
				}

				case Menus.Sticker:
				{
					if (_stickerScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Stickers);
					_stickerScene = scene;

					break;
				}

				case Menus.Formation:
				{
					if (_formationScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Formation);
					_formationScene = scene;


					break;
				}

				case Menus.Quests:
				{
					if (_questScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Quest);

					_questScene = scene;
					_questScene.SetRootActive(false);

					break;
				}

				case Menus.Save:
				{
					if (_loadSaveScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_LoadSave);
					_loadSaveScene = scene;

					break;
				}

				case Menus.Load:
				{
					if (_loadSaveScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_LoadSave);
					_loadSaveScene = scene;

					break;
				}

				case Menus.Tutorial:
				{
					if (_tutorialScene.isLoaded) return;

					Scene scene = await SceneLoader.GetOrLoadAsync(Addresses.Scene_MENU_Tutorial);
					_tutorialScene = scene;

					break;
				}

				case Menus.Settings:
				{
					break;
				}

				default:
				{
					throw new ArgumentOutOfRangeException(nameof(menu), menu, null);
				}
			}
		}

		public static async UniTask SetMenu(Menus menu, bool state = true)
		{
			activeMenus.Remove(menu);
			if (state)
			{
				activeMenus.Add(menu);

				// Load the menu so we can use it
				await LoadMenu(menu);
			}

			switch (menu)
			{
				case Menus.System:
					break;

				case Menus.SplicerHub:
					if (state)
						await SplicerHub.EnableMenu();
					else
						await SplicerHub.DisableMenu();
					break;

				case Menus.SplicerBarrel:
					if (state)
						await SplicerBarrel.EnableMenu();
					else
						await SplicerBarrel.DisableMenu();
					break;

				case Menus.EquipLimb:
					await LimbMenu.SetState(state);
					break;

				case Menus.Sticker:
					if (state)
						await StickerMenu.EnableMenu();
					else
						await StickerMenu.DisableMenu();
					break;

				case Menus.Formation:
					if (state)
						await FormationMenu.EnableMenu();
					else
						await FormationMenu.DisableMenu();

					break;

				case Menus.Quests:
					if (state)
						await QuestMenu.EnableMenu();
					else
						await QuestMenu.DisableMenu();

					break;

				case Menus.Save:
					if (state)
					{
						Menu.LoadSave.LoadSaveMenu.PrepMenu(true);
						await LoadSaveMenu.EnableMenu();
					}
					else
						await LoadSaveMenu.DisableMenu();

					break;

				case Menus.Load:
					if (state)
					{
						Menu.LoadSave.LoadSaveMenu.PrepMenu(false);
						await LoadSaveMenu.EnableMenu();
					}
					else
						await LoadSaveMenu.DisableMenu();

					break;

				case Menus.Tutorial:
					if (state)
						await TutorialMenu.EnableMenu();
					else
						await TutorialMenu.DisableMenu();
					break;

				case Menus.Settings:
					if (state)
						await SettingsMenu.EnableMenu();
					else
						await SettingsMenu.DisableMenu();

					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(menu), menu, null);
			}
		}

		public static void DisableMenu(Menus menu) => activeMenus.Remove(menu);
	}


	public enum Menus
	{
		/// <summary>
		/// A simple and generic pause menu accessed through ESCAPE or START.
		/// Accessible at any point in the game, whether in cutscenes or battle.
		/// - Resume: Resume the game.
		/// - Options: Access a submenu with the game's options.
		/// - Shutdown: Close the game completely and return to desktop.
		/// </summary>
		System,

		/// <summary>
		/// The in-universe splicer menu which appears as a ring around the player.
		/// Accessible only in overworld when the player has control over Nas.
		/// - Splice
		/// - Formation
		/// - Stickers
		/// - Nanopedia
		/// </summary>
		SplicerHub,

		SplicerBarrel,

		/// <summary>
		/// The limb menu which allows changing limbs on nanokins. (known as 'splice' in the game)
		/// </summary>
		EquipLimb,

		/// <summary>
		/// The sticker menu which allows changing stickers on nanokin.
		/// </summary>
		Sticker,

		/// <summary>
		/// The quest menu to view the quests and objectives and stuff.
		/// </summary>
		Quests,

		/// <summary>
		/// The formation menu which allows modifying the team's initial formation in combat.
		/// </summary>
		Formation,

		Settings,

		Load,

		Save,

		Tutorial
	}
}
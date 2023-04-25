using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Anjin.EditorUtility;
using Anjin.EditorUtility.UIShape;
using Anjin.Nanokin;
using Anjin.Nanokin.Core.Options;
using Anjin.UI;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using Util;
using Util.Odin.Attributes;
using Random = UnityEngine.Random;

namespace Overworld.UI.Settings
{
	public class SettingsMenu : StaticMenu<SettingsMenu>
	{
		private class CategoryInfo
		{
			public string Category { get; private set; }

			private Dictionary<string, ActionInfo> actionInfo;

			public CategoryInfo(string category)
			{
				Category = category;

				actionInfo = new Dictionary<string, ActionInfo>();
			}

			public void RegisterAction(string action, GameInputs.IHasInputAction _action)
			{
				if (!actionInfo.ContainsKey(action))
				{
					actionInfo.Add(action, new ActionInfo(Category));
					actionInfo[action].InspectAction(_action, action);
				}
			}

			//public void UpdateAction(string name, string oldMapping, GameInputs.IHasInputAction _action)
			//{
			//	if (actionInfo.ContainsKey(name))
			//	{
			//		actionInfo[name].UpdateAction(oldMapping, _action);
			//	}
			//}

			public bool CheckForOverrides()
			{
				foreach (var info in actionInfo.Values)
				{
					if (!string.IsNullOrEmpty(info.Action.SaveBindingOverridesAsJson()))
					{
						return true;
					}
				}

				return false;
			}

			public ActionInfo GetActionInfo(string action)
			{
				return (actionInfo.ContainsKey(action) ? actionInfo[action] : null);
			}

			public bool FindDuplicate(string mapping, out BindingInfo bindingInfo)
			{
				bool found = false;
				bindingInfo = null;

				foreach (var info in actionInfo)
				{
					var result = info.Value.GetBindingInfoForMapping(mapping);

					if (result != null)
					{
						bindingInfo = result;
						found = true;

						break;
					}
				}

				return found;
			}
		}

		private class ActionInfo
		{
			public string Category { get; private set; }

			public InputAction Action { get; private set; }

			private Dictionary<string, BindingInfo> bindingInfo;

			public ActionInfo(string category)
			{
				Category = category;

				bindingInfo = new Dictionary<string, BindingInfo>();
			}

			public void InspectAction(GameInputs.IHasInputAction _action, string name)
			{
				Action = _action.InputAction;

				int i = 0;

				int bindingCount = Action.bindings.Count;

				InputBinding binding;
				string path;
				string platform;
				string group;
				string mapping;
				string[] tokens;

				while (i < bindingCount)
				{
					binding = Action.bindings[i];
					group = binding.groups;

					if (!binding.isComposite)
					{
						path = binding.effectivePath;
						tokens = path.Split(GameInputs.PathDelimeter, StringSplitOptions.RemoveEmptyEntries);
						platform = tokens[0];
						mapping = tokens[(tokens.Length - 1)];

						if (!bindingInfo.ContainsKey(mapping))
						{
							bindingInfo.Add(mapping, new BindingInfo(Category, Action, i, name, platform, group, mapping));
						}

						++i;
					}
					else
					{
						binding = Action.bindings[++i];
						group = binding.groups;

						while (binding.isPartOfComposite)
						{
							path = binding.effectivePath;
							tokens = path.Split(GameInputs.PathDelimeter, StringSplitOptions.RemoveEmptyEntries);
							platform = tokens[0];
							mapping = tokens[(tokens.Length - 1)];

							if (!bindingInfo.ContainsKey(mapping))
							{
								bindingInfo.Add(mapping, new BindingInfo(Category, Action, i, name, platform, group, mapping));
							}

							++i;

							if (i >= bindingCount)
							{
								break;
							}

							binding = Action.bindings[i];
						}
					}
				}
			}

			//public void UpdateAction(string oldMapping, GameInputs.IHasInputAction _action)
			//{
			//	InputAction action = _action.InputAction;

			//	if (bindingInfo.ContainsKey(oldMapping))
			//	{
			//		//InputAction toReplace
			//	}
			//}

			public BindingInfo GetBindingInfoForMapping(string mapping)
			{
				return (bindingInfo.ContainsKey(mapping) ? bindingInfo[mapping] : null);
			}

			public List<BindingInfo> GetBindingInfoForPlatform(string platform)
			{
				var results = bindingInfo.Where(x => x.Value.Platform == platform).Select(x => x.Value).ToList();

				return ((results.Count > 0) ? results : null);
			}
		}

		public class BindingInfo
		{
			public int Index { get; private set; }

			public string Category { get; private set; }
			public string Name { get; private set; }
			public string Mapping { get; private set; }
			public string Group { get; private set; }
			public string Platform { get; private set; }

			public InputAction Action { get; private set; }

			public InputBinding Binding => ((Index != -1) ? Action.bindings[Index] : default(InputBinding));

			public BindingInfo(string category, InputAction action, int index, string name, string platform, string group, string mapping)
			{
				this.Index = index;

				Category = category;
				Name = name;
				Action = action;
				Platform = platform.Replace("<", "").Replace(">", "");
				Group = group;
				Mapping = mapping;
			}

			public void UpdateAction(InputAction action)
			{
				Action = action;
			}
		}

		// Main Menu
		//----------------------------------------------------------
		public HUDElement MenuElement;

		public GraphicRaycaster Raycaster;
		public ScrollRect       ScrollRect;
		public Button           BackButton;

		public InputButtonLabel LeftMovementLabel;
		public InputButtonLabel RightMovementLabel;

		// TODO: Move these to prefabs that can be reused for other things in-game...?
		public SettingsMenuHeader		Template_Header;
		public CheckboxControl			Template_Checkbox;
		public SliderControl			Template_Slider;
		public DropdownControl			Template_Dropdown;
		public InputFieldControl		Template_InputField;
		public SingleRebindControl      Template_SingleRebind;
		public MultiRebindControl		Template_MultiRebind;
		public RebindSectionTitle		Template_RebindSectionTitle;
		public ButtonControl			Template_Button;

		public SettingsMenuTab  Template_Tab;
		public UIRectangleShape TabHighlighter;
		public UIRectangleShape SettingHighlighter;

		public RectTransform ControlsRoot;
		public RectTransform PoolingRoot;
		public RectTransform TabsRoot;

		[ShowInPlay, NonSerialized]
		private List<ISettingsMenuControl> ActiveMenuControls;

		[ShowInPlay, NonSerialized]
		private List<ISettingsMenuControl> ActiveRebindControls;

		[ShowInPlay, NonSerialized]
		private Dictionary<Page, PageItems> _pages;
		private Dictionary<Page, int> _controlIndexTracker;
		private List<Page> _pagesInOrder;

		[ShowInPlay]
		private Submenu _submenu;

		private Page _page;
		private int  _selectedPage;

		//[ShowInPlay]
		//private List<SettingsMenuTab> _tabs;

		[SerializeField] private List<SettingsPageTab> tabs;

		[ShowInPlay, NonSerialized] public ComponentPool<SettingsMenuHeader> Pool_Header;
		[ShowInPlay, NonSerialized] public ComponentPool<CheckboxControl>    Pool_Checkbox;
		[ShowInPlay, NonSerialized] public ComponentPool<SliderControl>      Pool_Slider;
		[ShowInPlay, NonSerialized] public ComponentPool<DropdownControl>    Pool_Dropdown;
		[ShowInPlay, NonSerialized] public ComponentPool<InputFieldControl>  Pool_InputField;
		[ShowInPlay, NonSerialized] public ComponentPool<SingleRebindControl>      Pool_Rebind;
		[ShowInPlay, NonSerialized] public ComponentPool<MultiRebindControl> Pool_MultiRebind;
		[ShowInPlay, NonSerialized] public ComponentPool<RebindSectionTitle> Pool_RebindSectionTitle;
		[ShowInPlay, NonSerialized] public ComponentPool<ButtonControl>      Pool_Buttons;

		// Rebind Menu
		//----------------------------------------------------------
		public HUDElement RebindElement;
		public HUDElement RebindPopupElement;

		public GameObject PreRebindMenu;
		public GameObject StartRebindMenu;
		public GameObject ResetDefaultsPopup;

		public GameObject RestoreDefaultsButtonHighlight;

		public ScrollRect      RebindScrollRect;
		public TextMeshProUGUI PreRebindPopupLabel;
		public TextMeshProUGUI StartRebindPopupLabel;

		public ButtonControl   RestoreDefaultsButton;

		//public Util.RenderingElements.PointBars.PointBar RebindCountdown;
		public Image RebindCountdown;

		//public TextMeshProUGUI PanelTitleMain;
		//public TextMeshProUGUI PanelTitleBacking;

		public RectTransform RebindControlsRoot;
		public RebindMode    RebindingMode;

		public UIRectangleShape RebindHighlighter;

		[ShowInPlay, NonSerialized]
		private List<UnityEngine.Object> ActiveRebindUI;

		[ShowInPlay, NonSerialized]
		private InputActionRebindingExtensions.RebindingOperation _currentRebindOperation;

		[SerializeField] private AudioDef _sfxScrollNext;

		[SerializeField] private List<AudioSource> audioSettingSources;

		private ISettingsMenuControl currentControl;

		private BindingInfo infoToRebind;

		private System.Collections.IEnumerator rebindCancel;

		/// <summary>
		/// Used to store the current control schemes for each rebind category (Overworld, Combat, etc.) and info for each input binding (action-info pairing)
		/// </summary>
		private Dictionary<string, CategoryInfo> inputBindings;

		private Dictionary<string, RebindControl> rebindControlRows;

		public delegate void MenuEvent(params object[] args);
		public static MenuEvent OnControlRebound;
		public static MenuEvent OnDefaultsRestored;

		public void Start()
		{
			_submenu = Submenu.Main;
			_page    = Page.Game;

			rebindCancel = null;

			ActiveMenuControls       = new List<ISettingsMenuControl>();
			ActiveRebindControls = new List<ISettingsMenuControl>();
			ActiveRebindUI		 = new List<UnityEngine.Object>();

			inputBindings = new Dictionary<string, CategoryInfo>();

			rebindControlRows = new Dictionary<string, RebindControl>();

			_pages        = new Dictionary<Page, PageItems>();
			_controlIndexTracker = new Dictionary<Page, int>();
			_pagesInOrder = new List<Page>();
			//_tabs         = new List<SettingsMenuTab>();

			infoToRebind = null;

			Pool_Header     = new ComponentPool<SettingsMenuHeader>(PoolingRoot, Template_Header) { allocateTemp    = true, initSize = 10, };
			Pool_Checkbox   = new ComponentPool<CheckboxControl>(PoolingRoot, Template_Checkbox) { allocateTemp     = true, initSize = 10, };
			Pool_Slider     = new ComponentPool<SliderControl>(PoolingRoot, Template_Slider) { allocateTemp         = true, initSize = 10, };
			Pool_Dropdown   = new ComponentPool<DropdownControl>(PoolingRoot, Template_Dropdown) { allocateTemp     = true, initSize = 10, };
			Pool_InputField = new ComponentPool<InputFieldControl>(PoolingRoot, Template_InputField) { allocateTemp = true, initSize = 10, };
			Pool_Rebind     = new ComponentPool<SingleRebindControl>(PoolingRoot, Template_SingleRebind) { allocateTemp         = true, initSize = 10, };
			Pool_MultiRebind = new ComponentPool<MultiRebindControl>(PoolingRoot, Template_MultiRebind) { allocateTemp = true, initSize = 10, };
			Pool_RebindSectionTitle = new ComponentPool<RebindSectionTitle>(PoolingRoot, Template_RebindSectionTitle) { allocateTemp = true, initSize = 10, };
			Pool_Buttons    = new ComponentPool<ButtonControl>(PoolingRoot, Template_Button) { allocateTemp         = true, initSize = 10, };

			StopControlHighlight();

			Raycaster.enabled = false;

			MenuElement.Alpha        = 0;
			RebindElement.Alpha      = 0;
			RebindPopupElement.Alpha = 0;

			GameInputs.forceUnlocks.Set("settings_menu", false);

			BackButton.onClick.AddListener(OnExitStatic);

			RestoreDefaultsButton.OnSelected += data =>
			{
				if (data is PointerEventData) return;

				RestoreDefaultsButtonHighlight.SetActive(true);
			};

			RestoreDefaultsButton.OnDeselected += data =>
			{
				if (data is PointerEventData) return;

				RestoreDefaultsButtonHighlight.SetActive(false);
			};

			menuActive = false;
		}

		//[Button]
		protected override async UniTask enableMenu()
		{
			Init();
			GameInputs.forceUnlocks.Set("settings_menu", true);
			_selectedPage = (int)_pagesInOrder[0];

			//Page section = (Page)_selectedPage;
			//string sectionTitle = System.Enum.GetName(typeof(Page), section);
			//PanelTitleMain.text = sectionTitle;
			//PanelTitleBacking.text = sectionTitle;

			LeftMovementLabel.Button = GameInputs.menuLeft;
			RightMovementLabel.Button = GameInputs.menuRight;

			//PanelTitleMain.gameObject.SetActive(true);
			//PanelTitleBacking.gameObject.SetActive(true);

			ChangeSubmenu(Submenu.Main);
			MenuElement.DoScale(Vector3.one * 0.95f, Vector3.one, 0.2f);
			await MenuElement.DoAlphaFade(1, 0.2f).Tween.ToUniTask();
			Raycaster.enabled = true;
		}

		//[Button]
		protected override async UniTask disableMenu()
		{
			GameInputs.forceUnlocks.Set("settings_menu", false);
			Raycaster.enabled = false;
			//PanelTitleMain.gameObject.SetActive(false);
			//PanelTitleBacking.gameObject.SetActive(false);
			await ChangeSubmenu(Submenu.Main);
			MenuElement.DoScale(Vector3.one * 0.95f, 0.2f);
			await MenuElement.DoAlphaFade(0, 0.2f).Tween.ToUniTask();
		}

		[Button]
		public async UniTask ChangeSubmenu(Submenu next)
		{
			if (next == _submenu) return;

			MenuElement.Interactable = next == Submenu.Main;


			var batch = UniTask2.Batch();

			if (next == Submenu.Rebind)
				RebuildRebindWindowUI();

			if (_submenu == Submenu.Main || next == Submenu.Main)
			{
				batch.Add(ActivateOrDeactivate(RebindElement, next == Submenu.Rebind || next == Submenu.RebindPopup));
			}

			batch.Add(ActivateOrDeactivate(RebindPopupElement, next == Submenu.RebindPopup));

			await batch;

			_submenu = next;

			//if (_submenu == Submenu.Rebind && ActiveRebindControls.Count > 0)
			//{
			//	EventSystem.current.SetSelectedGameObject(ActiveRebindControls[0].Selectable.gameObject);
			//}

			async UniTask ActivateOrDeactivate(HUDElement element, bool active)
			{
				element.Interactable = active;

				if (active)
				{
					element.gameObject.SetActive(true);
					element.DoScale(Vector3.one * 0.95f, Vector3.one, 0.2f);
					await element.DoAlphaFade(1, 0.2f).Tween.ToUniTask();
				}
				else
				{
					element.DoScale(Vector3.one * 0.95f, 0.2f);
					await element.DoAlphaFade(0, 0.2f).Tween.ToUniTask();
					element.gameObject.SetActive(false);
				}
			}
		}

		public void AddRandomControls()
		{
			for (int i = 0; i < 50; i++)
			{
				var r = Random.value;

				string label = DataUtil.MakeShortID(24);

				if (r < 0.2f)
				{
					SpawnHeader(label);
				}
				else if (r < 0.4f)
				{
					SpawnCheckbox(label, Random.value > 0.5f);
				}
				else if (r < 0.6f)
				{
					var min = 0;
					var max = Random.Range(10f, 100);
					SpawnSlider(label, min, max, Random.Range(min, max), 1f, false);
				}
				else if (r < 0.8f)
				{
					var list = new List<string>();
					var num  = Mathf.FloorToInt(Random.Range(10, 50));

					for (int j = 0; j <= num; j++)
						list.Add(DataUtil.MakeShortID(10));

					SpawnDropdown(label, list, Mathf.FloorToInt(Random.Range(0, num)));
				}
				else
				{
					SpawnInputField(label, DataUtil.MakeShortID(10));
				}

				//SpawnHeader("This is a header" + i);
			}
		}

		[Button]
		public void Init()
		{
			ResetDefaultsPopup.SetActive(false);

			/*_activeContainer = new GameOptions.Container();
			GameOptions._file.CopyToContainer(_activeContainer);*/
			RebuildPages(GameOptions.current);

			foreach (SettingsPageTab tab in tabs)
			{
				tab.Initialize();
			}

			// TODO: Pool
			//foreach (var tab in _tabs)
			//{
			//	tab.gameObject.Destroy();
			//}

			//_tabs.Clear();

			//// Build tabs
			//foreach (var key in _pages.Keys)
			//{
			//	SettingsMenuTab tab = Instantiate(Template_Tab, TabsRoot);
			//	tab.Text.text = key.ToString();
			//	tab.gameObject.SetActive(true);
			//	tab.onPointerClick += menuTab => ChangePage(key);
			//	_tabs.Add(tab);

			//	_pages[key].Tab = tab;
			//}

			RebuildUI();

			inputBindings.Clear();
			StoreCurrentInputBindings();

			RestoreDefaultsButton.Selectable.interactable = CheckForOverrides();
		}

		public bool CheckForOverrides()
		{
			foreach (var category in inputBindings.Values)
			{
				if (category.CheckForOverrides())
				{
					return true;
				}
			}

			return false;
		}

		public void RestoreAllDefaults()
		{
			ResetDefaultsPopup.SetActive(false);

			GameInputs.Live.RestoreAllDefaultBindingsAndSave();

			foreach (var control in ActiveMenuControls)
			{
				var controlBase = control as SettingsMenuControlBase;

				if (controlBase != null)
				{
					controlBase.UpdateUI();
				}
			}

			RestoreDefaultsButton.Selectable.interactable = false;

			//GameInputs.move.InputAction.RemoveAllBindingOverrides();
			//GameInputs.jump.InputAction.RemoveAllBindingOverrides();
			//GameInputs.sword.InputAction.RemoveAllBindingOverrides();
			//GameInputs.interact.InputAction.RemoveAllBindingOverrides();
			//GameInputs.dive.InputAction.RemoveAllBindingOverrides();
			//GameInputs.splicer.InputAction.RemoveAllBindingOverrides();
			//GameInputs.pogo.InputAction.RemoveAllBindingOverrides();
			//GameInputs.showOverworldHUD.InputAction.RemoveAllBindingOverrides();
			//GameInputs.reorient.InputAction.RemoveAllBindingOverrides();

			//GameInputs.overdriveUp.InputAction.RemoveAllBindingOverrides();
			//GameInputs.overdriveDown.InputAction.RemoveAllBindingOverrides();

			//GameInputs.menuLeft.InputAction.RemoveAllBindingOverrides();
			//GameInputs.menuRight.InputAction.RemoveAllBindingOverrides();
			//GameInputs.menuLeft.InputAction.RemoveAllBindingOverrides();
			//GameInputs.menuRight2.InputAction.RemoveAllBindingOverrides();
			//GameInputs.toggleMode.InputAction.RemoveAllBindingOverrides();
			//GameInputs.favoriteItem.InputAction.RemoveAllBindingOverrides();
			//GameInputs.detachItem.InputAction.RemoveAllBindingOverrides();

			//GameInputs.confirm.InputAction.RemoveAllBindingOverrides();
			//GameInputs.cancel.InputAction.RemoveAllBindingOverrides();

			inputBindings.Clear();
			StoreCurrentInputBindings();

			RefreshRebindRows();
		}

		private void StoreCurrentInputBindings()
		{
			inputBindings.Add("Overworld", new CategoryInfo("Overworld"));
			inputBindings.Add("Combat", new CategoryInfo("Combat"));
			inputBindings.Add("Menu", new CategoryInfo("Menu"));
			inputBindings.Add("General", new CategoryInfo("General"));

			CategoryInfo overworldInfo = inputBindings["Overworld"];
			overworldInfo.RegisterAction("Move", GameInputs.move);
			overworldInfo.RegisterAction("Jump", GameInputs.jump);
			overworldInfo.RegisterAction("Sword", GameInputs.sword);
			overworldInfo.RegisterAction("Interact", GameInputs.interact);
			overworldInfo.RegisterAction("Dive", GameInputs.dive);
			overworldInfo.RegisterAction("Splicer", GameInputs.splicer);
			overworldInfo.RegisterAction("Pogo", GameInputs.pogo);
			overworldInfo.RegisterAction("Show Overworld HUD", GameInputs.showOverworldHUD);
			overworldInfo.RegisterAction("Reorient", GameInputs.reorient);

			CategoryInfo combatInfo = inputBindings["Combat"];
			combatInfo.RegisterAction("Overdrive Up", GameInputs.overdriveUp);
			combatInfo.RegisterAction("Overdrive Down", GameInputs.overdriveDown);		
			//combatInfo.RegisterAction("View Health", GameInputs.viewHealth);

			CategoryInfo menuInfo = inputBindings["Menu"];
			menuInfo.RegisterAction("Change Tab Left", GameInputs.menuLeft);
			menuInfo.RegisterAction("Change Tab Right", GameInputs.menuRight);
			menuInfo.RegisterAction("Change Party Member Left", GameInputs.menuLeft2);
			menuInfo.RegisterAction("Change Party Member Right", GameInputs.menuRight2);
			menuInfo.RegisterAction("Toggle Menu Mode", GameInputs.toggleMode);
			menuInfo.RegisterAction("Favorite Item", GameInputs.favoriteItem);
			menuInfo.RegisterAction("Detach Item", GameInputs.detachItem);

			CategoryInfo generalInfo = inputBindings["General"];
			generalInfo.RegisterAction("Confirm", GameInputs.confirm);
			generalInfo.RegisterAction("Cancel", GameInputs.cancel);
		}

		[Button]
		public void SaveOptions()
		{
			GameOptions._file.ApplyAllTemp();
			GameOptions.Save();
		}

		public void RebuildPages(GameOptions.Container opt)
		{
			_pages.Clear();
			_controlIndexTracker.Clear();

			NewPage(Page.Game)
				.AddINIBool(opt.run_by_default, "Run by Default")
				.AddINIBool(opt.combat_intro, "Combat Intro")
				.AddINIBool(opt.combat_memory_cursors, "Combat Memory Cursors")
				.AddINIBool(opt.use_test_dummy_bust, "Use friendly test dummy For characters with no portraits")
			   .AddINIFloat(opt.dummy, 50, 200, 5, "Health Scaling (UNIMPLEMENTED)")
				.Button("TEST TEST TEST", () => DebugLogger.Log("PRESSED", LogContext.UI, LogPriority.Temp));

			NewPage(Page.Camera)
				.AddINIBool(opt.CameraAuto, "Use Automatic Camera")
				.AddINIFloat(opt.CameraSensitivity, 0.1f, 10f, label: "Camera Sensitivity")
				.AddFloat(Quality.Current.FOV, "Field of View", Quality.FOV_MIN, Quality.FOV_MAX, 1, val => Quality.Current.FOV = val, 0)
				.Header("Invert")
				.AddINIBool(opt.CameraMouseInvertX, "Mouse Horizontal")
				.AddINIBool(opt.CameraMouseInvertY, "Mouse Vertical")
				.AddINIBool(opt.CameraPadInvertX, "Gamepad Horizontal")
				.AddINIBool(opt.CameraPadInvertY, "Gamepad Vertical");

			NewPage(Page.Audio)
				.Header("Levels")
				.AddAudioFloat(opt.audio_level_master, 0, 10, audioSettingSources[0], label: "Master")
				.AddAudioFloat(opt.audio_level_sfx, 0, 10, audioSettingSources[1], label: "Effects")
				.AddAudioFloat(opt.audio_level_music, 0, 10, audioSettingSources[2], label: "Music")
				.AddAudioFloat(opt.audio_level_ambient, 0, 10, audioSettingSources[3], label: "Ambient")
				.AddAudioFloat(opt.audio_level_voice, 0, 10, audioSettingSources[4], label: "Voice");

			// TEMP. Should probably have a better way of doing this.
			List<(string, object)> resolutions = new List<(string, object)>();
			for (int i = 0; i < GameController.SupportedResolutions.Count; i++)
			{
				var res = GameController.SupportedResolutions[i];
				resolutions.Add((res.x + "x" + res.y, (float)i));
			}

			NewPage(Page.Display)
				.AddINIFloatDropdown(opt.screen_resolution, "Resolution", resolutions.ToArray())
				.AddEnum(Quality.Current.TextureQuality, "Texture Resolution", Quality.SetTextureQuality)
				.AddFloat(Quality.Current.ResolutionScale, "Resolution Scaling", Quality.MIN_RESOLUTION_SCALING, Quality.MAX_RESOLUTION_SCALING, 0.1f, Quality.SetResolutionScaling)
				.Header("Antialiasing")
				.AddEnum(Quality.Current.AntialiasMode, "Mode", Quality.SetAntialiasMode)
				.AddEnum(Quality.Current.SMAAQualityLevel, "SMAA Quality", Quality.SetSMAAQuality)
				.Header("Dynamic Shadows")
				.AddEnum(Quality.Current.ShadowQuality, "Quality", Quality.SetShadowQuality)
				.AddEnum(Quality.Current.ShadowResolution, "Resolution", Quality.SetShadowResolution)
				.AddEnum(Quality.Current.ShadowProjection, "Projection", Quality.SetShadowProjection)
				.AddFloat(Quality.Current.ShadowDistance, "Distance", Quality.MIN_SHADOW_DISTANCE, Quality.MAX_SHADOW_DISTANCE, 0.5f, Quality.SetShadowDistance)
				.Header("Effects / Post Processing")
				.AddBool(Quality.Current.SoftParticles, "Soft Particles", Quality.SetSoftParticles)
				.AddBool(Quality.Current.BloomEnabled, "Bloom", Quality.SetBloom)
				.AddBool(Quality.Current.SSAOEnabled, "SSAO", Quality.SetSSAO);

			NewPage(Page.Input)
				.Button("Rebind Controls", () =>
				{
					if (_submenu == Submenu.Main)
					{
						if (ActiveMenuControls.Count > 0)
						{
							for (int i = 0; i < ActiveMenuControls.Count; i++)
							{
								ActiveMenuControls[i].OnDeselected?.Invoke(null);
							}
						}

						//RebindingMode = RebindMode.Keyboard;
						ChangeSubmenu(Submenu.Rebind);
					}
				})
				//.Button("Rebind Gamepad", () =>
				//{
				//	if (_submenu == Submenu.Main)
				//	{
				//		RebindingMode = RebindMode.Gamepad;
				//		ChangeSubmenu(Submenu.Rebind);
				//	}
				//})
				.Button("Restore All Defaults", () =>
				{
					PromptForResetConfirmation();
					//GameInputs.Live.RestoreAllDefaultBindingsAndSave();
				},
				(selectable) =>
				{
					selectable.interactable = CheckForOverrides();
				});

			NewPage(Page.Network)
				.AddINIString(opt.netplay_username, "Username");

			NewPage(Page.Accessibility)
				.Header("Unimplemented")
				.AddBool(false, "Colorblind Mode", b => { })
				.AddBool(false, "Large Font", b => { })
				.AddBool(opt.combat_camera_motions, "Combat Camera Motions", b => { opt.combat_camera_motions.Value = b; })
				//.AddBool(opt.combat_camera_motions, "Combat Camera Motion", b => { opt.combat_camera_motions.Value = b; })
				.AddBool(false, "Et Cetera.", b => { })
				;


			//NewPage(Page.Debug)
			//	.Header("Spawning")
			//	.AddINIBool(opt.ow_party)
			//	.AddINIBool(opt.ow_guests)
			//	.AddINIBool(opt.ow_encounters)
			//	.AddINIBool(opt.ow_encounters_launch)
			//	.Header("Combat")
			//	.AddINIBool(opt.ow_encounters_helix)
			//	.AddINIBool(opt.combat_fast_warmup)
			//	.AddINIBool(opt.combat_fast_pace)
			//	.AddINIBool(opt.combat_skill_unlocks)
			//	.AddINIBool(opt.combat_use_cost)
			//	.AddINIBool(opt.combat_hurt)
			//	//.AddBool(opt.combat_)
			//	//.AddBool(opt.combat_)
			//	.AddINIBool(opt.combat_autowin)
			//	.Header("Misc")
			//	.AddINIString(opt.default_savefile)
			//	.AddINIBool(opt.load_on_demand)
			//	.AddINIBool(opt.pool_on_demand)
			//	.AddINIBool(opt.keep_lua_core)
			//	.AddINIBool(opt.mouselock_on_startup)
			//	.AddINIBool(opt.mouselock_disable)
			//	.AddINIBool(opt.debug_ui_on_startup)
			//	.AddINIBool(opt.spawn_with_imgui)
			//	.AddINIBool(opt.spawn_with_priority)
			//	.AddINIBool(opt.autosave_devmode)
			//	.AddINIBool(opt.autosave_on_quit)
			//	.AddINIBool(opt.sprite_color_replacement)
			//	.AddINIBool(opt.splicer_hub_backdrop)
			//	.AddINIBool(opt.log_addressable_profiling);
		}

		public async UniTask RebuildUI()
		{
			ClearActiveControls(ActiveMenuControls);

			if (!_pages.TryGetValue(_page, out PageItems items)) return;

			foreach (var audioSource in audioSettingSources)
			{
				if (audioSource != null)
				{
					audioSource.gameObject.SetActive((_page == Page.Audio));
				}
			}

			foreach (Item item in items.AllValues)
			{
				switch (item)
				{
					case HeaderItem header:
						SpawnHeader(header.label);
						break;
					case ButtonItem button:
						SpawnButton(button.label, button.action, button.update);
						break;

					case BoolItem boolItem:
						SpawnCheckbox(boolItem.label, boolItem.InitialValue, boolItem.OnChanged);
						break;

					case DropdownItem dropdownItem:
						break;

					case FloatItem floatItem:
						SpawnSlider(floatItem.label, floatItem.min, floatItem.max, floatItem.InitialValue, floatItem.increment, false, floatItem.OnChanged, floatItem.decimalPlaces);
						break;

					case StringItem stringItem: break;

					case IEnumItem enumItem:
						SpawnDropdown(item.label, enumItem.GetDropdownValues().ToList(), enumItem.GetInitialValue(), v => enumItem.OnValueChanged(v));
						break;

					case INIItem ini:

						switch (ini.type)
						{
							case ControlType.Button:
								break;

							case ControlType.Bool:
							{
								if (ini.ini_value is INIBool boolVal)
									SpawnCheckbox(ini.label ?? boolVal.DisplayName, boolVal.Value, new Action<bool>(v => boolVal.TempValue = v));
							}
								break;

							case ControlType.Float:
							{
								if (ini.ini_value is INIFloat floatVal)
									SpawnSlider(item.label ?? floatVal.DisplayName, ini.min, ini.max, floatVal.Value, ini.increment, false, v => floatVal.TempValue = v);
							}
								break;

							case ControlType.AudioFloat:
							{
								if (ini.ini_value is INIFloat floatVal)
									SpawnSlider(item.label ?? floatVal.DisplayName, ini.min, ini.max, floatVal.Value, ini.increment, false, v =>
									{
										floatVal.TempValue = v;

										if ((ini.associatedObject != null) && (ini.associatedObject is AudioSource))
										{
											AudioSource audioSource = ini.associatedObject as AudioSource;
											audioSource.volume = (v / Anjin.Audio.AudioManager.AUDIO_LEVEL_UPPER_BOUND);

											if (!audioSource.isPlaying)
											{
												audioSource.Play();
											}
										}
									});
							}

								break;

							case ControlType.Int: break;

							case ControlType.String:
							{
								if (ini.ini_value is INIString stringVal)
									SpawnInputField(ini.label ?? stringVal.DisplayName, stringVal.Value, v => stringVal.TempValue = v);
							}
								break;

							case ControlType.Enum: break;

							case ControlType.FloatDropdown:
							{
								if (ini.ini_value is INIFloat floatVal)
								{
									SpawnDropdown(ini.label ?? floatVal.DisplayName, ini.dropdown_values.Select(x => x.Item1).ToList(), (int)floatVal, v =>
									{
										if (ini.dropdown_values.TryGet(Mathf.FloorToInt(v), out (string, object) val))
										{
											floatVal.TempValue = (float)val.Item2;
										}
									});
								}
							}
								break;
						}

						break;
				}
			}

			// TODO: LINQ
			UGUI.SetupListNavigation(ActiveMenuControls.Where(x => x.Selectable != null).Select(x => x.Selectable).ToList(), AxisDirection.Vertical);

			await UniTask.WaitForEndOfFrame();
			//SelectTab(items.Tab);
			SelectTab(tabs[_selectedPage]);

			if (ActiveMenuControls.Count > 0)
			{
				var control = ActiveMenuControls.FirstOrDefault(x => x.Selectable != null);
				GameObject first = control?.Selectable.gameObject;

				if (first != null)
				{
					EventSystem.current.SetSelectedGameObject(first);
					control.OnSelected?.Invoke(null);
				}
			}

			//SpawnCheckbox(options.ow_no_encounter_spawning.DisplayName.Text, options.ow_no_encounter_spawning.Value, b => options.ow_no_encounter_spawning.Value = b);
		}

		public void RebuildRebindWindowUI()
		{
			ClearActiveRebindUI(ActiveRebindUI);
			ActiveRebindControls.Clear();
			rebindControlRows.Clear();

			//string group = ((RebindingMode == RebindMode.Gamepad) ? GameInputs.GROUP_GAMEPAD : GameInputs.GROUP_KEYBOARD);
			//string group = "";

			AddSectionTitle("OVERWORLD");
			AddMultiAction(GameInputs.move, "Overworld", "Move", false, true, false, true);
			AddAction(GameInputs.jump, "Overworld", "Jump", false, true, true, true);
			AddAction(GameInputs.sword, "Overworld", "Sword", false, true, true, true);
			AddAction(GameInputs.interact, "Overworld", "Interact", true, true, true, true);
			AddAction(GameInputs.dive, "Overworld", "Dive", false, true, true, true);
			AddAction(GameInputs.splicer, "Overworld", "Splicer", false, true, true, true);
			AddAction(GameInputs.pogo, "Overworld", "Pogo", false, true, true, true);
			AddAction(GameInputs.showOverworldHUD, "Overworld", "Show Overworld HUD", false, true, true, true);
			AddAction(GameInputs.reorient, "Overworld", "Reorient", false, true, true, true);

			AddSectionTitle("COMBAT");
			AddAction(GameInputs.overdriveUp, "Combat", "Overdrive Up", false, true, true, true);
			AddAction(GameInputs.overdriveDown, "Combat", "Overdrive Down", false, true, true, true);

			AddSectionTitle("MENU");
			AddAction(GameInputs.menuLeft, "Menu", "Change Tab Left", false, true, true, true);
			AddAction(GameInputs.menuRight, "Menu", "Change Tab Right", false, true, true, true);
			AddAction(GameInputs.menuLeft2, "Menu", "Change Party Member Left", false, true, true, true);
			AddAction(GameInputs.menuRight2, "Menu", "Change Party Member Right", false, true, true, true);
			AddAction(GameInputs.toggleMode, "Menu", "Toggle Menu Mode", false, true, true, true);
			AddAction(GameInputs.favoriteItem, "Menu", "Favorite Item", false, true, true, true);
			AddAction(GameInputs.detachItem, "Menu", "Detach Item", false, true, true, true);

			AddSectionTitle("GENERAL");
			AddAction(GameInputs.confirm, "General", "Confirm", false, true, true, true);
			AddAction(GameInputs.cancel, "General", "Cancel", false, true, true, true);

			for (int i = ActiveRebindControls.Count - 3; i < ActiveRebindControls.Count; i++)
			{
				var selectable = ActiveRebindControls[i].Selectable;
				var nav = selectable.navigation;

				nav.selectOnDown = RestoreDefaultsButton.Selectable;
				selectable.navigation = nav;
			}

			//ActiveRebindControls.Add(RestoreDefaultsButton);

			var navigation = RestoreDefaultsButton.Selectable.navigation;
			navigation.selectOnLeft = ActiveRebindControls[ActiveRebindControls.Count - 3].Selectable;
			navigation.selectOnUp = ActiveRebindControls[ActiveRebindControls.Count - 2].Selectable;
			navigation.selectOnRight = ActiveRebindControls[ActiveRebindControls.Count - 1].Selectable;
			navigation.selectOnDown = RestoreDefaultsButton.Selectable;

			RestoreDefaultsButton.Selectable.navigation = navigation; 

			//UGUI.SetupListNavigation(ActiveRebindUI.Where(x => x.Selectable != null).Select(x => x.Selectable).ToList(), AxisDirection.Vertical);

			/*int index = input.InputAction.GetBindingIndex(group);

			if (index == -1) return;*/

			//UGUI.SetupListNavigation(ActiveRebindUI.Where(x => x.Selectable != null).Select(x => x.Selectable).ToList(), AxisDirection.Vertical);

			if (ActiveRebindControls.Count > 0)
			{
				var control = ActiveRebindControls.FirstOrDefault(x => x.Selectable != null);
				GameObject first = control?.Selectable.gameObject;

				if (first != null)
				{
					EventSystem.current.SetSelectedGameObject(first);
					control.OnSelected?.Invoke(null);
				}
			}

			RestoreDefaultsButton.Selectable.interactable = CheckForOverrides();

			void AddSectionTitle(string title)
			{
				var sectionTitle = Pool_RebindSectionTitle.Rent();
				sectionTitle.Set(title);

				ActiveRebindUI.Add(sectionTitle);
				sectionTitle.RT.SetParent(RebindControlsRoot);
			}

			void AddAction(GameInputs.IHasInputAction _action, string category, string name, bool allowDuplicates, bool keyboardRebindAllowed, bool mouseRebindAllowed, bool gamepadRebindAllowed)
			{
				if (inputBindings.ContainsKey(category))
				{
					CategoryInfo categoryInfo = inputBindings[category];

					ActionInfo actionInfo = categoryInfo.GetActionInfo(name);

					if (actionInfo != null)
					{
						var keyboardBindingInfo = actionInfo.GetBindingInfoForPlatform("Keyboard");
						var mouseBindingInfo = actionInfo.GetBindingInfoForPlatform("Mouse");
						var gamepadBindingInfo = actionInfo.GetBindingInfoForPlatform("Gamepad");

						var control = Pool_Rebind.Rent();

						control.Set(_action.InputAction, category, name, allowDuplicates, keyboardRebindAllowed, mouseRebindAllowed, gamepadRebindAllowed, keyboardBindingInfo, mouseBindingInfo, gamepadBindingInfo);

						//OnControlRebound += control.ProcessRebind;

						ActiveRebindUI.Add(control);

						//ActiveRebindControls.Add(control.KeyboardIcon.gameObject.activeSelf ? control.KeyboardIcon : control.KeyboardBlankButton);
						control.RT.SetParent(RebindControlsRoot);
						control.Label.text = name;

						ActiveRebindControls.AddRange(control.GetSelectables());
					}
				}
			}

			//void AddHandler(GameInputs.IHasInputAction _action)
			//{
			//	InputAction                 action   = _action.InputAction;
			//	ReadOnlyArray<InputBinding> bindings = action.bindings;

			//	bool first = true;

			//	for (int i = 0; i < bindings.Count; i++)
			//	{
			//		InputBinding binding = bindings[i];

			//		//if (!binding.groups.Contains(group)) continue;

			//		var control = Pool_Rebind.Rent();

			//		control.Set(i, group, first ? action.name : "", action);

			//		//control.Button.onClick.RemoveAllListeners();
			//		//control.Button.onClick.AddListener(() =>
			//		//{
			//		//	StartRebind(control);
			//		//});

			//		// setup
			//		//{
			//		//ActiveRebindControls.Add(control);
			//		//control.RT.SetParent(RebindControlsRoot);
			//		////control.Label.text = action.name;

			//		//control.OnSelected += data =>
			//		//{
			//		//	if (data is PointerEventData) return;

			//		//	RebindHighlighter.gameObject.SetActive(true);
			//		//	control.SnapUIRectTo(RebindHighlighter);
			//		//	Canvas.ForceUpdateCanvases();
			//		//	RebindScrollRect.ScrollTo(control.RT);
			//		//};

			//		//control.OnDeselected += data =>
			//		//{
			//		//	RebindHighlighter.gameObject.SetActive(false);
			//		//};
			//		//}

			//		ActiveRebindUI.Add(control);
			//		control.RT.SetParent(RebindControlsRoot);
			//		control.Label.text = action.name;

			//		first = false;
			//	}
			//}

			void AddMultiAction(GameInputs.IHasInputAction _action, string category, string action, bool allowDuplicates, bool keyboardRebindAllowed, bool mouseRebindAllowed, bool gamepadRebindAllowed)
			{
				if (inputBindings.ContainsKey(category))
				{
					CategoryInfo categoryInfo = inputBindings[category];

					ActionInfo actionInfo = categoryInfo.GetActionInfo(action);

					if (actionInfo != null)
					{
						var keyboardBindingInfo = actionInfo.GetBindingInfoForPlatform("Keyboard");
						var mouseBindingInfo = actionInfo.GetBindingInfoForPlatform("Mouse");
						var gamepadBindingInfo = actionInfo.GetBindingInfoForPlatform("Gamepad");

						var control = Pool_MultiRebind.Rent();

						//OnControlRebound += control.ProcessRebind;

						control.Set(_action.InputAction, category, action, allowDuplicates, keyboardRebindAllowed, mouseRebindAllowed, gamepadRebindAllowed, keyboardBindingInfo, mouseBindingInfo, gamepadBindingInfo);

						ActiveRebindUI.Add(control);
						control.RT.SetParent(RebindControlsRoot);
						control.Label.text = action;

						ActiveRebindControls.AddRange(control.GetSelectables());
					}
				}
			}
		}

		public async void ShowRebindPopup(BindingInfo bindingInfo)
		{
			infoToRebind = bindingInfo;

			PreRebindPopupLabel.text = $"Handler to rebind: {infoToRebind.Name}\nCurrent binding: {((infoToRebind.Mapping != "") ? infoToRebind.Mapping : "[NONE]")}";

			StartRebindMenu.SetActive(false);
			PreRebindMenu.SetActive(true);

			await ChangeSubmenu(Submenu.RebindPopup);
		}

		public void CancelRebind()
		{
			infoToRebind = null;

			ChangeSubmenu(Submenu.Rebind);
		}

		public void PromptForResetConfirmation()
		{
			ResetDefaultsPopup.SetActive(true);
		}

		public void CancelDefaultsReset()
		{
			ResetDefaultsPopup.SetActive(false);
		}

		public async void StartRebind()
		{
			if (infoToRebind != null)
			{
				Vector2 sizeDelta = RebindCountdown.rectTransform.sizeDelta;
				sizeDelta.x = 1098;
				RebindCountdown.rectTransform.sizeDelta = sizeDelta;

				string category = infoToRebind.Category;

				InputAction action = infoToRebind.Action;

				InputBinding binding = infoToRebind.Binding;
				string effectivePath = binding.effectivePath;

				int bindingIndex = infoToRebind.Index;

				string name = infoToRebind.Name;

				string platform = infoToRebind.Platform;
				string group = infoToRebind.Group;
				string mapping = infoToRebind.Mapping;

				string firstPlatformToExclude = "";
				string secondPlatformToExclude = "";

				switch (platform)
				{
					case "Keyboard":
						firstPlatformToExclude = "Mouse";
						secondPlatformToExclude = "Gamepad";

						StartRebindPopupLabel.text = $"Press any key before the timer runs out to rebind {name}";

						break;
					case "Mouse":
						firstPlatformToExclude = "Keyboard";
						secondPlatformToExclude = "Gamepad";

						StartRebindPopupLabel.text = $"Click any mouse button before the timer runs out to rebind {name}";

						break;
					case "Gamepad":
						firstPlatformToExclude = "Keyboard";
						secondPlatformToExclude = "Mouse";

						StartRebindPopupLabel.text = $"Press any gamepad button before the timer runs out to rebind {name}";

						break;
				}

				

				action.Disable();

				_currentRebindOperation = action.PerformInteractiveRebinding(bindingIndex)
					.WithControlsExcluding(firstPlatformToExclude)
					.WithControlsExcluding(secondPlatformToExclude)
					.OnComplete(x =>
					{
						x.Dispose();

						if (rebindCancel != null)
						{
							StopCoroutine(rebindCancel);
							rebindCancel = null;
						}

						ChangeSubmenu(Submenu.Rebind);
						action.Enable();

						string newBindingPath = action.bindings[bindingIndex].effectivePath;

						string[] tokens = newBindingPath.Split(GameInputs.PathDelimeter, StringSplitOptions.RemoveEmptyEntries);
						string newMapping = tokens[(tokens.Length - 1)];

						if (inputBindings.ContainsKey(category))
						{
							CategoryInfo categoryInfo = inputBindings[category];

							BindingInfo infoToUpdate;
							bool foundDuplicate = categoryInfo.FindDuplicate(newMapping, out infoToUpdate);

							if (foundDuplicate)
							{
								infoToUpdate.Action.ApplyBindingOverride(effectivePath);
							}
						}

						inputBindings.Clear();
						StoreCurrentInputBindings();

						RefreshRebindRows();

						foreach (var control in ActiveMenuControls)
						{
							var controlBase = control as SettingsMenuControlBase;

							if (controlBase != null)
							{
								controlBase.UpdateUI();
							}
						}

						RestoreDefaultsButton.Selectable.interactable = true;

						GameInputs.Live.SaveBindings();
					})
					.OnCancel(x =>
					{
						x.Dispose();

						if (rebindCancel != null)
						{
							StopCoroutine(rebindCancel);
							rebindCancel = null;
						}

						ChangeSubmenu(Submenu.Rebind);
						action.Enable();
					});

				//if (infoToRebind.Index != -1)
				//{
				//	_currentRebindOperation = infoToRebind.Handler.PerformInteractiveRebinding(infoToRebind.Index)
				//		.WithTargetBinding(infoToRebind.Index)
				//		.OnComplete(x =>
				//		{
				//			x.Dispose();
				//			ChangeSubmenu(Submenu.Rebind);
				//			x.action.Enable();
				//			GameInputs.Live.SaveBindings();
				//		});
				//}
				//else
				//{
				//	_currentRebindOperation = infoToRebind.Handler.PerformInteractiveRebinding()
				//		.WithTargetBinding(infoToRebind.Index)
				//		.OnComplete(x =>
				//		{
				//			x.Dispose();
				//			ChangeSubmenu(Submenu.Rebind);
				//			x.action.Enable();
				//			GameInputs.Live.SaveBindings();
				//		});
				//}

				//await ChangeSubmenu(Submenu.RebindPopup);

				PreRebindMenu.SetActive(false);
				StartRebindMenu.SetActive(true);

				if (rebindCancel != null)
				{
					StopCoroutine(rebindCancel);
					rebindCancel = null;
				}

				rebindCancel = WaitToCancelRebind(1098, 5);
				StartCoroutine(rebindCancel);

				_currentRebindOperation.Start();
			}
		}

		private void RefreshRebindRows()
		{
			foreach (var row in ActiveRebindUI)
			{
				RebindControl control = row as RebindControl;

				if (control != null)
				{
					string category = control.Category;
					string action = control.Name;

					if (inputBindings.ContainsKey(category))
					{
						CategoryInfo categoryInfo = inputBindings[category];

						ActionInfo actionInfo = categoryInfo.GetActionInfo(action);

						if (actionInfo != null)
						{
							var keyboardBindings = actionInfo.GetBindingInfoForPlatform("Keyboard");
							var mouseBindings = actionInfo.GetBindingInfoForPlatform("Mouse");
							var gamepadBindings = actionInfo.GetBindingInfoForPlatform("Gamepad");

							control.Refresh(keyboardBindings, mouseBindings, gamepadBindings);
						}
					}
				}
			}
		}

		private System.Collections.IEnumerator WaitToCancelRebind(float value, float duration)
		{
			float delta = 0;

			if ((value < Mathf.Epsilon) || (duration < Mathf.Epsilon))
			{
				if (_currentRebindOperation != null)
				{
					_currentRebindOperation.Cancel();
				}

				yield break;
			}

			delta = value / duration;

			Vector2 sizeDelta = RebindCountdown.rectTransform.sizeDelta;

			for (float t = 0; t < duration; t += Time.deltaTime)
			{
				yield return null;
				value = value - Time.deltaTime * delta;
				value = ((value < 0) ? 0 : value);

				sizeDelta.x = value;
				RebindCountdown.rectTransform.sizeDelta = sizeDelta;

				if (value == 0)
				{
					_currentRebindOperation.Cancel();
				}
			}

			rebindCancel = null;
		}

		//public async void StartRebind(RebindControl control)
		//{
		//	switch (RebindingMode)
		//	{
		//		case RebindMode.Keyboard:
		//			StartRebindPopupLabel.text = $"Press any key to rebind \"{control.Handler.name}\"\nPress Escape to cancel";

		//			break;
		//		case RebindMode.Mouse:
		//			StartRebindPopupLabel.text = $"Click any mouse button to rebind \"{control.Handler.name}\"\nPress Escape to cancel";

		//			break;
		//		case RebindMode.Gamepad:
		//			StartRebindPopupLabel.text = $"Press any gamepad button to rebind \"{control.Handler.name}\"\nPress Escape to cancel";

		//			break;
		//	}

		//	control.Handler.Disable();
		//	_currentRebindOperation = control.Handler.PerformInteractiveRebinding()
		//		.WithTargetBinding(control.BindingIndex)
		//		.OnComplete(x =>
		//		{
		//			x.Dispose();
		//			ChangeSubmenu(Submenu.Rebind);
		//			x.action.Enable();
		//			GameInputs.Live.SaveBindings();
		//		});
		//	await ChangeSubmenu(Submenu.RebindPopup);
		//	_currentRebindOperation.Start();
		//}

		private int test = 0;

		private void Update()
		{
			if (menuActive)
			{
				if (ResetDefaultsPopup.activeSelf)
				{
					if (GameInputs.confirm.IsPressed)
					{
						RestoreAllDefaults();
					}
					else if (GameInputs.cancel.IsPressed)
					{
						CancelDefaultsReset();
					}

					return;
				}

				if (_submenu == Submenu.Main)
				{
					//if (GameInputs.menuNavigate.left.IsPressed) PrevPage();
					//else if (GameInputs.menuNavigate.right.IsPressed) NextPage();
					if (GameInputs.menuLeft.IsPressed) PrevPage();
					else if (GameInputs.menuRight.IsPressed) NextPage();
				}

				//Mouse.current.IsPressed
				//	Mouse.current.getp

				if (_submenu == Submenu.RebindPopup && Keyboard.current.escapeKey.wasReleasedThisFrame)
				{
					ChangeSubmenu(Submenu.Rebind);
				}

				List<ISettingsMenuControl> current_controls = null;

				switch (_submenu)
				{
					case Submenu.Main:
						current_controls = ActiveMenuControls;
						break;
					case Submenu.Rebind:
						current_controls = ActiveRebindControls;
						//current_controls = null;
						break;
					case Submenu.RebindPopup: break;
				}

				if (current_controls != null && current_controls.Count > 0)
				{
					if (GameInputs.menuNavigate.AnyPressed)
					{
						if (EventSystem.current.currentSelectedGameObject == null)
						{
							GameObject first = current_controls.FirstOrDefault(x => x.Selectable != null)?.Selectable.gameObject;

							if (first != null)
							{
								DebugLogger.Log("Pressed down " + (++test) + " times, and the current control is " + first.name, LogContext.UI, LogPriority.Temp);

								EventSystem.current.SetSelectedGameObject(first);

								/*MoveDirection move_dir = MoveDirection.None;

								if (GameInputs.move.up.IsPressed) move_dir = MoveDirection.Up;
								if (GameInputs.move.down.IsPressed) move_dir = MoveDirection.Down;

								ExecuteEvents.Execute(
									first,
									new AxisEventData(EventSystem.current) { moveDir = move_dir },
									ExecuteEvents.moveHandler);*/

								if (_submenu != Submenu.Rebind)
								{
									ScrollTo(current_controls[0].RT);
								}
								else
								{
									RebindScrollTo(current_controls[0].RT);
								}
							}
							else
							{
								DebugLogger.Log("Pressed down " + (++test) + " times, and no selectable could be found", LogContext.UI, LogPriority.Temp);
							}
						}
						else
						{
							//DebugLogger.Log("Pressed down " + (++test) + " times, and the current EventSystems object isn't null", LogContext.UI, LogPriority.Temp);
							DebugLogger.Log("Pressed down " + (++test) + " times, and the current control is " + EventSystem.current.currentSelectedGameObject.name, LogContext.UI, LogPriority.Temp);
						}
					}

					if (GameInputs.confirm.AbsorbPress() && (EventSystem.current.currentSelectedGameObject != null))
					{
						ControlInteraction control = EventSystem.current.currentSelectedGameObject.GetComponent<ControlInteraction>();

						if (control != null)
						{
							//current_controls[0].OnSelected?.Invoke(null);
							control.Interact();
						}
					}
				}
				else
				{
					EventSystem.current.SetSelectedGameObject(null);
					StopControlHighlight();
				}

				if (GameInputs.cancel.AbsorbPress())
				{
					if (_submenu == Submenu.Main)
					{
						OnExitStatic();
					}
					else if (_submenu == Submenu.Rebind)
					{
						ChangeSubmenu(Submenu.Main);

						if (ActiveMenuControls.Count > 0)
						{
							var control = ActiveMenuControls.FirstOrDefault(x => x.Selectable != null);
							GameObject first = control?.Selectable.gameObject;

							if (first != null)
							{
								EventSystem.current.SetSelectedGameObject(first);
								control.OnSelected?.Invoke(null);
							}
						}
					}
				}
			}
		}

		protected override void OnExit()
		{
			// TODO move this to disableMenu?
			EventSystem.current.SetSelectedGameObject(null);
			StopControlHighlight();
			SaveOptions();
		}

		public void ScrollTo(RectTransform target)
		{
			Canvas.ForceUpdateCanvases();
			ScrollRect.ScrollTo(target);
		}

		public void RebindScrollTo(RectTransform target)
		{
			Canvas.ForceUpdateCanvases();
			RebindScrollRect.ScrollTo(target);
		}

		public void ChangePage(int page)
		{
			bool success = Enum.IsDefined(typeof(Page), page);

			if (success)
			{
				ChangePage((Page)page);
			}
		}

		[Button]
		public void ChangePage(Page page)
		{
			if (_page == page) return;
			_page         = page;
			_selectedPage = _pagesInOrder.IndexOf(page);
			GameSFX.PlayGlobal(_sfxScrollNext, transform, 1, -1);
			RebuildUI();
		}

		[Button]
		public void NextPage()
		{
			_selectedPage = (_selectedPage + 1).Wrap(0, _pagesInOrder.Count - 1);

			//Page section = (Page)_selectedPage;
			//string sectionTitle = System.Enum.GetName(typeof(Page), section);
			//PanelTitleMain.text = sectionTitle;
			//PanelTitleBacking.text = sectionTitle;

			ChangePage(_pagesInOrder[_selectedPage]);
			//RebuildUI().Forget();
		}

		[Button]
		public void PrevPage()
		{
			_selectedPage = (_selectedPage - 1).Wrap(0, _pagesInOrder.Count - 1);

			//Page section = (Page)_selectedPage;
			//string sectionTitle = System.Enum.GetName(typeof(Page), section);
			//PanelTitleMain.text = sectionTitle;
			//PanelTitleBacking.text = sectionTitle;

			ChangePage(_pagesInOrder[_selectedPage]);
			//RebuildUI().Forget();
		}

		//public void SelectTab(SettingsMenuTab tab)
		//{
		//	foreach (SettingsMenuTab _tab in _tabs)
		//		_tab.Selected = false;

		//	TabHighlighter.gameObject.SetActive(true);
		//	tab.SnapRectangleShapeTo(TabHighlighter);
		//	tab.Selected = true;
		//}

		public void SelectTab(SettingsPageTab selected)
		{
			foreach (SettingsPageTab tab in tabs)
			{
				tab.ToggleSelection(false);
				tab.transform.SetAsLastSibling();
			}

			selected.ToggleSelection(true);
			selected.transform.SetAsLastSibling();
		}

		public void ClearSelection() { }

		public void HighlightControl(ISettingsMenuControl control)
		{
			SettingHighlighter.gameObject.SetActive(true);
			control.SnapUIRectTo(SettingHighlighter);
		}

		public void HighlightRebindControl(ISettingsMenuControl control)
		{
			control.HighlightRT.gameObject.SetActive(true);
		}

		public void StopControlHighlight()
		{
			SettingHighlighter.gameObject.SetActive(false);
		}

		public void StopRebindControlHighlight(ISettingsMenuControl control)
		{
			control.HighlightRT.gameObject.SetActive(false);
		}

		public PageItems NewPage(Page page)
		{
			var p = new PageItems();
			_pages[page] = p;
			_controlIndexTracker[page] = 0;
			_pagesInOrder.Add(page);
			return p;
		}

		public void ClearActiveControls(List<ISettingsMenuControl> controls)
		{
			foreach (ISettingsMenuControl control in controls)
			{
				switch (control)
				{
					case SettingsMenuHeader header:
						Pool_Header.ReturnSafe(header);
						break;
					case CheckboxControl checkbox:
						Pool_Checkbox.ReturnSafe(checkbox);
						break;
					case SliderControl slider:
						Pool_Slider.ReturnSafe(slider);
						break;
					case DropdownControl dropdown:
						Pool_Dropdown.ReturnSafe(dropdown);
						break;
					case InputFieldControl input_field:
						Pool_InputField.ReturnSafe(input_field);
						break;
					case ButtonControl button:
						Pool_Buttons.ReturnSafe(button);
						break;
				}
			}

			controls.Clear();

			EventSystem.current.SetSelectedGameObject(null);
			StopControlHighlight();
		}

		public void ClearActiveRebindUI(List<UnityEngine.Object> ui)
		{
			foreach (UnityEngine.Object control in ui)
			{
				switch (control)
				{
					case SingleRebindControl rebind_control:
						//OnControlRebound -= rebind_control.ProcessRebind;

						Pool_Rebind.ReturnSafe(rebind_control);
						break;
					case MultiRebindControl multi_rebind_control:
						//OnControlRebound -= multi_rebind_control.ProcessRebind;

						Pool_MultiRebind.ReturnSafe(multi_rebind_control);
						break;
					case RebindSectionTitle section_title:
						Pool_RebindSectionTitle.ReturnSafe(section_title);
						break;
					default:
						break;
				}
			}

			ui.Clear();

			//EventSystem.current.SetSelectedGameObject(null);
			//StopControlHighlight();
		}

		public void Setup(string text, ISettingsMenuControl control)
		{
			ActiveMenuControls.Add(control);
			control.RT.SetParent(ControlsRoot);

			if (control.Label != null)
			{
				control.Label.text = text;
			}

			control.OnSelected += data =>
			{
				if (data is PointerEventData) return;

				HighlightControl(control);
				ScrollTo(control.RT);
			};

			control.OnDeselected += data =>
			{
				StopControlHighlight();
			};
		}

		public void OnRebindInputSelected(BaseEventData data, ISettingsMenuControl control)
		{
			if (data is PointerEventData) return;

			HighlightRebindControl(control);
			RebindScrollTo(control.RT);
		}

		public void OnRebindInputDeselected(BaseEventData data, ISettingsMenuControl control)
		{
			StopRebindControlHighlight(control);
		}

		public void SpawnHeader(string text)
		{
			var obj = Pool_Header.Rent();

			Setup("- " + text + " -", obj);
		}

		public void SpawnButton(string text, Action onClick, Action<Selectable> onUpdate)
		{
			var obj = Pool_Buttons.Rent();

			Setup(text, obj);

			obj.OnClicked = null;
			obj.OnClicked = onClick;

			obj.OnUpdateUI = null;
			obj.OnUpdateUI = onUpdate;

			obj.UpdateUI();
		}

		public void SpawnCheckbox(string labelText, bool initialValue, Action<bool> OnChanged = null)
		{
			var obj = Pool_Checkbox.Rent();

			Setup(labelText, obj);
			obj.OnChanged = null;
			if (OnChanged != null) obj.OnChanged += OnChanged;

			obj.Set(initialValue);
		}

		public void SpawnSlider(string labelText, float min, float max, float initialValue, float increment = 1f, bool snapToInt = false, Action<float> OnChanged = null, int decimalPlaces = 1)
		{
			var obj = Pool_Slider.Rent();

			Setup(labelText, obj);
			obj.OnChanged = null;

			obj.Setup(min, max, increment, snapToInt, decimalPlaces);
			obj.Set(initialValue);
			if (OnChanged != null) obj.OnChanged += OnChanged;
		}

		public void SpawnDropdown(string labelText, List<string> options, int initialIndex, Action<int> OnChanged = null)
		{
			for (int i = 0; i < options.Count; i++) //TODO: FIGURE OUT A WAY TO OPTIMIZE THIS (SOME SORT OF BETTER, GLOBAL STRING REPLACEMENT ALGORITHM?)
			{
				string option = options[i];

				if (option == "VeryHigh")
				{
					option     = "Very High";
					options[i] = option;

					continue;
				}

				if (option == "CloseFit")
				{
					option     = "Close Fit";
					options[i] = option;

					continue;
				}

				if (option == "StableFit")
				{
					option     = "Stable Fit";
					options[i] = option;

					continue;
				}
			}

			var obj = Pool_Dropdown.Rent();

			Setup(labelText, obj);
			obj.OnChanged = null;
			if (OnChanged != null) obj.OnChanged += OnChanged;

			obj.Setup(options, initialIndex);
		}

		public void SpawnInputField(string labelText, string initialValue, Action<string> OnChanged = null)
		{
			var obj = Pool_InputField.Rent();

			Setup(labelText, obj);
			obj.OnChanged = null;
			if (OnChanged != null) obj.OnChanged += OnChanged;

			obj.Set(initialValue);
		}

		public void SpawnRebind(string labelText, string initialValue, Action<string> OnChanged = null) { }

		public enum Submenu
		{
			Main,
			Rebind,
			RebindPopup
		}

		public enum Page
		{
			Game,
			Camera,
			Audio,
			Input,
			Display,
			Network,
			Accessibility,
			Debug
		}

		public enum RebindMode
		{
			Keyboard,
			Mouse,
			Gamepad
		}

		public enum ControlType { Header, Button, Bool, Float, Int, String, Enum, FloatDropdown, AudioFloat }

		public class Item
		{
			public string label;
		}

		public abstract class ValueItem<T> : Item
		{
			public T         InitialValue;
			public Action<T> OnChanged;
		}

		public class HeaderItem : Item { }

		public class ButtonItem : Item
		{
			public Action action;
			public Action<Selectable> update;
		}

		public class BoolItem : ValueItem<bool> { }

		public class StringItem : ValueItem<string> { }

		public class FloatItem : ValueItem<float>
		{
			public float min;
			public float max;
			public float increment;
			public int   decimalPlaces;
		}

		public interface IEnumItem
		{
			string[] GetDropdownValues();
			int      GetInitialValue();
			void     OnValueChanged(object val);
		}

		public class EnumItem<T> : ValueItem<T>, IEnumItem where T : Enum
		{
			public string[] GetDropdownValues()        => typeof(T).GetEnumNames();
			public int      GetInitialValue()          => Convert.ToInt32(InitialValue);
			public void     OnValueChanged(object val) => OnChanged?.Invoke((T)val);
		}

		public class DropdownItem : ValueItem<int>
		{
			public (string, object)[] dropdown_values;
		}

		public class INIItem : Item
		{
			public ControlType      type;
			public INIFile.INIValue ini_value;

			public float min;
			public float max;
			public float increment;

			public (string, object)[] dropdown_values;

			public object associatedObject;
		}

		public class PageItems
		{
			public List<Item>      AllValues = new List<Item>();
			public SettingsMenuTab Tab;

			public PageItems Header(string text)
			{
				AllValues.Add(new HeaderItem
				{
					label = text,
				});
				return this;
			}

			public PageItems Button(string label, Action OnClicked = null, Action<Selectable> OnUpdate = null)
			{
				AllValues.Add(new ButtonItem
				{
					action = OnClicked,
					label  = label,
					update = OnUpdate
				});
				return this;
			}

			public PageItems AddBool(bool initial_val, string label, Action<bool> onChanged = null)
			{
				AllValues.Add(new BoolItem
				{
					label        = label,
					InitialValue = initial_val,
					OnChanged    = onChanged
				});
				return this;
			}

			public PageItems AddFloat(float initial_val, string label, float min, float max, float increment = 0.1f, Action<float> onChanged = null, int decimalPlaces = 1)
			{
				AllValues.Add(new FloatItem
				{
					label         = label,
					InitialValue  = initial_val,
					min           = min,
					max           = max,
					increment     = increment,
					OnChanged     = onChanged,
					decimalPlaces = decimalPlaces
				});
				return this;
			}

			public PageItems AddEnum<T>(T initial_val, string label, Action<T> onChanged = null) where T : Enum
			{
				AllValues.Add(new EnumItem<T>
				{
					label        = label,
					InitialValue = initial_val,
					OnChanged    = onChanged,
				});
				return this;
			}

			public PageItems AddINIBool(INIBool val, string label = null)
			{
				AllValues.Add(new INIItem
				{
					type      = ControlType.Bool,
					ini_value = val,
					label     = label,
				});
				return this;
			}

			public PageItems AddINIFloat(INIFloat val, float min, float max, float increment = 0.1f, string label = null)
			{
				AllValues.Add(new INIItem
				{
					type      = ControlType.Float,
					ini_value = val,
					min       = min,
					max       = max,
					increment = increment,
					label     = label,
				});
				return this;
			}

			public PageItems AddAudioFloat(INIFloat val, float min, float max, object audioSource, float increment = 0.1f, string label = null)
			{
				AllValues.Add(new INIItem
				{
					type             = ControlType.AudioFloat,
					ini_value        = val,
					min              = min,
					max              = max,
					associatedObject = audioSource,
					increment        = increment,
					label            = label,
				});
				return this;
			}

			public PageItems AddINIString(INIString val, string label = null)
			{
				AllValues.Add(new INIItem
				{
					type      = ControlType.String,
					ini_value = val,
					label     = label,
				});
				return this;
			}


			public PageItems AddINIFloatDropdown(INIFloat val, string label = null, params (string, object)[] values)
			{
				AllValues.Add(new INIItem
				{
					type            = ControlType.FloatDropdown,
					ini_value       = val,
					dropdown_values = values,
					label           = label,
				});
				return this;
			}
		}
	}
}
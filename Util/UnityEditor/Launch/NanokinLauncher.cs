using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Anjin.Util;
using Combat;
using Combat.Launch;
using Combat.Startup;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util.Addressable;
using Util.Odin.Attributes;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using Anjin.Nanokin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;

#endif

namespace Util.UnityEditor.Launch
{
	public class NanokinLauncher : SerializedScriptableObject
	{
		// CORE SCENES
		// ----------------------------------------
		private const string GLOBAL_PATH      = "Assets/Scenes/Globals/Global.unity";
		private const string STARTUP_PATH     = "Assets/Scenes/application_startup_windows.unity";
		private const string TITLESCREEN_NAME = "MENU_Title";

		// MENU SCENES
		// ----------------------------------------
		private const string SPLICER_BARREL_PATH = "Assets/Scenes/application_startup_windows.unity";
		private const string EQUIP_LIMB_PATH     = "Assets/Scenes/application_startup_windows.unity";
		private const string FORMATION_PATH      = "Assets/Scenes/application_startup_windows.unity";
		private const string STICKER_PATH        = "Assets/Scenes/application_startup_windows.unity";
		private const string QUEST_PATH          = "Assets/Scenes/application_startup_windows.unity";

		// ETC PATHS
		// ----------------------------------------
		private const  string DEFAULT_ARENA_PATH          = "Assets/Scenes/Combat/Maps/Debug_Arena1.unity";
		private const  string COMBAT_INJECTION_SCENE_NAME = "debug_battle_launcher";
		private static string COMBAT_SCENEPATH => $"Assets/Editor/_local/{COMBAT_INJECTION_SCENE_NAME}.unity";
		private static string COMBAT_DIRPATH   => $"{Application.dataPath}/Editor/_local";

		// COMBAT
		// ----------------------------------------
		private const string COMBAT_ARENA_NOT_FOUND_ERR   = "There was no arena found to inject a battle into. Open a scene which contains an Arena component somewhere.";
		private const string COMBAT_INTRUSIVE_SCENES_WARN = "irrelevant scenes were found: \n\n{SCENES}\n\n. Do you whish to close them before starting?";

		private static readonly string[] CombatWhitelist = {"Global", "Arena", "UI_", "MENU_", COMBAT_INJECTION_SCENE_NAME};

		private static readonly HashSet<string> _combatLastLaunchScenes = new HashSet<string>();

		// ------------------------------------------------------------
#if UNITY_EDITOR
		[ShowInInspector]
		[PropertyTooltip("Load in extra scenes to avoid loading them every launch. (menus, ui, etc.)")]
		public bool ExtraScenes
		{
			get => InternalEditorConfig.Instance.LauncherPreload;
			set => InternalEditorConfig.Instance.LauncherPreload = value;
		}
#endif

		[Title("Launch")]
		[PropertyOrder(0)]
		[Button(ButtonSizes.Large)]
		[ReadOnly]
#if UNITY_EDITOR
		[MenuItem("Nanokin/Launch/Title Screen")]
#endif
		public void LaunchTitle()
		{
			throw new NotImplementedException();
		}

		[PropertyOrder(1)]
		[Button(ButtonSizes.Large)]
		[ReadOnly]
#if UNITY_EDITOR
		[MenuItem("Nanokin/Launch/New Save")]
#endif
		public void LaunchNewSave()
		{
			throw new NotImplementedException();
		}

		[PropertyOrder(2)]
		[DarkBox(true), Inline]
		[Space]
		[ReadOnly]
		public LoadSave_ load_save = new LoadSave_();

		[PropertyOrder(3)]
		[DarkBox(true), Inline]
		[Space]
		public LaunchMap_ launch_level = new LaunchMap_();

		[PropertyOrder(4)]
		[DarkBox(true), Inline]
		[Space]
		public LaunchCombat_ launch_combat = new LaunchCombat_();

		// ------------------------------------------------------------

#if UNITY_EDITOR
		[PropertyOrder(5)]
		[Title("State")]
		[GUIColor("yellow")]
		[Button(ButtonSizes.Large)]
		[MenuItem("Nanokin/State/Clear", false, 30)]
		public static void Clear()
		{
			EditorSceneManager.SaveOpenScenes();

			if (Instance.ExtraScenes)
			{
				var entries = Addressables2.FindEntriesInEditor($"Scenes");

				foreach (string scene in Addresses.PRELOAD_SCENES)
				{
					if (SceneManager.GetSceneByName(scene).IsValid())
						continue; // Already loaded

					foreach (AddressableAssetEntry entry in entries)
					{
						if (entry.address == $"Scenes/{scene}")
						{
							Scene scene1 = EditorSceneManager.OpenScene(entry.AssetPath, OpenSceneMode.Additive);
							EditorUtil.SetExpanded(scene1, false);
							break;
						}
					}
				}

				// Remove unnecessary scenes
				for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
				{
					Scene scene = SceneManager.GetSceneAt(i);
					if (scene.IsValid() && !Addresses.PRELOAD_SCENES.Contains(scene.name))
					{
						EditorSceneManager.CloseScene(scene, true);
					}
				}
			}
			else
			{
				EditorUtil.OpenSceneCollapsed(GLOBAL_PATH, OpenSceneMode.Single);
			}

			SelectLauncher();
		}

#endif

#if UNITY_EDITOR
		[PropertyOrder(6)]
		[Button(ButtonSizes.Large), GUIColor("yellow")]
		[MenuItem("Nanokin/State/Windows Startup", false, 30)]
		public static void SetStartup()
		{
			EditorUtil.OpenSceneCollapsed(STARTUP_PATH, OpenSceneMode.Single);
			SelectLauncher();
		}
#endif

#if UNITY_EDITOR
		[PropertyOrder(7)]
		[Button(ButtonSizes.Large), GUIColor("yellow")]
		[MenuItem("Nanokin/State/Title Screen", false, 30)]
		public static void SetTitle()
		{
			Clear();
			EditorUtil.EnsureSceneCollapsed(TITLESCREEN_NAME);
			SelectLauncher();
		}
#endif

		[PropertyOrder(8)]
		[DarkBox(true), Inline]
		[Space]
		public SetLevel_ set_level = new SetLevel_
		{
			level = null
		};


		[PropertyOrder(9)]
		[DarkBox(true), Inline]
		[Space]
		public SetArena_ set_arena = new SetArena_()
		{
			arena = ""
		};

		// ------------------------------------------------------------

		[Title("Disk Data")]
		[PropertyOrder(10)]
		[DarkBox(true), Inline]
		[Space]
		[ReadOnly]
		public SetOptionfile set_optionfile = new SetOptionfile
		{
			list = new List<string>()
		};

		[PropertyOrder(11)]
		[DarkBox(true), Inline]
		[Space]
		[ReadOnly]
		public SetSavefile set_savefile = new SetSavefile
		{
			list = new List<string>()
		};

		private static NanokinLauncher _instance;

		// ------------------------------------------------------------
#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void Init()
		{
			_instance = Instance;
			_instance.init();

			Selection.selectionChanged += DetectSelection;

			DetectSelection();

			EditorSceneManager.sceneOpened += (scene, ev) =>
			{
				if (!scene.isLoaded) return;
				if (Application.isPlaying) return;

				if (!Combat_IsWhitelisted(scene, CombatWhitelist))
				{
					Combat_CloseInjection();
				}
			};

			EditorSceneManager.sceneClosed += scene =>
			{
				//Debug.Log("NanokinLauncher: sceneClosed"); // Trying to debug a crash
				if (scene.name == COMBAT_INJECTION_SCENE_NAME) return;
				if (Application.isPlaying) return;
				if (!scene.isLoaded) return;

				Combat_CloseInjection();
			};

			EditorSceneManager.sceneClosing += (scene, removingScene) =>
			{
				if (EditorApplication.isPlaying) return;
				if (scene.name == COMBAT_INJECTION_SCENE_NAME) return;
				if (Combat_IsWhitelisted(scene, CombatWhitelist)) return;

				Combat_CloseInjection();
			};
		}
#endif

		private void init()
		{
			set_savefile.list   = set_savefile.list ?? new List<string>();
			set_optionfile.list = set_optionfile.list ?? new List<string>();
			set_arena.arena     = set_arena.arena ?? "Scenes/Debug_Arena1";
			launch_combat.arena = launch_combat.arena ?? "Scenes/Debug_Arena1";
		}

#if UNITY_EDITOR
		private static void DetectSelection()
		{
			if (Selection.activeObject is SceneAsset scene && Selection.activeObject.name.Contains("Arena"))
			{
				string n = scene.GetAddressInEditor();

				Instance.launch_combat.arena = n;
				Instance.set_arena.arena     = n;
			}

			if (Selection.activeObject is LevelManifest lman)
			{
				Instance.set_level.level    = lman;
				Instance.launch_level.level = lman;
			}

			if (Selection.activeObject is BattleRecipeAsset brecipe)
			{
				Instance.launch_combat.recipe = brecipe;
			}
		}
#endif

		public static NanokinLauncher Instance => _instance ? _instance : _instance = CreateInstance<NanokinLauncher>();

#if UNITY_EDITOR
		[MenuItem("Nanokin/Launcher"), Shortcut("Nanokin/Launcher", KeyCode.L, ShortcutModifiers.Action)]
		public static void SelectLauncher()
		{
			Selection.activeObject = Instance;
		}
#endif

		// ------------------------------------------------------------

		[Serializable]
		public struct SetOptionfile
		{
			[LabelText("Set Options")]
			[Button]
			[PropertyOrder(-1)]
			public void Set()
			{
				throw new NotImplementedException();
			}

			[ShowInInspector]
			[ReadOnly]
			public List<string> list;
		}

		[Serializable]
		public struct SetSavefile
		{
			[LabelText("Set Save")]
			[Button]
			[PropertyOrder(-1)]
			public void Set()
			{
				throw new NotImplementedException();
			}

			[ReadOnly]
			[HideLabel]
			public List<string> list;
		}

		[Serializable]
		public struct LoadSave_
		{
			[PropertyOrder(-1)]
			[Button(ButtonSizes.Large)]
			[LabelText("Load Save")]
			public void launch()
			{
				throw new NotImplementedException();
			}

			public string name;
		}

		[Serializable]
		public struct LaunchMap_
		{
			[PropertyOrder(-1)]
			[Button(ButtonSizes.Large)]
			[LabelText("Overworld")]
			[EnableIf("@level != null")]
			public void launch()
			{
#if UNITY_EDITOR
				SetLevel(level);
				EditorApplication.EnterPlaymode();
#endif
			}

			public LevelManifest level;
		}

		[Serializable]
		public struct LaunchCombat_
		{
			[PropertyOrder(-1)]
			[Button(ButtonSizes.Large)]
			[LabelText("Combat")]
			[EnableIf("@arena != null && recipe != null")]
			public void launch()
			{
#if UNITY_EDITOR
				Clear();
				SetArena(arena);
				LaunchCombat(recipe.Value);
#endif
			}

			[AddressFilter(prefix: "Scenes/", contains: "Arena")]
			public string arena;

			public BattleRecipeAsset recipe;
		}

		[Serializable]
		public struct SetLevel_
		{
			[PropertyOrder(-1)]
			[Button(ButtonSizes.Large)]
			[LabelText("Set Level")]
			[EnableIf("@level != null")]
			public void set()
			{
#if UNITY_EDITOR
				SetLevel(level);
#endif
			}

			public LevelManifest level;
		}

		[Serializable]
		public struct SetArena_
		{
			[AddressFilter(contains: "Arena")]
			public string arena;

			[LabelText("Set Arena")]
			[PropertyOrder(-1)]
			[Button(ButtonSizes.Large)]
			[EnableIf("Enable")]
			public void Set()
			{
#if UNITY_EDITOR
				SetArena(arena);
#endif
			}

#if UNITY_EDITOR
			[UsedImplicitly]
			private bool Enable => arena?.Length > 0;
#endif
		}

		// IMPLEMENTATIONS
		// ------------------------------------------------------------

#if UNITY_EDITOR
		public static void SetArena(string address)
		{
			Clear();
			Scene scene = EditorUtil.OpenSceneCollapsed(Addressables2.GetPathByAddressEditor(address), OpenSceneMode.Additive);
			SceneManager.SetActiveScene(scene);

			SelectLauncher();
		}

		public static void SetLevel(LevelManifest level)
		{
			if (!level) return;

			//Clear();

			List<SceneSetup> current = EditorSceneManager.GetSceneManagerSetup().ToList();

			int glbIndex = current.FindIndex(x => x.path == Addresses.Scene_Global_Path);

			bool  unloadedInHierarchy = glbIndex != -1 && !current[glbIndex].isLoaded;

			Scene global = default;

			// Global not currently opened. We should open it
			if (unloadedInHierarchy || glbIndex == -1) {
				Scene first = EditorSceneManager.GetSceneAt(0);
				global = EditorUtil.OpenSceneCollapsed(Addresses.Scene_Global_Path, OpenSceneMode.Additive);

				if (!unloadedInHierarchy) {
					EditorSceneManager.MoveSceneBefore(global, first);
					glbIndex = 0;
				}
			} else {
				global = EditorSceneManager.GetSceneAt(glbIndex);
			}



			List<SceneReference> allToLoad = new List<SceneReference>();
			allToLoad.Add(level.MainScene);

			foreach (SceneReference sceneref in level.SubScenes) {
				allToLoad.Add(sceneref);
			}

			Scene last = global;
			foreach (SceneReference sceneRef in allToLoad) {
				if(sceneRef.IsValid) {
					var next = EditorUtil.OpenSceneCollapsed(sceneRef.DetectAssetPath(), OpenSceneMode.Additive);
					EditorSceneManager.MoveSceneAfter(next, last);
					last = next;

				}
			}

			/*List<Scene>

			EditorUtil.OpenSceneCollapsed()*/


			/*if (level)
			{
				Scene mainScene = EditorUtil.OpenSceneCollapsed(level.MainScene.DetectAssetPath(), OpenSceneMode.Additive);
				SceneManager.SetActiveScene(mainScene);

				foreach (SceneReference sceneref in level.SubScenes)
				{
					EditorUtil.OpenSceneCollapsed(sceneref.DetectAssetPath(), OpenSceneMode.Additive);
				}
			}



			SelectLauncher();*/
		}

		private static void CloseScenesExcept(string name)
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.name.Contains(name)) continue;

				EditorSceneManager.CloseScene(scene, true);
			}
		}
#endif


		public static void LaunchCombat(BattleRecipe recipe)
		{
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				// Inject in edit mode (component.start)
				// ----------------------------------------

				InternalEditorConfig.Instance.ForceSerializeAsset(); // Can't remember why this is needed

				if (!Combat_FindArena(out Arena arena))
					return;

				EditorUtil.EnsureSceneCollapsed("Global");
				EditorUtil.EnsureSceneCollapsed("UI_Combat");
				EditorUtil.EnsureSceneCollapsed("UI_Triangle");

				if (!Combat_IsDirectLaunch() && Combat_IsLaunchSetupDifferent(out List<Scene> nonCombatScenes)) // We don't wanna ask this every time if we specifically wanna keep other scenes loaded
				{
					string listStr = "";
					for (var i = 0; i < nonCombatScenes.Count; i++)
					{
						Scene name = nonCombatScenes[i];
						if (string.IsNullOrEmpty(name.name))
							listStr += "Untitled";
						else
							listStr += name.name;

						listStr += "\n";
					}


					int result = EditorUtility.DisplayDialogComplex("Intrusive Scene Warning", COMBAT_INTRUSIVE_SCENES_WARN.Replace("{SCENES}", listStr), "Close Scenes", "Cancel", "Keep");
					switch (result)
					{
						case 0:
							foreach (Scene s in nonCombatScenes)
								EditorSceneManager.CloseScene(s, true);
							break;

						case 1:
							// Cancel everything
							return;

						case 2:
							break;
					}

					Combat_UpdateLastLaunchSetup();
				}

				Combat_ClearInjection();

				Scene launcherScene = Combat_OpenInjection();
				launcherScene.Clear();

				InjectCombat(recipe, launcherScene);

				//EditorSceneManager.SaveScene(scene, SCENE_PATH, false);
				EditorApplication.EnterPlaymode();
			}
			else
			{
				// Inject at runtime (core.play)
				// ----------------------------------------
				if (!Combat_FindArena(out Arena arena))
					return;

				InjectCombat(recipe, arena.gameObject.scene);
			}
#endif
		}

		private static void InjectCombat(BattleRecipe recipe, Scene scene)
		{
#if UNITY_EDITOR
			// Create new injection
			var goInjection = new GameObject("[BATTLE LAUNCHING INJECT]");
			SceneManager.MoveGameObjectToScene(goInjection, scene);

			BattleLauncherInjection injection = goInjection.AddComponent<BattleLauncherInjection>();
			injection.Recipe = recipe;

			InternalEditorConfig.Instance.LastLauncherRecipe = recipe;
#endif
		}

		private static void Combat_CloseInjection()
		{
#if UNITY_EDITOR
			Scene scene = SceneManager.GetSceneByName(COMBAT_INJECTION_SCENE_NAME);
			if (scene.isLoaded && scene.IsValid())
			{
				EditorSceneManager.CloseScene(scene, true);
			}

			/*if (temp_scene.isLoaded) {
				EditorSceneManager.CloseScene(temp_scene, true);
			}*/
#endif
		}

#if UNITY_EDITOR
		public static Scene Combat_OpenInjection()
		{
			// use already loaded scene
			Scene scene = SceneManager.GetSceneByName(COMBAT_INJECTION_SCENE_NAME);
			if (scene.isLoaded && scene.IsValid())
				return scene;

			// load existing editor scene
			if (AssetDatabase.LoadAssetAtPath<SceneAsset>(COMBAT_SCENEPATH) != null)
				return EditorSceneManager.OpenScene(COMBAT_SCENEPATH, OpenSceneMode.Additive);

			// doesn't exist yet
			scene      = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			scene.name = COMBAT_INJECTION_SCENE_NAME;

			// The directory must exist before trying to call EditorSceneManager.SaveScene(...)!
			Directory.CreateDirectory(COMBAT_DIRPATH);

			if (EditorSceneManager.SaveScene(scene, COMBAT_SCENEPATH, true))
			{
				Debug.LogError($"Battle injection scene \"{COMBAT_INJECTION_SCENE_NAME}\" did not save!");
			}

			// We need to close the scene, then reopen it so the one loaded is linked to the one on disk
			EditorSceneManager.CloseScene(scene, true);
			scene = EditorSceneManager.OpenScene(COMBAT_SCENEPATH, OpenSceneMode.Additive);

			return scene;
		}
#endif

		/// <summary>
		/// Clear the existing injection in the injection scene.
		/// </summary>
		private static void Combat_ClearInjection()
		{
#if UNITY_EDITOR
			Scene scene = SceneManager.GetSceneByName(COMBAT_INJECTION_SCENE_NAME);
			if (!scene.isLoaded || !scene.IsValid())
				return;

			BattleLauncherInjection existing = scene.FindRootComponent<BattleLauncherInjection>();
			if (existing)
			{
				if (Application.isPlaying) Object.Destroy(existing.gameObject);
				else Object.DestroyImmediate(existing.gameObject);
			}
#endif
		}

#if UNITY_EDITOR
		[CanBeNull]
		public static bool Combat_FindArena([CanBeNull] out Arena arena)
		{
			arena = default;

			// Search for an existing arena
			// ----------------------------------------
			for (var idxScene = 0; idxScene < SceneManager.sceneCount; idxScene++)
			{
				Scene scene = SceneManager.GetSceneAt(idxScene);

				if (!scene.isLoaded)
					continue;

				foreach (GameObject rootGameObject in scene.GetRootGameObjects())
				{
					Arena a = rootGameObject.GetComponent<Arena>();

					if (a != null)
					{
						arena = a;
						return true;
					}
				}
			}

			// Load the debug arena
			// ----------------------------------------
			Scene debugScene = EditorSceneManager.OpenScene(DEFAULT_ARENA_PATH, OpenSceneMode.Additive);
			if (!debugScene.IsValid())
			{
				Debug.LogError($"Could not load debug arena at path '{DEFAULT_ARENA_PATH}'");
				return false;
			}

			return debugScene.FindRootComponent<Arena>();
		}
#endif

		private static bool Combat_IsWhitelisted(Scene scene, string[] list)
		{
			return list.Any(w => scene.name.Contains(w));
		}

		private static bool Combat_IsDirectLaunch()
		{
#if UNITY_EDITOR
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (Combat_IsWhitelisted(scene, CombatWhitelist)) continue;

				return false;
			}

#endif
			return true;
		}

		public static void Combat_UpdateLastLaunchSetup()
		{
#if UNITY_EDITOR
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.name == "Global") continue;
				if (scene.name.Contains("Arena")) continue;

				_combatLastLaunchScenes.Add(scene.name);
			}
#endif
		}

		public static bool Combat_IsLaunchSetupDifferent([NotNull] out List<Scene> intrusiveScenes)
		{
			intrusiveScenes = new List<Scene>();

#if UNITY_EDITOR
			if (Application.isPlaying)
				return false;

			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!Combat_IsWhitelisted(scene, CombatWhitelist) && scene.isLoaded && !_combatLastLaunchScenes.Contains(scene.name))
				{
					intrusiveScenes.Add(scene);
				}
			}

#endif
			return intrusiveScenes.Count > 0;
		}

#if UNITY_EDITOR
		private static Color green  => Color.green;
		private static Color yellow => Color.yellow;
#endif
	}
}
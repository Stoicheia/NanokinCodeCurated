using System.Collections.Generic;
using System.Linq;
using Anjin.Audio;
using Anjin.Nanokin.Park;
using Anjin.Regions;
using Anjin.Util;
using Combat.Launch;
using Data.Overworld;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Util;
using Util.Addressable;
using Util.UnityEditor;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using Util.UnityEditor.Launch;

#endif

[CreateAssetMenu(menuName = "Anjin/Level Manifest")]
public class LevelManifest : SerializedScriptableObject, IAddressable
{
	public string  DisplayName;
	public Areas   Area;

	[FormerlySerializedAs("Subarea")]
	public LevelID Level;

	public Sprite CardSprite;

	public string LevelName {
		get {
			if (!DisplayName.IsNullOrWhitespace()) return DisplayName;
			return name;
		}
	}

	/// <summary>
	/// Should get loaded first before any other scenes
	/// </summary>
	public SceneReference MainScene;

	/// <summary>
	/// Loaded on the order here.
	/// </summary>
	public List<SceneReference> SubScenes;     // TODO Addressable (verify uses)
	public List<SceneReference>   ArenaScenes; // TODO Addressable (verify uses)
	public List<RegionGraphAsset> RegionGraphs;
	public List<LuaAsset>         Scripts;

	public string            ActorReferencePath = "";
	public EncounterSettings EncounterLayer;

	[Title("Audio")]
	public AudioProfile MusicProfile;
	public AudioProfile AmbientProfile;

	[Title("ParkAI")]
	public bool ParkAIEnabled = true;
	public List<RegionGraphAsset> ParkAIGraphs;
	[MinValue(0)]
	public int PeepCount;

	public string Address { get; set; }


#if UNITY_EDITOR
	[Title("Workflow")]
	[Button]
	[HideInPlayMode]
	public void AddSubscene()
	{
		string basedir  = Path.GetDirectoryName(MainScene.DetectAssetPath());
		string savepath = EditorUtility.SaveFilePanel($"Add new {MainScene.SceneName} subscene...", basedir, $"{MainScene.SceneName}_Subscene", "unity");
		if (savepath != "")
		{
			string subsceneName = Path.GetFileNameWithoutExtension(savepath);

			// Create the scene
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

			// Save the scene
			if (EditorSceneManager.SaveScene(scene, savepath))
			{
				var sceneref = new SceneReference(subsceneName);
				SubScenes.Add(sceneref);

				AssetDatabase.Refresh();
				sceneref.AddToBuildSettings();

				EditorUtil.CollapseAllScenes(false);

				EditorSceneManager.CloseScene(scene, true);
				EditorSceneManager.OpenScene(sceneref.DetectAssetPath(), OpenSceneMode.Additive);
			}
		}
	}

	[Button]
	[HideInPlayMode]
	public void AddScript()
	{
		string guid = this.GetAssetGUID();
		string path = AssetDatabase.GUIDToAssetPath(guid);

		if (path == null)
			return;

		string newScript = Path.Combine(Path.GetDirectoryName(path), "New Script.lua");
		newScript = FileUtilAnjin.NextAvailableFilename(newScript);
		File.WriteAllText(newScript, @"
-- A script to handle some functions and gameplay interactions.
-- Add functions here and they can be called from any
-- LuaOn component. They can also receive events
-- about the game.

function on_level_start()
end

function on_player_spawn(player, party)
end
");
		AssetDatabase.Refresh();

		LuaAsset asset = AssetDatabase.LoadAssetAtPath<LuaAsset>(newScript);
		Scripts.Add(asset);
	}


	private bool NotInDB => LevelManifestDatabase.LoadedDB == null ||
	                        !LevelManifestDatabase.LoadedDB.Manifests.Contains(this);

	[ShowIf("NotInDB"), GUIColor(1, 0, 0), PropertyOrder(-1)]
	[Button(ButtonSizes.Large, Name = "NOT IN DATABASE! CLICK TO FIX!")]
	public void EnsureInDatabase()
	{
		LevelManifestDatabase.LoadedDB.Manifests.AddIfNotExists(this);
		EditorUtility.SetDirty(LevelManifestDatabase.LoadedDB);
	}

	[Button(ButtonSizes.Large), GUIColor(0, 1, 0), PropertyOrder(-2)]
	public void Play()
	{
		NanokinLauncher.SetLevel(this);
		EditorApplication.EnterPlaymode();
	}


	[Button(ButtonSizes.Large), GUIColor(0, 1, 0), PropertyOrder(-1), HorizontalGroup("OpenButtons")]
	public void Open()
	{
		NanokinLauncher.SetLevel(this);
	}

	[Button(ButtonSizes.Large), GUIColor(0, 1, 0), PropertyOrder(-1), HorizontalGroup("OpenButtons")]
	public void OpenWithoutSubscenes()
	{
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene scene = SceneManager.GetSceneAt(i);
			if (scene.name != "Global")
			{
				EditorSceneManager.CloseScene(scene, true);
			}
		}

		Scene mainScene = EditorUtil.OpenSceneCollapsed(EditorBuildSettings.scenes.First(s => s.path.Contains(MainScene.SceneName)).path, OpenSceneMode.Additive);
		SceneManager.SetActiveScene(mainScene);

		// Note: the scenes are expanded by default when opening, so we have to use this SetExpanded utility to make it not a pain in the ass.
		foreach (SceneReference sceneref in SubScenes)
		{
			EditorUtil.OpenSceneCollapsed(EditorBuildSettings.scenes.First(s => s.path.Contains(sceneref.SceneName)).path, OpenSceneMode.AdditiveWithoutLoading);
		}

		foreach (SceneReference sceneref in ArenaScenes)
		{
			EditorUtil.OpenSceneCollapsed(EditorBuildSettings.scenes.First(s => s.path.Contains(sceneref.SceneName)).path, OpenSceneMode.AdditiveWithoutLoading);
		}
	}

#endif
}
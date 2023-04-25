using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;


[Serializable, Inline]
public struct SceneReference
{
	[FormerlySerializedAs("sceneName")]
	public string SceneName;

	public SceneReference(string sceneName)
	{
		SceneName = sceneName;
	}


	public bool IsInBuild =>
		//( SceneManager.GetSceneByName(sceneName).buildIndex < SceneManager.sceneCountInBuildSettings );
		Application.CanStreamedLevelBeLoaded(SceneName);

	public override string ToString()
	{
		return $"SceneReference({SceneName})";
	}

	public bool IsValid => !string.IsNullOrWhiteSpace(SceneName);

	public bool IsInvalid => string.IsNullOrWhiteSpace(SceneName);

	public static implicit operator SceneReference(string sceneName)
	{
		return new SceneReference(sceneName);
	}

	public static implicit operator string(SceneReference sceneReference)
	{
		return sceneReference.SceneName;
	}

#if UNITY_EDITOR
	public void AddToBuildSettings()
	{
		EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
		string                     path        = DetectAssetPath();

		// Check if the scene is already in the build settings.
		for (var i = 0; i < buildScenes.Length; i++)
		{
			if (buildScenes[i].path == path && buildScenes[i].enabled)
				return;
		}

		Array.Resize(ref buildScenes, buildScenes.Length + 1);
		buildScenes[buildScenes.Length - 1] = new EditorBuildSettingsScene(path, true);

		EditorBuildSettings.scenes = buildScenes;
	}

	public string DetectAssetPath()
	{
		// We will attempt to detect the path using magic.
		if (SceneName == null)
			return null;

		string tmpName = SceneName;

		// Try getting from build settings first.
		EditorBuildSettingsScene scene = EditorBuildSettings.scenes.FirstOrDefault(s => Path.GetFileName(s.path) == tmpName);

		if (scene != null)
			return scene.path;
		else
		{
			// Try searching the asset database otherwise.
			string assetPath = AssetDatabase.FindAssets($"t:sceneasset {tmpName}")
				.FirstOrDefault(guid =>
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					string name = Path.GetFileNameWithoutExtension(path);

					return name == tmpName;
				});
			if (assetPath != null)
			{
				return AssetDatabase.GUIDToAssetPath(assetPath);
			}
		}

		return null;
	}
#endif
}
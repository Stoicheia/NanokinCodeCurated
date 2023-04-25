#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using Anjin.Nanokin;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util.Addressable;

namespace Util.UnityEditor
{
	public static class EditorUtil
	{
		public static void SetExpanded(Scene scene, bool expand)
		{
			foreach (var window in Resources.FindObjectsOfTypeAll<SearchableEditorWindow>())
			{
				if (window.GetType().Name != "SceneHierarchyWindow")
					continue;

				var method = window.GetType().GetMethod("SetExpandedRecursive",
					BindingFlags.Public |
					BindingFlags.NonPublic |
					BindingFlags.Instance, null,
					new[] { typeof(int), typeof(bool) }, null);

				if (method == null)
				{
					Debug.LogError("Could not find method 'UnityEditor.SceneHierarchyWindow.SetExpandedRecursive(int, bool)'.");
					return;
				}

				object sceneHandle = GetHandle(scene);
				method.Invoke(window, new[] { sceneHandle, expand });
			}
		}

		private static object GetHandle(Scene scene)
		{
			var field = scene.GetType().GetField("m_Handle", BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
			{
				Debug.LogError("Could not find field 'int UnityEngine.SceneManagement.Scene.m_Handle'.");
				return null;
			}

			return field.GetValue(scene);
		}

		public static Scene EnsureSceneCollapsed(string name_or_path)
		{
			Scene scene = EnsureScene(name_or_path);
			SetExpanded(scene, false);

			return scene;
		}

		private static Scene EnsureScene(string name_or_path)
		{
			for (var i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				if (scene.path == name_or_path)
				{
					return scene;
				}
			}

			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (scene.path.Contains(name_or_path))
				{
					return OpenScene(scene.path);
				}
			}

			return OpenScene(name_or_path);
		}

		public static Scene OpenScene(string name_or_path, OpenSceneMode mode = OpenSceneMode.Additive)
		{
			bool is_path = name_or_path.Contains("/");
			if (is_path)
				return EditorSceneManager.OpenScene(name_or_path, mode);

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			foreach (AddressableAssetGroup g in settings.groups)
			{
				foreach (AddressableAssetEntry e in g.entries)
				{
					if (e.address.Contains(Addresses.ScenePrefix) && e.address.Contains(name_or_path))
					{
						return EditorSceneManager.OpenScene(e.AssetPath, mode);
					}
				}
			}

			return EditorSceneManager.OpenScene(name_or_path, mode);
		}

		public static Scene OpenSceneCollapsed(string path, OpenSceneMode mode)
		{
			Scene scene = EditorSceneManager.OpenScene(path, mode);
			SetExpanded(scene, false);

			return scene;
		}

		public static Scene OpenSceneCollapsedAsync(string name, OpenSceneMode mode)
		{
			var addresses = Addressables2.FindEntriesInEditor($"Scenes/{name}");
			if (addresses.Count == 0)
				return new Scene();

			AddressableAssetEntry entry = addresses.First();

			Scene scene = EditorSceneManager.OpenScene(entry.AssetPath, mode);
			SetExpanded(scene, false);

			return scene;
		}

		public static void CollapseAllScenes(bool b)
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					SetExpanded(scene, !b);
				}
			}
		}

		public static void SetProjectSearch(string text)
		{
			// Set project search
			object projectBrowser = typeof(ProjectWindowUtil).GetMethod("GetProjectBrowserIfExists", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
			Assembly.GetAssembly(typeof(EditorWindow)).GetType("UnityEditor.ProjectBrowser")?.GetMethod("SetSearch", new[] { typeof(string) }).Invoke(projectBrowser, new[] { text });
		}

		public static string GetProjectSearch()
		{
			// Set project search
			object projectBrowser = typeof(ProjectWindowUtil).GetMethod("GetProjectBrowserIfExists", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
			return (string)Assembly.GetAssembly(typeof(EditorWindow)).GetType("UnityEditor.ProjectBrowser")?.GetField("m_SearchFieldText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(projectBrowser);
		}

		[MenuItem("Tools/Hierarchy/Collapse All")]
		public static void MI_CollapseAllScenes()
		{
			CollapseAllScenes(true);
		}

		[MenuItem("Tools/Hierarchy/Collapse All & Unselect")]
		public static void MI_CollapseAllScenesUnselect()
		{
			CollapseAllScenes(true);
			Selection.activeObject = null;
		}
	}
}
#endif
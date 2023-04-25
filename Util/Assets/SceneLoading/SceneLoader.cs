using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;

#endif

// using Cysharp.Threading.Tasks;

namespace Anjin.Utils
{
	/// <summary>
	/// Provides a convenient wrapper around Unity's async scene management and utility methods.
	/// </summary>
	public class SceneLoader
	{
		/// <summary>
		/// Get a scene with a gameobject that has a component that is integral to driving the scene.
		/// Examples:
		/// 	- Shop UI (SCENE) + ShopUI (COMPONENT)
		/// 	- Victory (SCENE) + VictoryScreen (COMPONENT)
		/// </summary>
		/// <param name="sceneName"></param>
		/// <param name="onLoaded"></param>
		/// <typeparam name="T"></typeparam>
		public static AsyncSceneOperation GetDriverScene<T>(string sceneName, Action<Scene, T> onLoaded)
		{
			void OnSceneLoaded(Scene scene)
			{
				GameObject[]   rootGameObjects = scene.GetRootGameObjects();
				IEnumerable<T> components      = rootGameObjects.Select(go => go.GetComponent<T>());
				T              value           = components.First(component => component != null);

				onLoaded(scene, value);
			}


			Scene sceneToLoad = SceneManager.GetSceneByName(sceneName);

			AsyncSceneOperation op = sceneToLoad.isLoaded
				// Dirty trick because we are mixing async and sync code.
				// We want to still be able to use the complete event, however it would not be hooked yet if we called OnLoaded here.
				// MockSceneOperation will invoke onLoaded as soon as someone attempts to hook it.
				? new MockSceneOperation(sceneToLoad).OnScene(OnSceneLoaded)
				: new SceneLoading(sceneName, LoadSceneMode.Additive).OnScene(OnSceneLoaded);

			return op;
		}

		/// <summary>
		/// Require that a particular scene be loaded at least once. If it is already loaded, nothing new is loaded.
		/// </summary>
		/// <param name="sceneName"></param>
		/// <returns></returns>
		public static AsyncSceneOperation GetOrLoad(string sceneName)
		{
			AsyncSceneOperation op;

			Scene sceneToLoad = SceneManager.GetSceneByName(sceneName);
			if (sceneToLoad.isLoaded)
			{
				// Dirty trick because we are mixing async and sync code.
				// We want to still be able to use the complete event, however it would not be hooked if we called OnLoaded here.
				op = new MockSceneOperation(sceneToLoad);
			}
			else
			{
				op = new SceneLoading(sceneName, LoadSceneMode.Additive);
			}

			return op;
		}

		public static async Cysharp.Threading.Tasks.UniTask<Scene> GetOrLoadAsync(string scene)
		{
			string prefixedName = $"Scenes/{scene}";

			Scene existing = SceneManager.GetSceneByName(scene);
			if (existing.IsValid() && existing.isLoaded)
				return existing;

			return (await Addressables.LoadSceneAsync(prefixedName, LoadSceneMode.Additive)).Scene;
		}

		public static async UniTask UnloadAsync(string name, bool optional = true)
		{
			// We can't unload scenes
			// It turns out UnloadSceneAsync panics if it can't find the scene name...
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);

				if (scene.IsValid() && scene.name == name)
				{
					AsyncOperation op = SceneManager.UnloadSceneAsync(scene);

					if (op != null) // Operation can be null some times??
						await op;

					return;
				}
			}

			if (!optional)
			{
				AjLog.LogError($"Couldn't find scene '{name}' to unload.", nameof(SceneLoader), nameof(UnloadAsync));
			}
		}

		public static async Cysharp.Threading.Tasks.UniTask<TDriver> GetOrLoadAsync<TDriver>(string sceneName)
		{
			Scene scene = await GetOrLoadAsync(sceneName);

			TDriver ret = scene.FindRootComponent<TDriver>();

			return ret;
		}

		public static AsyncSceneOperation Unload(Scene scene)
		{
			return Unload(scene.name);
		}

		public static AsyncSceneOperation Unload(params string[] scenes)
		{
			List<SceneUnloading> operations = scenes
				.Where(name => SceneManager.GetSceneByName(name).isLoaded)
				.Select(name => new SceneUnloading(name))
				.ToList();

			if (operations.Count == 0) return new MockSceneOperation(new Scene());
			else if (operations.Count == 1) return operations.First();
			else return new MultiSceneLoading(operations);
		}

		public static AsyncSceneOperation Load(params string[] scenes)
		{
			IEnumerable<SceneLoading> asyncOperations = scenes.Select(scene => new SceneLoading(scene, LoadSceneMode.Additive));

			if (scenes.Length == 1)
			{
				return asyncOperations.First();
			}
			else
			{
				return new MultiSceneLoading(asyncOperations);
			}
		}

		public static void SetActive(string name)
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.name == name)
					SceneManager.SetActiveScene(scene);
			}
		}

		public static async Cysharp.Threading.Tasks.UniTask<Scene> GetSceneAsync(string sceneName)
		{
			Scene scene = SceneManager.GetSceneByName(sceneName);

			if (scene.IsValid())
				return scene;

			SceneInstance instance = await Addressables.LoadSceneAsync($"Scenes/{sceneName}", LoadSceneMode.Additive).Task;

			return instance.Scene;
		}

#if UNITY_EDITOR
		public static void GetOrLoadEditor(string scenepath)
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.path == scenepath)
					return;
			}

			EditorSceneManager.OpenScene(scenepath, OpenSceneMode.Additive);
		}
#endif
	}
}
using System.Collections.Generic;
using Anjin.Nanokin;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using AStar;
using esm = UnityEditor.SceneManagement.EditorSceneManager;

#endif

#if UNITY_EDITOR
namespace Anjin.Editor
{
	public static class EditorLevelCache
	{
		static         Level editModeLevel;
		private static bool  initialized;

		private static void Initialize()
		{
			if (initialized) return;
			initialized = true;

			editModeLevel = null;


#if UNITY_EDITOR
			roots                            =  new List<GameObject>();
			esm.activeSceneChangedInEditMode += OnActiveSceneChanged;
			FindLevelInScene(esm.GetActiveScene());
#endif
		}

		static List<GameObject> roots;

		public static Level GetLevel()
		{
			Initialize();
			if (Application.isPlaying)
			{
				if (GameController.Live == null) return null;
				return GameController.ActiveLevel;
			}
			else return editModeLevel;
		}

		static void OnActiveSceneChanged(Scene arg0, Scene arg1)
		{
			Initialize();
#if UNITY_EDITOR
			FindLevelInScene(arg1);
#endif
		}

		static void FindLevelInScene(Scene scene)
		{
#if UNITY_EDITOR
			editModeLevel = null;

			if(scene.IsValid())
			{
				//Stopwatch watch = Stopwatch.StartNew();
				scene.GetRootGameObjects(roots);

				for (int i = 0; i < roots.Count; i++)
				{
					editModeLevel = roots[i].GetComponent<Level>();
					if (editModeLevel != null) break;
				}

				if (editModeLevel == null)
				{
					for (int i = 0; i < roots.Count; i++)
					{
						editModeLevel = roots[i].transform.GetComponentInChildren<Level>();
						if (editModeLevel != null) break;
					}
				}
				//watch.Stop();
				//Debug.Log("EditorLevelCache, Find Level: " + CurrentLevel + ", " + watch.ElapsedMilliseconds + " ms");
			}
#endif
		}
	}
}
#endif
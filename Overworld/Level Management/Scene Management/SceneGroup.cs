using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Nanokin.SceneLoading
{

	 //	Scene Groups
	//	This is how the scene loader tracks the scenes it loads.
	//---------------------------------------------------------------------
	public enum SceneGroupType {
		Normal, Level
	}

	public class SceneGroup
	{
		public int? ID;

		public List<Scene> LoadedScenes;
		public int?        MainSceneIndex;

		public Level         Level;
		public LevelManifest ManifestOfOrigin;

		public SceneGroup       Parent;
		public List<SceneGroup> Children;

		List<GameObject> inactive_transforms;

		public SceneGroupType Type;

		public SceneGroup()
		{
			LoadedScenes = new List<Scene>();
			Children     = new List<SceneGroup>();
			inactive_transforms = new List<GameObject>();
			Reset();
		}

		public void Reset()
		{
			ID             = null;
			MainSceneIndex = null;

			Level            = null;
			ManifestOfOrigin = null;

			Parent = null;

			Type = SceneGroupType.Normal;

			LoadedScenes.Clear();
			inactive_transforms.Clear();
			Children.Clear();
		}

		public Scene? MainScene => MainSceneIndex == null ? (Scene?)null : LoadedScenes[MainSceneIndex.Value];

		public T FindComponentInScenes<T>() where T : MonoBehaviour
		{
			for (int i = 0; i < LoadedScenes.Count; i++) {
				var roots = LoadedScenes[i].GetRootGameObjects();

				for (int j = 0; j < roots.Length; j++) {
					var component = roots[j].GetComponent<T>();
					if(component == null)
						component = roots[j].transform.GetComponentInChildren<T>();

					if (component != null)
						return component;
				}
			}

			return null;
		}

		public void SetRootObjectsActive(bool active)
		{
			for (int i = 0; i < LoadedScenes.Count; i++) {
				var objs = LoadedScenes[i].GetRootGameObjects();
				for (int j = 0; j < objs.Length; j++) {

					if(!active && !objs[j].activeSelf)
						inactive_transforms.Add(objs[j]);

					if(active && inactive_transforms.Contains(objs[j])) {
						inactive_transforms.Remove(objs[j]);
						continue;
					}

					objs[j].SetActive(active);
				}
			}
		}

		public Scene GetMainScene() => LoadedScenes[MainSceneIndex != null ? MainSceneIndex.Value : 0];

		public void SetMainSceneActive()
		{
			if (LoadedScenes.Count == 0) return;

			SceneManager.SetActiveScene(GetMainScene());
		}

		public void RequestNewLoad(SceneReference reference)
		{

		}
		//SetSubSceneActive(int index)
		//Unload()
		//UnloadAsync()
		//SetRootObjectsActive(bool active)


	}
}
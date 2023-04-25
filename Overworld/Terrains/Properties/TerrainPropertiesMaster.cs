using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util.Odin.Attributes;

namespace Overworld.Terrains
{
	public class TerrainPropertiesMaster : SerializedScriptableObject
	{
		[ListDrawerSettings(CustomAddFunction = "OnAddEntry"), Inline]
		public List<Entry> MaterialProperties = new List<Entry>();

		public class Entry
		{
			[HideLabel, HorizontalGroup] public Material          material;
			[HideLabel, HorizontalGroup] public TerrainProperties properties;
		}

#if UNITY_EDITOR
		[Title("Tools")]
		[ShowInInspector, Inline] private static List<SceneMaterials> _sceneMaterials = new List<SceneMaterials>();

		[Button]
		private void CollectSceneMaterials()
		{
			_sceneMaterials.Clear();

			List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);

				if (!scene.isLoaded)
					continue;

				SceneMaterials sceneMaterials = new SceneMaterials
				{
					sceneName = scene.name
				};

				foreach (GameObject root in scene.GetRootGameObjects())
				{
					root.GetComponentsInChildren(meshRenderers);

					foreach (MeshRenderer mr in meshRenderers)
					{
						foreach (Material material in mr.sharedMaterials)
						{
							sceneMaterials.materials.Add(material);
						}
					}
				}

				_sceneMaterials.Add(sceneMaterials);
			}
		}

		[Button]
		private void DiscardRegisteredMaterials()
		{
			foreach (Entry entry in MaterialProperties)
			{
				foreach (SceneMaterials sceneMaterials in _sceneMaterials)
				{
					sceneMaterials.materials.Remove(entry.material);
				}

			}
		}


		private void OnAddEntry()
		{
			MaterialProperties.Add(new Entry());
		}

		private class SceneMaterials
		{
			[HideLabel, DisplayAsString] public string            sceneName;
			[Inline]                     public HashSet<Material> materials = new HashSet<Material>();
		}
#endif
	}
}
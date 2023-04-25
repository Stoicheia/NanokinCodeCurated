using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworld.Terrains
{
	public class SceneTerrainProperties : SerializedMonoBehaviour
	{
		public static readonly Dictionary<Scene, SceneTerrainProperties> Scenes = new Dictionary<Scene, SceneTerrainProperties>();

		[SerializeField, OnValueChanged("OnMastersChanged")]
		private TerrainPropertiesMaster[] Masters = new TerrainPropertiesMaster[0];

		private Dictionary<Material, TerrainProperties> _propertiesByMaterial;

		private void Awake()
		{
			_propertiesByMaterial = new Dictionary<Material, TerrainProperties>();
		}

		private void OnEnable()
		{
			_propertiesByMaterial.Clear();

			foreach (TerrainPropertiesMaster master in Masters)
			{
				foreach (TerrainPropertiesMaster.Entry entry in master.MaterialProperties)
				{
					if(entry.material != null)
						_propertiesByMaterial[entry.material] = entry.properties;
				}
			}

			Scenes[gameObject.scene] = this;
		}

		private void OnDisable()
		{
			Scenes.Remove(gameObject.scene);
		}

		public static TerrainProperties GetProperties(Scene scene, Material material)
		{
			if (Scenes.TryGetValue(scene, out SceneTerrainProperties sceneMasters))
			{
				if (sceneMasters._propertiesByMaterial.TryGetValue(material, out TerrainProperties properties))
				{
					return properties;
				}
			}

			return null;
		}

#if UNITY_EDITOR
		private void OnMastersChanged()
		{
			if (Application.isPlaying)
				OnEnable();
		}
#endif
	}
}
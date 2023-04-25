using System;
using System.Collections.Generic;
using Anjin.Nanokin.SceneLoading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Anjin.Nanokin
{

	/// <summary>
	/// Marks the scene it's in as an actual game level, presumably loaded from a level manifest.
	/// For convenience, should be placed on a scene root object.
	/// </summary>
	public class Level : SerializedMonoBehaviour
	{
		public LevelManifest Manifest;

		[Required("A default spawn point must be set, or any overworld deaths may result in undefined behavior!")]
		public SpawnPoint DefaultSpawnpoint;

		public static List<Level> all;

		private void OnEnable()  => all.AddIfNotExists(this);
		private void OnDisable() => all.Remove(this);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void Subsystem()
		{
			all = new List<Level>();
		}

		private async void Awake()
		{
			all.AddIfNotExists(this);
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Level))]
	public class LevelInspector : OdinEditor {
		private SerializedProperty _defaultSpawnpoint;

		protected override void OnEnable()
		{
			base.OnEnable();
			_defaultSpawnpoint = serializedObject.FindProperty("DefaultSpawnpoint");
		}

		public override void OnInspectorGUI()
		{
			Level lvl = target as Level;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Manifest:", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if(lvl.Manifest != null) {
				if (GUILayout.Button("Ping", SirenixGUIStyles.MiniButtonLeft)) {
					EditorGUIUtility.PingObject(lvl.Manifest);
				}
				else if (GUILayout.Button("Select", SirenixGUIStyles.MiniButtonRight)) {
					Selection.activeObject = lvl.Manifest;
				}
			}
			GUILayout.EndHorizontal();

			lvl.Manifest = (LevelManifest)SirenixEditorFields.UnityObjectField(GUIContent.none, lvl.Manifest, typeof(LevelManifest), false);

			if(lvl.Manifest == null) {
				if (GUILayout.Button("Create New Manifest")) {

					var path = UnityEditor.EditorUtility.SaveFilePanel("New Manifest", "Assets/Resources/Level Manifests", "New Manifest.asset", "asset");

					if (path.Length != 0) {
						var manifest = CreateInstance<LevelManifest>();

						manifest.MainScene = new SceneReference(lvl.gameObject.scene.name);


						AssetDatabase.CreateAsset(manifest, "Assets" + path.Substring(Application.dataPath.Length));
						AssetDatabase.SaveAssets();

						lvl.Manifest = manifest;

						EditorSceneManager.MarkSceneDirty(lvl.gameObject.scene);
						EditorSceneManager.SaveScene(lvl.gameObject.scene);

						UnityEditor.EditorUtility.SetDirty(manifest);

						if (LevelManifestDatabase.LoadedDB) {
							LevelManifestDatabase.LoadedDB.Manifests.Add(manifest);
							UnityEditor.EditorUtility.SetDirty(LevelManifestDatabase.LoadedDB);
						}


						Selection.activeObject = manifest;
					}
				}
			}

			EditorGUILayout.PropertyField(_defaultSpawnpoint);

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}
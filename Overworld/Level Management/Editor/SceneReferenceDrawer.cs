using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;

public class SceneReferenceDrawer : OdinValueDrawer<SceneReference>
{
	private string _scenePath;
	private bool   _hasLabel = true;

	protected override void Initialize()
	{
		_scenePath = null;
		ReadScenePath();

		// _hasLabel = !Property.Attributes.Any(attr => attr is HideLabelAttribute);
	}

	private void DrawPicker(GUIContent label)
	{
		SceneAsset oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);

		eg.BeginChangeCheck();
		SceneAsset newScene = (SceneAsset) eglo.ObjectField((_hasLabel ? label : null) ?? new GUIContent(), oldScene, typeof(SceneAsset), false);
		if (eg.EndChangeCheck())
		{
			if (newScene == null)
			{
				ValueEntry.SmartValue = new SceneReference(null);
				ReadScenePath();
				return;
			}

			// If the path wasn't set, then we might still have a scene that was set through the code. We don't want to automatically swap it because it would register as null here.
			string   assetPath = AssetDatabase.GetAssetPath(newScene);
			string[] tokens    = assetPath.Split('/', '.');
			string   sceneName = tokens[tokens.Length - 2];

			ValueEntry.SmartValue = new SceneReference(sceneName);
			ReadScenePath();

			// For some reason the scene doesn't get set as dirty without this, and we lose changes since it doesn't re-serialize....
			ValueEntry.Values.ForceMarkDirty();
		}
	}

	protected override void DrawPropertyLayout(GUIContent label)
	{
		if (label == null)
			label = new GUIContent(Property.NiceName);

		glo.BeginVertical();
		{
			DrawPicker(label);

			if (ValueEntry.SmartValue.SceneName != null && _scenePath != null)
			{
				bool isInBuildSettings = EditorBuildSettings.scenes.Any(bs => bs.path == _scenePath);
				if (!isInBuildSettings)
				{
					glo.BeginHorizontal();

					glo.Label("The scene is not in the build settings");
					if (glo.Button("FIX"))
					{
						ValueEntry.SmartValue.AddToBuildSettings();
					}

					glo.EndHorizontal();
				}
			}
		}


		glo.EndVertical();
	}

	private void ReadScenePath()
	{
		_scenePath = ValueEntry.SmartValue.DetectAssetPath();
	}

}
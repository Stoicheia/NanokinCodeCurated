using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat;
using Combat.Scripting;
using Combat.Startup;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif


public class InternalEditorConfig : SerializedScriptableObject
{
	public                                float                               editorDopesheetZoom;
	public                                AudioClip                           OnRecompile;
	public                                AudioClip                           OnRecompileErrors;
	public                                AudioClip                           OnRecompileSuccess;
	public                                AudioClip                           OnScriptsReloaded;
	public                                AudioClip                           OnDomainReload;
	public                                AudioClip                           OnDomainReloadEnded;
	public                                AudioClip                           OnEnteredPlay;
	public                                AudioClip                           OnEnteredEdit;
	public                                bool                                enableNotificationSounds;
	[OdinSerialize, NonSerialized] public BattleConsoleConfig                 battleConsole             = new BattleConsoleConfig();
	public                                List<SelectionEntry>                SelectionHistory          = new List<SelectionEntry>();
	public                                int                                 SelectionHistoryPos       = 0;
	public                                LuaChangeWatcher.HotReloadBehaviors cutsceneHotReloadBehavior = LuaChangeWatcher.HotReloadBehaviors.ContinueExisting;
	public                                SkillAsset                          LastTestedSkill;

	[Serializable]
	public struct SelectionEntry
	{
		public Object[] Selection;
		public string   Search;
	}

	/// <summary>
	/// Whether or not to load static scene editors.
	/// </summary>
	public bool StaticSceneEditors = true;

	/// <summary>
	/// Whether or not NanokinLauncher should preload all other scenes.
	/// </summary>
	public bool LauncherPreload = false;

	public BattleRecipe LastLauncherRecipe;

	private void OnEnable()
	{
		hideFlags = HideFlags.None;
	}

#if UNITY_EDITOR
	public void MarkDirty()
	{
		EditorUtility.SetDirty(this);
	}

	public void ForceSerializeAsset()
	{
		EditorUtility.SetDirty(this);

		string[] assetPaths =
		{
			AssetDatabase.GetAssetPath(this)
		};

		if (!EditorApplication.isPlaying)
		{
			AssetDatabase.ForceReserializeAssets(assetPaths, ForceReserializeAssetsOptions.ReserializeAssets);
		}

		AssetDatabase.SaveAssets();
	}

	public static InternalEditorConfig Instance => "InternalEditor".FetchLocalAsset<InternalEditorConfig>();
#endif
}
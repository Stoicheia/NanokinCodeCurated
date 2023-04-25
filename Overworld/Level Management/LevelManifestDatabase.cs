using System;
using System.Collections.Generic;
using Data.Overworld;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Anjin/Level Manifest Database")]
public class LevelManifestDatabase : SerializedScriptableObject
{
	public const string DBResourcesPath = "Level Manifests/Manifest Database";

	private static LevelManifestDatabase _LoadedDB;

	[BoxGroup("Internal"),ShowInInspector]
	public static LevelManifestDatabase LoadedDB
	{
		get
		{
			if (_LoadedDB == null) LoadDB();
			return _LoadedDB;
		}
	}

	[BoxGroup("Internal"),Button]
	public static void LoadDB()
	{
		_LoadedDB = Resources.Load<LevelManifestDatabase>(DBResourcesPath);
	}


	public List<LevelManifest> Manifests;

	[NonSerialized, ShowInInspector]
	public Dictionary<LevelID, LevelManifest> IDsToManifests;

	public void CatalogIDs()
	{
		IDsToManifests = new Dictionary<LevelID, LevelManifest>();
		foreach (LevelManifest manifest in Manifests) {
			if (manifest.Level != LevelID.None)
				IDsToManifests[manifest.Level] = manifest;
		}
	}

}

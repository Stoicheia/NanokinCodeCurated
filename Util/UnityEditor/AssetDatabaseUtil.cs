using System.Collections.Generic;
using System.IO;
using System.Linq;
using Anjin.Util;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

// Needs to be in utilities, we use this in our game classes with Odin editor code written through inspectors.
#endif

namespace Anjin.Utilities
{
#if UNITY_EDITOR
	public static class AssetDatabaseUtil
	{
		public static List<T> FindAssetsByType<T>()
			where T : Object
		{
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");

			return guids
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<T>)
				.Where(asset => asset != null)
				.ToList();
		}


		/// <summary>
		/// Example of acceptable inputs:
		/// - scripts/
		/// - Assets/data/config
		/// - ...
		/// </summary>
		public static List<T> LoadAssetsInDirectory<T>(string path)
			where T : Object
		{
			//path = $"{Application.dataPath}/{path}";
			if(!path.StartsWithFast("Assets\\"))
				path = $"Assets\\{path}";

			string[] fileEntries = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

			return fileEntries
				.Select(AssetDatabase.LoadAssetAtPath<T>)
				.WhereNotNull()
				.ToList();
		}
	}
#endif
}
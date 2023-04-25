using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.Scripting;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.Scripting
{
	/// <summary>
	/// A script loader which loads scripts under a specific directory:
	/// Resources/Scripts
	/// </summary>
	public class ResourceScriptLoader : ScriptLoaderBase
	{
		public readonly Dictionary<string, LuaAsset> loadedAssets;
		public readonly Dictionary<LuaAsset, string> assetPaths;
		public readonly string                       assetsPath;
		public readonly string                       luaScriptsRoot;

		[SerializeField]
		private static bool _reimported;

		public ResourceScriptLoader(string assetsPath, string scriptsRoot)
		{
			loadedAssets = new Dictionary<string, LuaAsset>();
			assetPaths   = new Dictionary<LuaAsset, string>();

			luaScriptsRoot = scriptsRoot;

			this.assetsPath = assetsPath;

#if UNITY_EDITOR
			// Reimport();
#endif
			ReloadResources();

			ModulePaths = new[] { "?" };
		}


#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorApplication.delayCall += Reimport;
		}


		/// <summary>
		/// Fixes problems with Resources.LoadAll on LuaAssets.
		/// This happens immediately after opening the editor some times.
		/// </summary>
		private static void Reimport()
		{
			if (_reimported) return;
			_reimported = true;

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			string[] assets = AssetDatabase.FindAssets("t:LuaAsset", new[] { "Assets/Resources/Scripts" });
			foreach (string asset in assets)
			{
				AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
			}

			if (!Application.isPlaying)
			{
				AssetDatabase.ForceReserializeAssets(assets);
				AssetDatabase.SaveAssets();
			}
		}
#endif


		public void ReloadResources()
		{
			LuaAsset[] assets = Resources.LoadAll<LuaAsset>(assetsPath);

			for (int i = 0; i < assets.Length; i++)
			{
				LuaAsset asset = assets[i];

				int index = asset.Path.IndexOf(luaScriptsRoot, StringComparison.Ordinal);
				// string assetPath = Path.GetFileNameWithoutExtension(asset.Path); // This also works

				loadedAssets[asset.name] = asset;
				assetPaths[asset]        = asset.name;
			}
		}

		public override bool ScriptFileExists(string file)
		{
			file = GetFileName(file);
			return loadedAssets.ContainsKey(file);
		}

		public LuaAsset LoadAsset(string file)
		{
			return loadedAssets.SafeGet(file);
		}

		public override object LoadFile(string file, Table globalContext)
		{
			LuaAsset ret = loadedAssets.SafeGet(file);
			if (ret == null)
			{
				Debug.LogError($"ResourceScriptLoader: Could not find script by name '{file}'.");
				return null;
			}

			LuaChangeWatcher.FlushChanges(ret);
			return ret.TranspiledText;
		}

		private static string GetFileName(string filename)
		{
			int b = Mathf.Max(filename.LastIndexOf('\\'), filename.LastIndexOf('/'));

			if (b > 0)
				filename = filename.Substring(b + 1);

			return filename;
		}

		public string GetCode(string file)
		{
			throw new NotImplementedException();
		}
	}
}
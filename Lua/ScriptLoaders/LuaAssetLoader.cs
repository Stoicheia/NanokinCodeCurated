using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEngine;

namespace Anjin.Scripting
{
	public class LuaAssetLoader : ScriptLoaderBase
	{
		public Dictionary<string, LuaAsset> loadedAssets;
		public Dictionary<LuaAsset, string> assetPaths;

		public string AssetsPath;
		public string luaScriptsRoot;

		public LuaAssetLoader(string assetsPath, string scriptsRoot)
		{
			loadedAssets = new Dictionary<string, LuaAsset>();
			assetPaths = new Dictionary<LuaAsset, string>();

			luaScriptsRoot = scriptsRoot;
			LoadResources(assetsPath);

			AssetsPath = assetsPath;

			ModulePaths = new [] {"?"};
		}

		public void ReloadResources()
		{
			LoadResources(AssetsPath);
		}

		public void LoadResources(string path)
		{
			LuaAsset[] assets = Resources.LoadAll<LuaAsset>(path);

			for (int i = 0; i < assets.Length; i++)
			{
				LuaAsset asset = assets[i];

				int index = asset.Path.IndexOf(luaScriptsRoot);
				string cleanPath = (index < 0) ? asset.Path : asset.Path.Remove(index, luaScriptsRoot.Length);

				index     = cleanPath.IndexOf(".lua");
				cleanPath = (index < 0) ? cleanPath : cleanPath.Remove(index, 4);

				loadedAssets[cleanPath] = asset;
				assetPaths[asset] 		= cleanPath;
			}
		}

		private string GetFileName(string filename)
		{
			int b = Mathf.Max(filename.LastIndexOf('\\'), filename.LastIndexOf('/'));

			if (b > 0)
				filename = filename.Substring(b + 1);

			return filename;
		}

		public override bool ScriptFileExists(string file)
		{
			file = GetFileName(file);
			return loadedAssets.ContainsKey(file);
		}

		public override object LoadFile(string file, Table globalContext)
		{
			return loadedAssets[file].TranspiledText;
		}
	}
}
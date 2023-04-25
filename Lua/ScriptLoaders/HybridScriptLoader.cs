using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Anjin.Scripting
{
	public class HybridScriptLoader : AnjinScriptLoader
	{
		private ResourceScriptLoader    _resLoader;
		private AddressableScriptLoader _addrLoader;

		public HybridScriptLoader(bool editor = false)
		{
			_resLoader  = new ResourceScriptLoader("Scripts", "Assets/Resources/Scripts");
			_addrLoader = new AddressableScriptLoader(editor);
			ModulePaths = new[] {"?"};
		}

		public override bool ScriptFileExists(string name)
		{
			return _resLoader.ScriptFileExists(name) || _addrLoader.ScriptFileExists(name);
		}

		public override object LoadFile(string file, Table globalContext)
		{
			if (_resLoader.ScriptFileExists(file)) return _resLoader.LoadFile(file, globalContext);
			if (_addrLoader.ScriptFileExists(file)) return _addrLoader.LoadFile(file, globalContext);
			return null;
		}

		public LuaAsset LoadAsset(string file)
		{
			if (_resLoader.ScriptFileExists(file)) return _resLoader.LoadAsset(file);
			if (_addrLoader.ScriptFileExists(file)) return _addrLoader.LoadAsset(file);
			return null;
		}

		public async UniTask LoadAll()
		{
			_resLoader.ReloadResources();
			await _addrLoader.loadTask;
		}

		public override List<string> GetAllNames()
		{
			List<string> list = new List<string>();

			foreach (var pair in _resLoader.loadedAssets)
			{
				list.Add(pair.Key);
			}

			foreach (KeyValuePair<string, LuaAsset> pair in _addrLoader.loadedAssets)
			{
				list.Add(pair.Key);
			}

			return list;
		}
	}
}
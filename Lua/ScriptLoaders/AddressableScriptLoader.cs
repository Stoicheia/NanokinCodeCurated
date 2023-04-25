using System.Collections.Generic;
using Anjin.Nanokin;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

#endif

namespace Anjin.Scripting
{
	/// <summary>
	/// A lua script loader which fetches scripts by address (addressable system)
	/// The addresses must have the Scripts/ prefix
	/// </summary>
	public class AddressableScriptLoader : ScriptLoaderBase
	{
		public readonly Dictionary<string, LuaAsset> loadedAssets;
		public readonly AsyncLazy                    loadTask;

		private AsyncOperationHandle<IList<LuaAsset>> _handle;

		private bool _editor;

		public AddressableScriptLoader(bool editor)
		{
			_editor = editor;

			ModulePaths  = new[] {"?"};
			loadedAssets = new Dictionary<string, LuaAsset>();

			if (!editor)
			{
				loadTask = UniTask.Lazy(Load);
			}
			else
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

					foreach (AddressableAssetGroup group in settings.groups)
					{
						if (group.name != Addresses.LuaGroup) continue;

						foreach (AddressableAssetEntry entry in group.entries)
						{
							LuaAsset asset = AssetDatabase.LoadAssetAtPath<LuaAsset>(entry.AssetPath);
							if (asset != null)
							{
								loadedAssets[asset.name] = asset;
							}
						}
					}
				}
#endif
			}
		}

		private async UniTask Load()
		{
			//UniTask2.InitPlayerLoopHelper();
			await Addressables.InitializeAsync(); // This is needed if we're not in playmode, e.g. unit tests

			// await UniTask.WaitUntil(() => Addressables.ResourceLocators.Any()); // Don't think this is needed anymore, and it seems to hang up completely some times

			_handle = Addressables.LoadAssetsAsync<LuaAsset>("Lua", null);

			IList<LuaAsset> assets = await _handle;
			foreach (LuaAsset asset in assets)
				loadedAssets[asset.name] = asset;

			this.Log($"Successfully loaded {assets.Count} lua scripts through Lua label.");
		}

		public override bool ScriptFileExists(string name) => loadedAssets.ContainsKey(name);

		public override object LoadFile(string file, Table globalContext)
		{
#pragma warning disable 612
			LuaAsset ret = LoadAsset(file);
			LuaChangeWatcher.FlushChanges(ret);
			return ret.TranspiledText;
#pragma warning restore 612
		}

		public LuaAsset LoadAsset(string file)
		{
			if (!_editor && loadTask.Task.Status != UniTaskStatus.Succeeded)
			{
				// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
				DebugLogger.LogWarning("Trying to load scripts before loading has finished. Make sure Lua.OnReady is being used.", LogContext.Data, LogPriority.High);

				const float TIMEOUT = 2.5f;

				float startTime = Time.time;
				while (loadTask.Task.Status != UniTaskStatus.Succeeded)
				{
					if (Time.time - startTime < TIMEOUT)
					{
						// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
						this.LogError($"LoadAsset timed out. (> {TIMEOUT} seconds)");
						return null;
					}
				}
			}

			return loadedAssets.TryGetValue(file, out LuaAsset asset) ? asset : null;
		}
	}
}
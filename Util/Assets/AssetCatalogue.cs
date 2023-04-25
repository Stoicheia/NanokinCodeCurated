using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

#endif

namespace Util.Assets
{
	public abstract class AssetCatalogue<TAsset, TCatalogue> : AssetCatalogue<TAsset>
		where TCatalogue : AssetCatalogue<TAsset, TCatalogue>, new()
		where TAsset : ScriptableObject, IAddressable
	{
		public static TCatalogue Instance => Singleton<TCatalogue>.Instance;
	}

	public abstract class AssetCatalogue<TAsset>
		where TAsset : ScriptableObject, IAddressable
	{
		public List<TAsset> loadedAssets = new List<TAsset>();

		private Dictionary<string, TAsset> _byAddress = new Dictionary<string, TAsset>();

		// private static IEnumerable<string> AllAddresses
		// {
		// 	get
		// 	{
		// 		return Addressables.ResourceLocators
		// 			.SelectMany(lmap => lmap.Keys.Select(key => key.ToString()))
		// 			.Where(key => key.StartsWith("Limbs/") && !key.EndsWith(".png"));
		// 	}
		// }

		public abstract string AddressPrefix    { get; }
		public abstract string AddressExcludes  { get; }
		public abstract string AddressableLabel { get; }

		public List<string> CatalogueAddresses => Addressables2.Find(AddressPrefix);

		/// <summary>
		/// Get an already loaded asset by its address.
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public TAsset GetByAddress(string addr)
		{
			return _byAddress.SafeGet(addr);
		}

		public async UniTask<TAsset> GetOrLoad(string addr)
		{
			if (addr == null)
				return null;

			if (_byAddress.TryGetValue(addr, out TAsset asset))
				return asset;

			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(addr);

			handle.Completed += hnd =>
			{
				_byAddress[addr]   = hnd.Result;
				hnd.Result.Address = addr;
			};

			return await handle;
		}

		public async UniTask<TAsset> LoadAssetAsync(string address, bool isEditor = false)
		{
			if (_byAddress.TryGetValue(address, out TAsset existing))
				return existing;

			try
			{
#if UNITY_EDITOR
				bool isPlaying = Application.isPlaying;

				if (!isPlaying || isEditor)
				{
					TAsset asset = Addressables2.LoadInEditor<TAsset>(address);
					asset.Address = address;
					RegisterAsset(address, asset);

					return asset;
				}
#endif

				Task<TAsset> task = Addressables.LoadAssetAsync<TAsset>(address).Task;

				TAsset ret = await task;
				RegisterAsset(address, ret);

				return ret;
			}
			catch (Exception e)
			{
				DebugLogger.Log($"Error loading address '{address}' in catalogue. Is the address correct? ", LogContext.Data, LogPriority.High);
				DebugLogger.LogException(e);
				throw;
			}
		}

		public async UniTask LoadAll(Action<IList<TAsset>> onComplete = null, bool isEditor = false)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying || isEditor)
			{
				IEnumerable<AddressableAssetEntry> addresses = Addressables2.FindEntriesInEditor(AddressPrefix, excludes: AddressExcludes, label: AddressableLabel); // BUG shouldn't this be notEndWith: AddressExcludes

				foreach (AddressableAssetEntry entry in addresses)
				{
					if (_byAddress.ContainsKey(entry.address)) // already loaded
						continue;

					TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(entry.AssetPath);
					if (asset != null)
						RegisterAsset(entry.address, asset);
				}

				onComplete?.Invoke(loadedAssets);

				await Task.CompletedTask;
				return;
			}
#endif

			AsyncLoading loading = new AsyncLoading();

			foreach (string catalogueAddress in CatalogueAddresses)
			{
				loading.Add<TAsset>(catalogueAddress).Completed += handle =>
				{
					RegisterAsset(catalogueAddress, handle.Result);
				};
			}

			loading.Completed += () =>
			{
				onComplete?.Invoke(loadedAssets.ToList());
			};

			await loading.NewTask;
		}

		private void RegisterAsset(string address, TAsset asset)
		{
			if (asset == null) return;

			asset.Address = address;

			_byAddress[address] = asset;
			loadedAssets.Add(asset);
		}

		protected abstract void OnAssetLoaded(TAsset asset);

#if UNITY_EDITOR
		// private static IEnumerable<AddressableAssetEntry> GetAddressesEditor(
		// 	string startsWith   = null,
		// 	string endsWith     = null,
		// 	string notStartWith = null,
		// 	string notEndWith   = null,
		// 	string notEndWith   = null
		// )
		// {
		// 	List<AddressableAssetEntry> addresses = new List<AddressableAssetEntry>();
		//
		// 	AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		// 	addresses.AddRange(settings.groups.SelectMany(g => g.entries));
		//
		// 	return addresses.Where(key => (startsWith == null || key.address.StartsWith(startsWith))
		// 	                              && (endsWith == null || key.address.EndsWith(endsWith))
		// 	                              && (notStartWith == null || !key.address.StartsWith(notStartWith))
		// 	                              && (notEndWith == null || !key.address.EndsWith(notEndWith)));
		// }

		public IList<ValueDropdownItem<string>> GetOdinDropdownItems()
		{
			ValueDropdownList<string> ret      = new ValueDropdownList<string>();
			AddressableAssetSettings  settings = AddressableAssetSettingsDefaultObject.Settings;

			List<AddressableAssetEntry> allAssets = new List<AddressableAssetEntry>(settings.groups.SelectMany(g => g.entries));

			ret.AddRange(allAssets
				.Where(ass => ass?.address != null
				              && ass.address.StartsWith(AddressPrefix)
				              && !ass.address.EndsWith(".png")
				              && (AddressExcludes == null || !ass.address.Contains(AddressExcludes))
				              && (AddressableLabel == null || ass.labels.Contains(AddressableLabel)))
				.Select(asset => new ValueDropdownItem<string>(asset.address, asset.address)));
			return ret;
		}
#endif
	}
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Anjin.Util;
using API.Spritesheet.Indexing;
using Assets.Nanokins;
using Combat;
using Combat.Entry;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SaveFiles.Elements.Inventory.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Vexe.Runtime.Extensions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

#endif

namespace Util.Addressable
{
	public static class Addressables2
	{
		private static List<string> _cache = new List<string>();

#if UNITY_EDITOR
		private static List<AddressableAssetEntry> _cacheEntry = new List<AddressableAssetEntry>();
#endif

		private static string GetPrefix<T>() where T : Object
		{
			Type type = typeof(T);

			if (type.IsA(typeof(NanokinLimbAsset))) return "Limbs/";
			else if (type.IsA(typeof(CharacterAsset))) return "Characters/";
			else if (type.IsA(typeof(LuaAsset))) return "Scripts/";
			else if (type.IsA(typeof(NanokinAsset))) return "Nanokins/";
			else if (type.IsA(typeof(SkillAsset))) return "Skills/";
			else if (type.IsA(typeof(StickerAsset))) return "Stickers/";

			return "";
		}

		private static string GetSuffix<T>() where T : Object
		{
			Type type = typeof(T);

			if (type.IsA(typeof(IndexedSpritesheetAsset))) return ".spritesheet";

			return "";
		}

		private static object GetExtension<T>() where T : Object
		{
			Type type = typeof(T);

			if (type.IsA(typeof(LuaAsset))) return ".lua";

			return ".asset";
		}

		private static bool CheckMatch(
			string                      addr,
			[CanBeNull] HashSet<string> labels,
			string                      startsWith,
			string                      endsWith,
			string                      notStartWith,
			string                      notEndWith,
			string                      contains,
			string                      excludes,
			string                      containsLabel
		)
		{
			bool matches = (startsWith == null || addr.StartsWithFast(startsWith))
			               && (endsWith == null || addr.EndsWithFast(endsWith))
			               && (notStartWith == null || !addr.StartsWithFast(notStartWith))
			               && (notEndWith == null || !addr.EndsWithFast(notEndWith))
			               && (contains == null || addr.Contains(contains))
			               && (excludes == null || !addr.Contains(excludes))
			               && (containsLabel == null || labels == null || labels.Contains(containsLabel));
			return matches;
		}

		[NotNull]
		public static List<string> Find(
			[CanBeNull] string startsWith   = null,
			[CanBeNull] string endsWith     = null,
			[CanBeNull] string notStartWith = null,
			[CanBeNull] string notEndWith   = null,
			[CanBeNull] string label        = null
		)
		{
			_cache.Clear();

			foreach (IResourceLocator locator in Addressables.ResourceLocators)
			{
				if (label == null || locator.LocatorId == label) // TODO test me
				{
					foreach (object locatorKey in locator.Keys)
					{
						if (locatorKey is string key && CheckMatch(key, null, startsWith, endsWith, notStartWith, notEndWith, null, null, label))
						{
							_cache.Add(key);
						}
					}
				}
			}

			return _cache;
		}

		[Obsolete("This function does not allow for correct management of handles and will result in memory leaks. Use LoadHandleAsync or AsyncHandles.LoadAssetAsync instead.")]
		public static async UniTask<TAsset> LoadAssetAsync<TAsset>(string address)
			where TAsset : class
		{
			try
			{
				AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(address);
				TAsset                       ass    = await handle;

				if (ass is IAddressable addressable)
					addressable.Address = address;
				return ass;
			}
			catch (Exception e)
			{
				DebugLogger.LogException(e);
			}

			return null;
		}

		public static UniTask<AsyncOperationHandle<TAsset>> LoadHandleAsync<TAsset>(AssetReferenceT<TAsset> aref)
			where TAsset : Object
		{
			if (!aref.RuntimeKeyIsValid())
				return UniTask.FromResult(new AsyncOperationHandle<TAsset>());

			return LoadHandleAsync<TAsset>((string)aref.RuntimeKey);
		}

		public static async UniTask<AsyncOperationHandle<TAsset>> LoadHandleAsync<TAsset>(string address)
		{
			await Addressables.InitializeAsync();

			Stopwatch stopwatch = null;

			if (GameOptions.current.log_addressable_profiling)
				stopwatch = Stopwatch.StartNew();

			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(address);
			TAsset                       asset  = await handle;

			if (GameOptions.current.log_addressable_profiling)
				AjLog.LogTrace("%%", $"LoadHandleAsync({address}) took {stopwatch.ElapsedMilliseconds}ms");

			if (asset is IAddressable addressable)
				addressable.Address = address;

			return handle;
		}

		public static AsyncOperationHandle<TAsset> LoadHandleAsyncSlim<TAsset>(string address)
		{
			// Stopwatch stopwatch = null;
			// if (GameOptions.current.log_addressable_profiling)
			// stopwatch = Stopwatch.StartNew();

			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(address);
			handle.CompletedTypeless += asset =>
			{
				if (asset.Result is IAddressable addressable)
					addressable.Address = address;

				// if (GameOptions.current.log_addressable_profiling)
				// AjLog.LogTrace("%%", $"LoadHandleAsync({address}) took {stopwatch.ElapsedMilliseconds}ms");
			};

			return handle;
		}

		public static async UniTask<GameObject> InstantiateAsync(string address)
		{
			Stopwatch stopwatch = null;

			if (GameOptions.current.log_addressable_profiling)
				stopwatch = Stopwatch.StartNew();

			AsyncOperationHandle<GameObject> handle   = Addressables.InstantiateAsync(address);
			GameObject                       instance = await handle;

			if (GameOptions.current.log_addressable_profiling)
				AjLog.LogTrace("%%", $"InstantiateAsync({address}) took {stopwatch.ElapsedMilliseconds}ms");

			return instance;
		}

		public static void Release<TAsset>(AsyncOperationHandle<TAsset> handle)
		{
			if (!handle.IsValid())
			{
				DebugLogger.LogWarning("[DEBUG] Nothing to release. An asset may have faile to load, may be worth investigating.", LogContext.Data, LogPriority.Low);
				return;
			}

			Addressables.Release(handle);
		}

		/// <summary>
		/// Release an handle only if it's valid, otherwise does nothing.
		/// </summary>
		/// <param name="handle"></param>
		/// <typeparam name="TAsset"></typeparam>
		public static void ReleaseSafe<TAsset>(AsyncOperationHandle<TAsset> handle)
		{
			if (!handle.IsValid()) return;
			Addressables.Release(handle);
		}

		/// <summary>
		/// Release an handle only if it's valid, otherwise does nothing.
		/// </summary>
		/// <param name="handle"></param>
		/// <typeparam name="TAsset"></typeparam>
		public static void ReleaseSafe(AsyncOperationHandle handle)
		{
			if (!handle.IsValid()) return;
			Addressables.Release(handle);
		}


		/// <summary>
		/// Release an instance GameObject only if it's not null.
		/// </summary>
		/// <param name="obj"></param>
		public static void ReleaseInstanceSafe(GameObject obj)
		{
			if (obj == null)
				return;

			Addressables.ReleaseInstance(obj);
		}

		public static AsyncOperationHandle<TAsset> OnResult<TAsset>(this AsyncOperationHandle<TAsset> handle, Action<TAsset> handler)
		{
			handle.Completed += hnd => handler(hnd.Result);
			return handle;
		}

		public static void Deconstruct<T>(this AsyncOperationHandle<T> handle, out AsyncOperationHandle<T> hnd, out T result)
		{
			hnd    = handle;
			result = handle.Result;
		}

		private static readonly HashSet<IResourceLocation> _tmpLocations = new HashSet<IResourceLocation>();

		public static List<IResourceLocation> GetResourceLocations(string key, Type type = null)
		{
			// if (type == null && key is AssetReference assetref)
			// type = assetref.SubOjbectType;

			foreach (IResourceLocator locator in Addressables.ResourceLocators)
			{
				if (locator.Locate(key, type, out IList<IResourceLocation> locs))
				{
					for (var i = 0; i < locs.Count; i++)
					{
						IResourceLocation loc = locs[i];
						_tmpLocations.Add(loc);
					}
				}
			}

			List<IResourceLocation> ret = new List<IResourceLocation>(_tmpLocations);
			_tmpLocations.Clear();
			return ret;
		}

		public static bool GetResourceLocations(string key, Type type, out IList<IResourceLocation> locations)
		{
			// if (type == null && key is AssetReference assetref)
			// type = assetref.SubOjbectType;

			foreach (IResourceLocator locator in Addressables.ResourceLocators)
			{
				if (locator.Locate(key, type, out IList<IResourceLocation> locs))
				{
					for (var i = 0; i < locs.Count; i++)
					{
						IResourceLocation loc = locs[i];
						_tmpLocations.Add(loc);
					}
				}
			}

			locations = _tmpLocations.Count == 0 ? null : new List<IResourceLocation>(_tmpLocations);
			_tmpLocations.Clear();
			return false;
		}


#if UNITY_EDITOR
		[NotNull]
		public static List<AddressableAssetEntry> FindEntriesInEditor(
			[CanBeNull] string startsWith   = null,
			[CanBeNull] string endsWith     = null,
			[CanBeNull] string notStartWith = null,
			[CanBeNull] string notEndWith   = null,
			[CanBeNull] string contains     = null,
			[CanBeNull] string excludes     = null,
			[CanBeNull] string label        = null

		)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			_cacheEntry.Clear();

			foreach (AddressableAssetGroup group in settings.groups)
			{
				foreach (AddressableAssetEntry entry in group.entries)
				{
					string addr = entry.address;
					if (CheckMatch(addr, entry.labels, startsWith, endsWith, notStartWith, notEndWith, contains, excludes, label))
					{
						_cacheEntry.Add(entry);
					}
				}
			}

			return _cacheEntry;
		}

		[NotNull]
		public static List<string> FindInEditor(
			[CanBeNull] string startsWith   = null,
			[CanBeNull] string endsWith     = null,
			[CanBeNull] string notStartWith = null,
			[CanBeNull] string notEndWith   = null,
			[CanBeNull] string contains     = null,
			[CanBeNull] string excludes     = null,
			[CanBeNull] string label        = null
		)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			_cache.Clear();

			foreach (AddressableAssetGroup group in settings.groups)
			{
				foreach (AddressableAssetEntry entry in group.entries)
				{
					string addr = entry.address;
					if (CheckMatch(addr, entry.labels, startsWith, endsWith, notStartWith, notEndWith, contains, excludes, label))
					{
						_cache.Add(addr);
					}
				}
			}

			return _cache;
		}


		[CanBeNull]
		public static string GetPathByAddressEditor(string addr)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null || settings.groups == null)
				return null;

			var allEntries = new List<AddressableAssetEntry>(settings.groups.SelectMany(g => g.entries));

			AddressableAssetEntry foundEntry = null;
			foreach (AddressableAssetEntry e in allEntries)
			{
				if (e.address == addr)
				{
					foundEntry = e;
					break;
				}
			}

			return foundEntry?.AssetPath;
		}

		/// <summary>
		/// Load the asset using the asset database. (or return null if the address does not match any)
		/// </summary>
		/// <param name="address"></param>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		[CanBeNull]
		public static TValue LoadInEditor<TValue>(string address)
			where TValue : Object
		{
			string path = GetPathByAddressEditor(address);
			TValue ret = path != null
				? AssetDatabase.LoadAssetAtPath<TValue>(path)
				: null;

			return ret;
		}

		[CanBeNull]
		public static T LoadInEditorGUID<T>(string guid) where T : Object
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) return null;
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		private static bool SetDefault<T>(AssetReferenceT<T> assref, string address)
			where T : Object
		{
			string assetpath = GetPathByAddressEditor(address);
			string guid      = AssetDatabase.AssetPathToGUID(assetpath);
			Object obj       = AssetDatabase.LoadAssetAtPath<Object>(assetpath);
			if (string.IsNullOrEmpty(guid))
				return false;

			assref.SetEditorAsset(obj);
			return true;
		}

		private static bool SetDefaultByName<T>(AssetReferenceT<T> assref, string name, Object parent)
			where T : Object
		{
			string path = AssetDatabase.GetAssetPath(parent);
			if (string.IsNullOrEmpty(path)) return false;

			string dirpath = Path.GetDirectoryName(path);
			if (dirpath == null) return false;

			string testpath = Path.Combine(dirpath, name) + GetExtension<T>();

			string guid = AssetDatabase.AssetPathToGUID(testpath);
			if (string.IsNullOrEmpty(guid))
				return false;

			Object result = AssetDatabase.LoadAssetAtPath<Object>(testpath);
			if (result != null)
				assref.SetEditorAsset(result);
			return true;
		}


		public static bool SetDefault<T>(AssetReferenceT<T> assref, Object parent)
			where T : Object
		{
			string oaddr = parent.GetAddressInEditor();
			string oname = parent.name;

			string prefix = GetPrefix<T>();
			string suffix = GetSuffix<T>();

			return SetDefault(assref, $"{prefix}{oaddr}{suffix}") ||
			       SetDefault(assref, $"{prefix}{oname}{suffix}") ||
			       SetDefaultByName(assref, $"{oname}{suffix}", parent) ||
			       SetDefaultByName(assref, $"{oname}{suffix}", parent);
		}

		public static bool SetDefaultDirect<T>(ref T obj, string defaultAddress)
			where T : Object
		{
			if (obj == null)
			{
				string assetpath = GetPathByAddressEditor(defaultAddress);
				string guid      = AssetDatabase.AssetPathToGUID(assetpath);
				Object result    = AssetDatabase.LoadAssetAtPath<Object>(assetpath);
				if (!string.IsNullOrEmpty(guid) && result is T resultTyped)
				{
					obj = resultTyped;
					return true;
				}

				return false;
			}

			return false;
		}

		private static bool SetDefaultDirectByName<T>(ref T obj, string name, Object parent) where T : Object
		{
			string path = AssetDatabase.GetAssetPath(parent);
			if (string.IsNullOrEmpty(path)) return false;

			string dirpath = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(path)) return false;

			string testpath =
				Path.Combine(dirpath, name) + GetExtension<T>();

			string guid = AssetDatabase.AssetPathToGUID(testpath);
			if (string.IsNullOrEmpty(guid))
				return false;

			obj = AssetDatabase.LoadAssetAtPath<T>(testpath);
			return true;
		}

		public static bool SetDefaultDirect<T>(ref T obj, Object parent)
			where T : Object
		{
			if (obj != null)
				return false;

			bool found = false;

			string name          = parent.name;
			string nameLowerdash = parent.name.ToLowerdash();
			string suffix        = GetSuffix<T>();
			string prefix        = GetPrefix<T>();

			if (SetDefaultDirectByName(ref obj, name, parent)) return true;
			if (SetDefaultDirectByName(ref obj, nameLowerdash, parent)) return true;
			if (suffix != String.Empty)
			{
				if (SetDefaultDirectByName(ref obj, $"{name}{suffix}", parent)) return true;
				if (SetDefaultDirectByName(ref obj, $"{nameLowerdash}{suffix}", parent)) return true;
			}

			string addr = parent.GetAddressInEditor(true);
			if (addr != null)
			{
				string addrLowerdash = addr.ToLowerdash();

				if (SetDefaultDirect(ref obj, $"{prefix}{addr}{suffix}")) return true;
				if (SetDefaultDirect(ref obj, $"{prefix}{addrLowerdash}{suffix}")) return true;
			}

			return false;
		}

		// public static bool SetDefault<TRef>(ref TRef assref, string default1, string default2)
		// 	where TRef : AssetReference
		// {
		// 	return SetDefault(ref assref, default1) || SetDefault(ref assref, default2);
		// }
#endif
	}
}
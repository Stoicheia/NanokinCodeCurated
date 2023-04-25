using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

#endif

namespace Util.Addressable
{
	public static class AddressableEditorExtensions
	{
#if UNITY_EDITOR
		/// <summary>
		/// Set Addressables Key/ID of an gameObject.
		/// </summary>
		/// <param name="gameObject">GameObject to set Key/ID</param>
		/// <param name="id">Key/ID</param>
		public static void SetAddressableID(this GameObject gameObject, string id)
		{
			SetAddressableID(gameObject as Object, id);
		}

		public static AssetReferenceT<TType> ToReferenceTyped<TType>(this TType o)
			where TType : Object
		{
			if (o == null) return null;
			return new AssetReferenceT<TType>(o.GetAddressInEditor());
		}

		/// <summary>
		/// Set Addressables Key/ID of an object.
		/// </summary>
		/// <param name="o">Object to set Key/ID</param>
		/// <param name="id">Key/ID</param>
		public static void SetAddressableID(this Object o, string id)
		{
			if (id.Length == 0)
			{
				DebugLogger.LogWarning("Can not set an empty adressables ID.", LogContext.Data, LogPriority.Low);
			}

			AddressableAssetEntry entry = GetAddressableAssetEntry(o);
			if (entry != null)
			{
				entry.address = id;
			}
			else
			{
				if (RegisterAddressable(o))
					SetAddressableID(o, id);
			}
		}

		/// <summary>
		/// Get Addressables Key/ID of an gameObject.
		/// </summary>
		/// <param name="gameObject">gameObject to recive addressables Key/ID</param>
		/// <returns>Addressables Key/ID</returns>
		public static string GetAddressInEditor(this GameObject gameObject)
		{
			return GetAddressInEditor(gameObject as Object);
		}

		/// <summary>
		/// Get Addressables Key/ID of an object.
		/// </summary>
		/// <param name="o">object to recive addressables Key/ID</param>
		/// <returns>Addressables Key/ID</returns>
		[CanBeNull]
		public static string GetAddressInEditor(this Object o, bool remove_prefix = false)
		{
			AddressableAssetEntry entry = GetAddressableAssetEntry(o);
			if (entry == null)
				return null;

			if (!remove_prefix)
			{
				int i = entry.address.IndexOf('/');
				if (i > -1 && i < entry.address.Length)
				{
					return entry.address.Substring(i + 1);
				}
			}

			return entry.address;
		}

		/// <summary>
		/// Get addressable asset entry of an object.
		/// </summary>
		/// <param name="o">>object to recive addressable asset entry</param>
		/// <returns>addressable asset entry</returns>
		public static bool RegisterAddressable(Object o)
		{
			if (GetAddressableAssetEntry(o) != null)
				return false;

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
				throw new ArgumentException("Addressable Settings null for some reason.");

			bool isAsset = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,
				out string guid,
				out long _);
			string path = AssetDatabase.GUIDToAssetPath(guid);

			if (string.IsNullOrEmpty(path))
				return false;

			if (isAsset && path.ToLower().Contains("assets"))
			{
				settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
				settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, o, true);
			}

			return true;
		}

		/// <summary>
		/// Get addressable asset entry of an object.
		/// </summary>
		/// <param name="o">>object to recive addressable asset entry</param>
		/// <returns>addressable asset entry</returns>
		public static AddressableAssetEntry GetAddressableAssetEntry(Object o)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			bool   foundAsset = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long _);
			string path       = AssetDatabase.GUIDToAssetPath(guid);

			AddressableAssetEntry entry = null;

			if (foundAsset && path.ToLower().Contains("assets"))
			{
				if (settings != null)
				{
					entry = settings.FindAssetEntry(guid);
				}
			}

			return entry;
		}

		public static TValue TryLoadInEditMode<TValue>(this AssetReference assref)
			where TValue : Object
		{
			if (assref != null /*&& assref.AssetGUID != null*/)
			{
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				AddressableAssetEntry    entry    = settings.FindAssetEntry(assref.AssetGUID);

				if (entry == null) return null;

				// TODO add support for sprites (match sub asset name in assref)

				if (assref.SubObjectName != null && typeof(TValue) == typeof(Sprite)) //
				{
					return AssetDatabase.LoadAllAssetsAtPath(entry.AssetPath).OfType<TValue>().FirstOrDefault(s => s.name == assref.SubObjectName);
				}

				return AssetDatabase.LoadAssetAtPath<TValue>(entry.AssetPath); // return the value from the AssetDatabase
			}

			return null;
		}

		public static List<string> FindAddresses(string startsWith   = null,
			string                                      endsWith     = null,
			string                                      notStartWith = null,
			string                                      notEndWith   = null,
			string                                      label        = null
		)
		{
			List<string> ret = new List<string>();

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			foreach (AddressableAssetGroup group in settings.groups)
			foreach (AddressableAssetEntry entry in @group.entries)
			{
				string key = entry.address;

				if ((startsWith == null || key.StartsWith(startsWith))
				    && (endsWith == null || key.EndsWith(endsWith))
				    && (notStartWith == null || !key.StartsWith(notStartWith))
				    && (notEndWith == null || !key.EndsWith(notEndWith)))
					ret.Add(key);
			}

			return ret;
		}
#endif
	}
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MoonSharp.Interpreter.Tree;
using Vexe.Runtime.Extensions;


namespace Anjin.Util
{

	public static partial class Extensions
	{
		public static void Deconstruct<TKey, TVal>(this KeyValuePair<TKey, TVal> kvp, out TKey key, out TVal value)
		{
			key   = kvp.Key;
			value = kvp.Value;
		}

		/// <summary>
		/// Safely get an entry form a Dictionary
		/// </summary>
		/// <param name="self"></param>
		/// <param name="key"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		[CanBeNull] public static TValue SafeGet<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> self, TKey key)
		{
			if (key == null)
				return default;

			if (self.TryGetValue(key, out TValue value))
				return value;

			return default;
		}

		/// <summary>
		/// Safely get an entry form a Dictionary
		/// </summary>
		/// <param name="self"></param>
		/// <param name="key"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TData"></typeparam>
		/// <returns></returns>
		 public static TData SafeGetOrCreate<TKey, TData>([NotNull] this Dictionary<TKey, TData> self, [CanBeNull] TKey key)
			where TData : new()
		{
			if (key == null)
				throw new ArgumentException("Key cannot be null!");

			if (!self.TryGetValue(key, out TData value))
				self[key] = value = new TData();

			return value;
		}

		/// <summary>
		/// Safely get an entry form a Dictionary
		/// </summary>
		/// <param name="self"></param>
		/// <param name="key"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		[CanBeNull] public static TValue AddIfMissing<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> self, [CanBeNull] TKey key, TValue defaultValue)
			where TValue : new()
		{
			if (key == null)
				return default;

			if (!self.TryGetValue(key, out TValue value))
				self[key] = value = defaultValue;

			return value;
		}


		/// <summary>
		/// Safely get an entry form a Dictionary
		/// </summary>
		/// <param name="self"></param>
		/// <param name="key"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TData"></typeparam>
		/// <returns></returns>
		[NotNull] public static TData GetOrCreate<TKey, TData>([NotNull] this Dictionary<TKey, TData> self, [NotNull] TKey key)
			where TData : new()
		{
			if (!self.TryGetValue(key, out TData value))
				self[key] = value = new TData();

			return value;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		public static TValue GetOrDefault<TKey, TValue>(
			this IDictionary<TKey, TValue> dictionary,
			TKey                           key,
			TValue                         defaultValue = default
		)
		{
			return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="funcDefaultProvider">A lambda that will define what the default is.</param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		public static TValue GetOrDefault<TKey, TValue>(
			this IDictionary<TKey, TValue> dictionary,
			TKey                           key,
			Func<TValue>                   funcDefaultProvider
		)
		{
			return dictionary.TryGetValue(key, out TValue value) ? value : funcDefaultProvider();
		}

		/// <summary>
		/// Merge a dictionary into another.
		/// </summary>
		/// <param name="theDictionary">The dictionary that will be copied.</param>
		/// <param name="other"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		public static void MergeInto<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> theDictionary, Dictionary<TKey, TValue> other)
		{
			foreach (KeyValuePair<TKey, TValue> kv in theDictionary)
				other[kv.Key] = kv.Value;
		}

		public static void MaskRemove<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> dictionary, TKey[] mask)
		{
			foreach (TKey key in mask)
			{
				if (dictionary.ContainsKey(key))
				{
					dictionary.Remove(key);
				}
			}
		}

		// Enum constraints unsupported by Unity, go figure...
		public static Dictionary<TKey, TVal> FillEnumKeys<TKey, TVal>(this Dictionary<TKey, TVal> dictionary)
			where TKey : struct // GOD BLESS C#7
		{
			TKey[] keys = (TKey[]) Enum.GetValues(typeof(TKey));

			foreach (TKey key in keys)
			{
				dictionary[key] = default;
			}

			return dictionary;
		}

		// Enum constraints unsupported by Unity, go figure...
		public static Dictionary<TKey, TVal> FillEnumKeysInstantiated<TKey, TVal>(this Dictionary<TKey, TVal> dictionary)
			where TKey : struct
			where TVal : new()
		// GOD BLESS C#7
		{
			TKey[] keys = (TKey[]) Enum.GetValues(typeof(TKey));

			foreach (TKey key in keys)
			{
				dictionary[key] = new TVal();
			}

			return dictionary;
		}


		public static void AddToDictionaryContainingList<TKey, TVal, TList>(this Dictionary<TKey, TList> dictionary, TKey key, TVal val)
			where TList: IList<TVal>, new()
		{
			if (!dictionary.ContainsKey(key) || dictionary[key] == null)
				dictionary[key] = new TList();

			dictionary[key].Add(val);
		}
	}
}
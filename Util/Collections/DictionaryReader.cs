using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Util
{
	public class DynamicDictionaryReader<TKey, TValue> : DictionaryReader<TKey, TValue>
		where TValue : class
	{
		private Func<Dictionary<TKey, TValue>> _getDictionary;

		public DynamicDictionaryReader(
			Func<Dictionary<TKey, TValue>> getDictionary,
			Func<TValue>                   getDefaultValue,
			Func<TKey, TValue>             getNewValue
		) : base(getDefaultValue, getNewValue)
		{
			_getDictionary = getDictionary;
		}

		protected override Dictionary<TKey, TValue> Dictionary => _getDictionary();
	}


	public class StaticDictionaryReader<TKey, TValue> : DictionaryReader<TKey, TValue>
		where TValue : class
	{
		public Dictionary<TKey, TValue> source;

		public StaticDictionaryReader(Dictionary<TKey, TValue> dictionary,
									  Func<TValue>             getDefaultValue,
									  Func<TKey, TValue>       getNewValue,
									  Action                   onBeforeRetrieval = null
		) : base(getDefaultValue, getNewValue, onBeforeRetrieval)
		{
			source = dictionary;
		}

		protected override Dictionary<TKey, TValue> Dictionary => source;

		/// <summary>
		/// Clear the content of the dictionary being read.
		/// </summary>
		public void Clear()
		{
			Dictionary.Clear();
		}

		public void Remove(TKey key)
		{
			Dictionary.Remove(key);
		}
	}


	public abstract class DictionaryReader<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
		where TValue : class
	{
		private readonly    Func<TValue>       _getDefaultValue;
		private             Func<TKey, TValue> _getNewValue;
		[CanBeNull] private Action             _onBeforeRetrieval;

		protected DictionaryReader(
			Func<TValue>       getDefaultValue,
			Func<TKey, TValue> getNewValue,
			Action             onBeforeRetrieval = null
		)
		{
			_getDefaultValue   = getDefaultValue;
			_getNewValue       = getNewValue;
			_onBeforeRetrieval = onBeforeRetrieval;
		}

		/// <summary>
		/// Gets the dictionary used for reading.
		/// </summary>
		protected abstract Dictionary<TKey, TValue> Dictionary { get; }

		/// <summary>
		/// Gets the keys of the dictionary being read.
		/// </summary>
		public Dictionary<TKey, TValue>.KeyCollection Keys => Dictionary.Keys;

		/// <summary>
		/// Gets the values of the dictionary being read.
		/// </summary>
		public Dictionary<TKey, TValue>.ValueCollection Values => Dictionary.Values;

		/// <summary>
		/// Gets the value for the pointed key, or an exception if it's missing.
		/// </summary>
		[NotNull] public TValue Value => Dictionary[CurrentKey];

		/// <summary>
		/// Gets the value for the pointed key, or null if it does not exist.
		/// </summary>
		[CanBeNull]
		public TValue ValueOrNull
		{
			get
			{
				_onBeforeRetrieval?.Invoke();

				if (CurrentKey == null)
					return default;

				if (Dictionary.TryGetValue(CurrentKey, out TValue val))
					return val;

				return default;
			}
		}

		/// <summary>
		/// Gets the value for the pointed key, or the default for the type if it does not exist.
		/// </summary>
		[NotNull]
		public TValue ValueOrDefault
		{
			get
			{
				_onBeforeRetrieval?.Invoke();

				if (CurrentKey != null && Dictionary.TryGetValue(CurrentKey, out TValue val))
					return val;

				return _getDefaultValue();
			}
		}

		/// <summary>
		/// Gets the value for the pointed key, or create the value for the key if it does not exist using the default creation specified by the user.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		[NotNull]
		public TValue ValueOrCreate
		{
			get
			{
				_onBeforeRetrieval?.Invoke();

				if (CurrentKey == null)
					throw new ArgumentException();

				if (!Dictionary.TryGetValue(CurrentKey, out TValue value))
				{
					Dictionary[CurrentKey] = value = _getNewValue(CurrentKey);
				}

				return value;
			}
		}

		public int Count => Dictionary.Count;

		private TKey CurrentKey { get; set; }

		/// <summary>
		/// Point the reader at the specified key.
		/// </summary>
		/// <param name="key">Key to point at.</param>
		[NotNull] public DictionaryReader<TKey, TValue> this[TKey key]
		{
			get
			{
				CurrentKey = key;
				return this;
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			_onBeforeRetrieval?.Invoke();
			return Dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool RemoveKey(TKey key)
		{
			return Dictionary.Remove(key);
		}

		public void Set(TValue value)
		{
			Dictionary[CurrentKey] = value;
		}

		public bool ContainsKey(TKey elementID)
		{
			return Dictionary.ContainsKey(elementID);
		}
	}
}
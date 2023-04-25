using System.Collections.Generic;
using JetBrains.Annotations;

namespace Util.Collections
{
	/// <summary>
	/// Bi-directional dictionary. (both T1 and T2 can be used as keys to access the other.)
	/// https://stackoverflow.com/a/10966684
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	public class Map<T1, T2>
	{
		private Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
		private Dictionary<T2, T1> _reverse = new Dictionary<T2, T1>();

		public Map()
		{
			Forward = new Indexer<T1, T2>(_forward);
			Reverse = new Indexer<T2, T1>(_reverse);
		}

		public Indexer<T1, T2> Forward { get; }
		public Indexer<T2, T1> Reverse { get; }

		public void Add([NotNull] T1 t1, [NotNull] T2 t2)
		{
			_forward.Add(t1, t2);
			_reverse.Add(t2, t1);
		}

		public void Set([NotNull] T1 t1, [NotNull] T2 t2)
		{
			_forward[t1] = t2;
			_reverse[t2] = t1;
		}

		public void Remove([NotNull] T1 t1)
		{
			T2 t2 = _forward[t1];

			_forward.Remove(t1);
			_reverse.Remove(t2);
		}

		public void Remove([NotNull] T2 t2)
		{
			T1 t1 = _reverse[t2];

			_forward.Remove(t1);
			_reverse.Remove(t2);
		}

		public void Clear()
		{
			_forward.Clear();
			_reverse.Clear();
		}

		public bool Has([NotNull] T1 t1)
		{
			return _forward.ContainsKey(t1);
		}

		public bool Has([NotNull] T2 t2)
		{
			return _reverse.ContainsKey(t2);
		}

		public T2 this[T1 t1] => Forward[t1];
		public T1 this[T2 t2] => Reverse[t2];

		// ReSharper disable once AnnotateCanBeNullParameter
		public bool TryGetValue([NotNull] T1 key, out T2 ret)
		{
			return _forward.TryGetValue(key, out ret);
		}

		// ReSharper disable once AnnotateCanBeNullParameter
		public bool TryGetValue([NotNull] T2 key, out T1 ret)
		{
			return _reverse.TryGetValue(key, out ret);
		}


		public class Indexer<T3, T4>
		{
			private Dictionary<T3, T4> _dictionary;

			public Indexer(Dictionary<T3, T4> dictionary)
			{
				_dictionary = dictionary;
			}

			public T4 this[[NotNull] T3 index]
			{
				get => _dictionary[index];
				set => _dictionary[index] = value;
			}

			[NotNull]
			public Dictionary<T3, T4>.ValueCollection Keys => _dictionary.Values;

			[NotNull]
			public Dictionary<T3, T4>.ValueCollection Values => _dictionary.Values;
		}
	}
}
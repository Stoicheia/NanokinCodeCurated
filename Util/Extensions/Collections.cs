using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using Overworld.Controllers;
using UnityEngine;
using Util;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		[CanBeNull] public static T Choose<T>(this T[] self)
		{
			return RNG.Choose(self);
		}

		[CanBeNull] public static T Choose<T>(this List<T> self)
		{
			return RNG.Choose(self);
		}

		[CanBeNull] public static T Choose<T>(this IEnumerable<T> self)
		{
			return RNG.Choose(self.ToArray());
		}

		[CanBeNull] public static T ChooseExcept<T>(this T[] self, T not)
		{
			return RNG.ChooseExcept(not, self);
		}

		public static void AddAll<T>(this List<T> self, T[] array)
		{
			self.AddRange(array);
		}

		public static void AddAll<T>(this List<T> self, List<T> list)
		{
			self.AddRange(list);
		}

		/// <summary>
		/// Inserts into an array, moves everything behind it further down the line, removing the last item in the process.
		/// </summary>
		/// <typeparam name="T">The array type</typeparam>
		/// <param name="self">The array</param>
		/// <param name="index">The index to insert something, the item at that index will be pushed back</param>
		/// <param name="t">The item to insert</param>
		public static void Insert<T>(this T[] self, int index, T t)
		{
			if (index == self.Length - 1)
			{
				self[self.Length - 1] = t;
				return;
			}

			T curr = t;
			for (int i = index; i < self.Length; i++)
			{
				T temp = self[i];
				self[i] = curr;
				curr    = temp;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="items"></param>
		/// <param name="v"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetMod<T>(this T[] items, float v)
		{
			return items[(int)v % items.Length];
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="items"></param>
		/// <param name="v"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetClamped<T>(this T[] items, float v)
		{
			return items[(int)Mathf.Clamp(v, 0, items.Length - 1)];
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="dictionary"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			return "{" + string.Join(",", dictionary.Select(kv => $"{kv.Key}={kv.Value}").ToArray()) + "}";
		}

		/// <summary>
		/// Get a sublist from a list, where TSub is extending T
		/// </summary>
		/// <param name="self"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TSub"></typeparam>
		/// <returns></returns>
		public static List<TSub> Sub<T, TSub>(this List<T> self)
			where TSub : T
		{
			List<TSub> subList = new List<TSub>();

			self.Apply(obj => subList.Add((TSub)obj), obj => obj is TSub);

			return subList;
		}

		public static IEnumerable<T> Apply<T>(this IEnumerable<T> self, Action<T> applyAction, Func<T, bool> condition = null)
		{
			List<T> enumerable = self as List<T> ?? self.ToList();
			for (int i = 0; i < enumerable.Count; i++)
			{
				T t = enumerable[i];
				if (condition != null && !condition(t)) continue;

				applyAction(t);
			}

			return self;
		}

		/// <summary>
		/// Swap to elements in the list
		/// </summary>
		/// <param name="self"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<T> Swap<T>(this List<T> self, int a, int b)
		{
			T tmp = self[a];
			self[a] = self[b];
			self[b] = tmp;

			return self;
		}

		/// <summary>
		/// Swap 2 elements in the array
		/// </summary>
		/// <param name="self"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T[] Swap<T>(this T[] self, int a, int b)
		{
			T t = self[a];
			self[a] = self[b];
			self[b] = t;

			return self;
		}

		/// <summary>
		/// Shuffles a list
		/// </summary>
		/// <param name="self">The list</param>
		/// <param name="shuffles">How many swaps</param>
		/// <returns>The list</returns>
		public static List<T> Shuffle<T>(this List<T> self, int shuffles = 50)
		{
			// this is a bad shuffle... but it kinda works
			for (int i = 0; i < shuffles; i++)
			{
				self.Swap(RNG.Next(self.Count), RNG.Next(self.Count));
			}

			return self;
		}

		/// <summary>
		/// Safely gets an index form a list. Will be default(T) if out of range
		/// </summary>
		/// <param name="list"></param>
		/// <param name="i"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[CanBeNull] public static T SafeGet<T>([CanBeNull] this IList<T> list, int i)
		{
			if (list == null) return default;
			if (i < 0 || i >= list.Count)
				return default;

			return list[i];
		}

		/// <summary>
		/// Tries to get the element in the list at the specified index, returning false if the index is out of bounds.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <param name="val"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static bool TryGet<T>(this IList<T> list, int index, out T val)
		{
			val = default;
			if (index < 0 || index >= list.Count)
				return false;
			val = list[index];
			return true;
		}

		/// <summary>
		/// Gets an item from a list, and wraps the index if out of bound.
		/// </summary>
		/// <param name="self"></param>
		/// <param name="index"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T WrapGet<T>(this IList<T> self, int index)
		{
			if (self.Count == 0)
				throw new ArgumentException("self is empty");

			int a   = index;
			int b   = self.Count;
			int idx = a - b * Mathf.FloorToInt(a / (float)b); // true modulo
			return self[idx];
		}

		/// <summary>
		/// Gets an item from a list, and clamps the index to the bounds.
		/// </summary>
		/// <param name="self"></param>
		/// <param name="index"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T ClampGet<T>(this IList<T> self, int index)
		{
			return self.SafeGet(index.Clamp(0, self.Count - 1));
		}

		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
		{
			return source.SkipLastN(1);
		}

		public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n)
		{
			IEnumerator<T> it                = source.GetEnumerator();
			bool           hasRemainingItems = false;
			Queue<T>       cache             = new Queue<T>(n + 1);

			do
			{
				if (hasRemainingItems = it.MoveNext())
				{
					cache.Enqueue(it.Current);
					if (cache.Count > n)
						yield return cache.Dequeue();
				}
			} while (hasRemainingItems);
		}

		public static void DestroyAll<TComponent>(this List<TComponent> components) where TComponent : MonoBehaviour
		{
			foreach (TComponent comp in components)
			{
				PrefabPool.DestroyOrReturn(comp.gameObject);
			}

			components.Clear();
		}

		public static void DestroyAll(this List<GameObject> gameObjects)
		{
			foreach (GameObject go in gameObjects)
			{
				go.Destroy();
			}

			gameObjects.Clear();
		}

		public static List<TType> RemoveNulls<TType>([NotNull] this List<TType> list)
			where TType : class
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (list[i] == null)
					list.RemoveAt(i--);
			}

			return list;
		}

		public static void DestroyAll(this IEnumerable<GameObject> gameObjects)
		{
			foreach (GameObject go in gameObjects)
			{
				go.Destroy();
			}
		}

		public static void DestroyAll(this IEnumerable<Transform> transforms)
		{
			foreach (Transform transform in transforms)
			{
				transform.Destroy();
			}
		}

		public static void CompleteAndClear(this List<Tween> tweens)
		{
			tweens.CompleteAll();
			tweens.Clear();
		}

		public static void CompleteAndClear(this List<Timer> timers)
		{
			timers.CompleteAll();
			timers.Clear();
		}

		public static void CompleteAll(this List<Tween> tweens)
		{
			foreach (Tween tween in tweens)
			{
				tween.Complete();
			}
		}

		public static void CompleteAll(this List<Timer> timers)
		{
			foreach (Timer timer in timers)
			{
				timer.Complete();
			}
		}

		public static Vector3 Centroid(this IEnumerable<Vector3> positions)
		{
			return MathUtil.Centroid(positions);
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
		{
			return enumerable.Where(item => item != null);
		}


		/// <summary>
		/// Returns the set of items, made distinct by the selected value.
		/// </summary>
		/// <typeparam name="TSource">The type of the source.</typeparam>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="selector">A function that selects a value to determine unique results.</param>
		/// <returns>IEnumerable&lt;TSource&gt;.</returns>
		public static IEnumerable<TSource> Distinct<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
		{
			HashSet<TResult> set = new HashSet<TResult>();

			foreach (TSource item in source)
			{
				TResult selectedValue = selector(item);

				if (set.Add(selectedValue))
					yield return item;
			}
		}

		public static void Toggle<T>(this HashSet<T> set, T value)
		{
			if (set.Contains(value))
			{
				set.Remove(value);
			}
			else
			{
				set.Add(value);
			}
		}

		/// <summary>
		/// Returns and casts only the items of type <typeparamref name="T" />.
		/// </summary>
		/// <param name="source">The collection.</param>
		public static IEnumerable<T> FilterCast<T>(this IEnumerable source)
		{
			foreach (object obj in source)
			{
				if (obj is T)
					yield return (T)obj;
			}
		}

		public static void Set<T>(this HashSet<T> set, T value, bool state)
		{
			set.Remove(value);
			if (state) set.Add(value);
		}

		public static T RandomElement<T>(this (T[], int) val, System.Random rand)
		{
			if (val.Item2 <= 0 || val.Item1.Length <= 0) return default;
			return val.Item1[rand.Next(0, Mathf.Min(val.Item2, val.Item1.Length))];
		}

		public static bool ContainsIndex<T>(this IList<T> list, int index)
		{
			return index >= 0 && index < list.Count;
		}

		public static T Dequeue<T>(this List<T> list)
		{
			if (list.Count == 0)
			{
				throw new IndexOutOfRangeException("Tried to dequeue a value from an empty list");
			}

			T value = list[0];
			list.RemoveAt(0);

			return value;
		}

		public static bool Dequeue<T>(this List<T> list, out T value)
		{
			value = default;

			if (list.Count == 0) return false;

			value = list[0];
			list.RemoveAt(0);

			return true;
		}

		public static T Pop<T>(this List<T> list)
		{
			if (list.Count == 0)
				throw new InvalidOperationException("Cannot pop from empty list.");

			T ret = list[0];
			list.RemoveAt(0);
			return ret;
		}
	}
}
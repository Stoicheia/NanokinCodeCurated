using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Anjin.Scripting;
using JetBrains.Annotations;
using Pathfinding.Util;
using UnityEngine;
using UnityUtilities;
using Random = UnityEngine.Random;

[LuaUserdata(staticAuto: true)]
public class RNG
{
	/// <summary>
	/// The seed of the RNG
	/// </summary>
	public static int Seed { get; set; }

	/// <summary>
	/// Returns a random float in range 0 to 1
	/// </summary>
	public static float Float => Random.value;

	/// <summary>
	/// Returns a random float in range -1 to 1
	/// </summary>
	public static float FloatSigned => Random.value * 2 - 1;

	/// <summary>
	/// Returns random unit vector where length = 1
	/// </summary>
	public static Vector3 OnSphere => Random.onUnitSphere;

	/// <summary>
	/// Returns Random Vector3 in range (0,0,0) to (1,1,1)
	/// </summary>
	public static Vector3 InSphere => Random.insideUnitSphere;

	/// <summary>
	/// Returns Random Vector3 in range (0,0,0) to (1,0,1) where length = 1
	/// </summary>
	public static Vector3 OnCircle => Random.onUnitSphere.Change3(y: 0);

	/// <summary>
	/// Returns Random Vector3 in range (0,0,0) to (1,0,1)
	/// </summary>
	public static Vector3 InCircle => Random.onUnitSphere.Change3(y: 0);

	/// <summary>
	/// Returns random opaque color
	/// </summary>
	public static Color Color => new Color(Float, Float, Float);

	public static int Sign => Chance(0.5f) ? -1 : 1;

	/// <summary>
	/// Pick an element from a list at random
	/// </summary>
	/// <param name="ts">The list to pick from</param>
	/// <returns>Element picked from ts at random</returns>
	public static T Pick<T>(IList<T> ts)
	{
		return ts[Int(ts.Count)];
	}

	// UNCOMMENT ME WHEN WE GET C# 7.3!
	// /// <summary>
	// /// Pick a member from an enum.
	// /// </summary>
	// /// <returns>Enum value picked from the enum at random.</returns>
	// public static T Pick<T>()
	// 	where T:Enum
	// {
	// 	T[] values = (T[]) Enum.GetValues(typeof(T));
	// 	return Pick(values);
	// }

	public static int Int()
	{
		return Range(int.MinValue, int.MaxValue);
	}

	/// <summary>
	/// Returns a random int in range [0, max[
	/// </summary>
	/// <param name="max">Max</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Int(int max)
	{
		return Random.Range(0, max);
	}

	/// <summary>
	/// Returns a random int in range [0, max[
	/// </summary>
	/// <param name="max">Max</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Next(int max)
	{
		return Random.Range(0, max);
	}

	/// <summary>
	/// Returns a random float in range [0, max]
	/// </summary>
	/// <param name="max">Max</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Range(float max)
	{
		return Range(0, max);
	}

	/// <summary>
	/// Returns a random int in range [min, max]
	/// </summary>
	/// <param name="min">Min</param>
	/// <param name="max">Max</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Range(int min, int max)
	{
		return Random.Range(min, max + 1);
	}

	/// <summary>
	/// Returns a random float in range [min, max]
	/// </summary>
	/// <param name="min">Min</param>
	/// <param name="max">Max</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Range(float min, float max)
	{
		return Random.Range(min, max);
	}

	/// <summary>
	/// Choose a random element from the given arguments
	/// </summary>
	/// <typeparam name="TType">The type of object</typeparam>
	/// <param name="choosing">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static TType Choose<TType>(List<TType> all)
	{
		switch (all.Count)
		{
			case 0:  return default;
			case 1:  return all[0];
			default: return all[Random.Range(0, all.Count)];
		}
	}

	private static List<int> _tmpIndices = new List<int>();

	/// <summary>
	/// Choose a random element from the given arguments
	/// </summary>
	/// <typeparam name="TType">The type of object</typeparam>
	/// <param name="choosing">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static List<TType> Choose<TType>(List<TType> all, int count)
	{
		var choices = new List<TType>(count);

		for (var i = 0; i < all.Count; i++)
			_tmpIndices.Add(i);

		for (var i = 0; i < count; i++)
		{
			int choice = Next(_tmpIndices.Count);
			choices.Add(all[_tmpIndices[choice]]);
			_tmpIndices.RemoveAt(choice);
		}

		_tmpIndices.Clear();
		return choices;
	}


	/// <summary>
	/// Choose a random element from the given arguments
	/// </summary>
	/// <typeparam name="T">The type of object</typeparam>
	/// <param name="all">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static T Choose<T>(params T[] all)
	{
		switch (all.Length)
		{
			case 0:  return default;
			case 1:  return all[0];
			default: return all[Random.Range(0, all.Length)];
		}
	}

	/// <summary>
	/// Choose a random element from the given arguments
	/// </summary>
	/// <typeparam name="TType">The type of object</typeparam>
	/// <param name="choosing">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static TType Choose<TType>(IEnumerable<TType> choosing)
	{
		List<object> list = ListPool<object>.Claim();

		foreach (TType v in choosing)
			list.Add(v);

		object ret = Choose(list);
		ListPool<object>.Release(list);

		return (TType) ret;
	}

	/// <summary>
	/// Choose a random element from the given arguments
	/// </summary>
	/// <param name="choosing">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static object Choose(IEnumerable choosing)
	{
		List<object> list = ListPool<object>.Claim();

		foreach (object v in choosing)
			list.Add(v);

		object ret = Choose(list);
		ListPool<object>.Release(list);

		return ret;
	}


	/// <summary>
	/// Will return a random element from the given arguments except if it equals to the "not", then it will choose the next one.
	/// If none are found, return the default value of T
	/// </summary>
	/// <typeparam name="T">The object type</typeparam>
	/// <param name="not">To not return</param>
	/// <param name="choosing">The objects to choose from</param>
	/// <returns>The choosen object</returns>
	[CanBeNull] public static T ChooseExcept<T>(T not, params T[] choosing)
	{
		int pos = Random.Range(0, choosing.Length);
		for (int i = 0; i < choosing.Length; i++)
		{
			T found = choosing[i];
			if (found.Equals(not))
				continue;

			return found;
		}

		return default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Chance(float chance)
	{
		if (chance >= 1) return true;
		else if (chance <= 0) return false;

		return Range(1f) <= chance;
	}

	public static int Range(int min, int max, int not)
	{
		int r;

		do
			r = Random.Range(min, max);
		while (r == not);

		return r;
	}

	/// <summary>
	/// Get a point in a radial perimeter around a center.
	/// </summary>
	/// <param name="center">The center of the perimeter.</param>
	/// <param name="radius">The radius of the perimeter.</param>
	/// <returns>Random point in the radial perimeter.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 InRadius(Vector3 center, float radius)
	{
		Vector2 rnd = Random.insideUnitCircle * radius;
		return center + new Vector3(rnd.x, 0, rnd.y);
	}
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		// Min/Max
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Minimum(this float v, float min) => Mathf.Max(min, v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Maximum(this float v, float max) => Mathf.Min(max, v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Minimum(this int v, int min) => Mathf.Max(min, v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Maximum(this int v, int max) => Mathf.Min(max, v);

		// Checks
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Between(this int x, int from, int to) => x >= from && x <= to;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Between(this float x, float from, float to) => x >= from && x <= to;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Between<T>(this int x, IList<T> list) => x >= 0 && x < list.Count;

		// Clamping
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp01(this float v) => Mathf.Clamp01(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(this float v, float min, float max) => Mathf.Clamp(v, min, max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(this int v, int min, int max) => Mathf.Clamp(v, min, max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp<T>(this int i, IList<T> list) => Clamp(i, 0, list.Count - 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Pow(this float v, float power) => Mathf.Pow(v, power);

		// Wrapping
		/// <summary>
		/// Wrap a value to remain in range [0, max[
		/// </summary>
		/// <param name="v">The value to wrap.</param>
		/// <param name="max">The max value. (exclusive)</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Wrap(this int v, int max) => Wrap(v, 0, max - 1);

		/// <summary>
		/// Wrap a value to remain in range [min, max]
		/// </summary>
		/// <param name="v">The value to wrap.</param>
		/// <param name="min">The minimum value. (inclusive)</param>
		/// <param name="max">The maximum value. (inclusive)</param>
		/// <returns></returns>
		public static int Wrap(this int v, int min, int max)
		{
			max++; // Make this inclusive.

			if (max - min == 0)
				return 0;

			return ((v - min) % (max - min) + (max - min)) % (max - min) + min;
		}

		/// <summary>
		/// Wrap a value to remain in range [min, max]
		/// </summary>
		/// <param name="v">The value to wrap.</param>
		/// <param name="min">The minimum value. (inclusive)</param>
		/// <param name="max">The maximum value. (inclusive)</param>
		/// <returns></returns>
		public static float Wrap(this float v, float min, float max)
		{
			max++; // Make this inclusive.

			if (max - min == 0)
				return 0;

			return ((v - min) % (max - min) + (max - min)) % (max - min) + min;
		}

		public static float WrapAngle(this float v)
		{
			while (v < 0) v   += 360;
			while (v > 360) v -= 360;

			return v;
		}

		public static int WrapAngle(this int v)
		{
			while (v < 0) v   += 360;
			while (v > 360) v -= 360;

			return v;
		}

		// Rounding
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Floor(this float v) => Mathf.FloorToInt(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Round(this float v) => Mathf.RoundToInt(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Ceil(this float v) => Mathf.CeilToInt(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float RoundSnap(this float v, float snapping)
		{
			return Mathf.Round(Mathf.Round(v / snapping)) * snapping;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float FloorSnap(this float v, float snapping)
		{
			return Mathf.Floor(Mathf.Floor(v / snapping)) * snapping;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float CeilSnap(this float v, float snapping)
		{
			return Mathf.Ceil(Mathf.Ceil(v / snapping)) * snapping;
		}

		// Absolute values.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Abs(this float v) => Mathf.Abs(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Abs(this int v) => Mathf.Abs(v);

		// Sign.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sign(this float v) => Mathf.Sign(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Sign(this int v) => (int) Mathf.Sign(v);

		// Lerping.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Lerp(this float v, float target, float t) => Mathf.Lerp(v, target, t);

		// Radians/Degrees
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Deg(this float v) => Mathf.Rad2Deg * v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rad(this float v) => Mathf.Deg2Rad * v;

		// Timers.
		public static Timer Wait(this float v, Action callback) => new Timer(v, callback);
	}
}
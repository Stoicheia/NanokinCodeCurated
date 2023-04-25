using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util
{
	[Serializable]
	[Inline(true)]
	public struct FloatRange
	{
		[ShowInInspector]
		[HorizontalGroup, SuffixLabel("min", Overlay = true)]
		[HideLabel]
		public float min;
		[ShowInInspector]
		[HorizontalGroup, SuffixLabel("max", Overlay = true)]
		[HideLabel]
		public float max;

		public static readonly FloatRange ZeroOne = new FloatRange(0, 1);

		public float Span => max - min;

		public FloatRange(float min, float max)
		{
			this.min = min;
			this.max = max;
		}


		/// <summary>
		/// Returns a random number between from [inclusive] and to [inclusive].
		/// </summary>
		public float RandomInclusive => RNG.Range(min, max);

		/// <summary>
		/// Linearly interpolates between to and from by t.
		/// </summary>
		/// <param name="t">How much to interpolate. Clamped between 0 and 1. 0 is [to] and 1 is [from].</param>
		/// <returns></returns>
		public float Lerp(float t)
		{
			return Mathf.Lerp(min, max, t);
		}

		/// <summary>
		/// Linearly interpolates between to and from by t.
		/// </summary>
		/// <param name="t">How much to interpolate. 0 is [to] and 1 is [from].</param>
		/// <returns></returns>
		public float LerpUnclamped(float t)
		{
			return Mathf.LerpUnclamped(min, max, t);
		}

		/// <summary>
		/// Calculates the linear parameter t that produces the interpolant value within the range [from, max].
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public float InverseLerp(float value)
		{
			return Mathf.InverseLerp(min, max, value);
		}

		public bool Contains(float value)
		{
			return value.Between(min, max);
		}

		public bool CollectionContains(float value)
		{
			return value.Between(min, max - 1);
		}

		public float GetNormalizedPosition(float v)
		{
			return Mathf.InverseLerp(min, max, v);
		}

		public float GetAtNormalized(float t)
		{
			return Mathf.Lerp(min, max, t);
		}

		public static float Remap(float sp1_val, FloatRange space1, FloatRange space2)
		{
			float sp1_pos = space1.GetNormalizedPosition(sp1_val);
			float sp2_val = space2.GetAtNormalized(sp1_pos);

			return sp2_val;
		}

		public static implicit operator float(FloatRange range)
		{
			return range.RandomInclusive;
		}

		public FloatRange PointwiseOperation(Func<float, float> operation)
		{
			return new FloatRange(operation(min), operation(max));
		}

		public static FloatRange Clamp(FloatRange val, FloatRange min, FloatRange max)
		{
			return new FloatRange(Mathf.Clamp(val.min, min.min, max.min), Mathf.Clamp(val.max, min.max, max.max));
		}
	}
}
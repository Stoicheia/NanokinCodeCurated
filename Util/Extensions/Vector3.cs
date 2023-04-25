using UnityEngine;
using Util;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static Vector2 Floor(this Vector3 v, float snap = 1) => new Vector3(
			Mathf.FloorToInt(Mathf.FloorToInt(v.x / snap)) * snap,
			Mathf.FloorToInt(Mathf.FloorToInt(v.y / snap)) * snap,
			Mathf.FloorToInt(Mathf.FloorToInt(v.z / snap)) * snap
		);

		public static Vector3 Ceil(this Vector3 v, float snap = 1) => new Vector3(
			Mathf.CeilToInt(Mathf.CeilToInt(v.x / snap)) * snap,
			Mathf.CeilToInt(Mathf.CeilToInt(v.y / snap)) * snap,
			Mathf.CeilToInt(Mathf.CeilToInt(v.z / snap)) * snap
		);

		public static Vector3 Round(this Vector3 v, float snap = 1) => new Vector3(
			Mathf.RoundToInt(Mathf.RoundToInt(v.x / snap)) * snap,
			Mathf.RoundToInt(Mathf.RoundToInt(v.y / snap)) * snap,
			Mathf.RoundToInt(Mathf.RoundToInt(v.z / snap)) * snap
		);

		public static Vector3 Horizontal(this Vector3 v)
		{
			return new Vector3(v.x, 0, v.z);
		}

		public static Vector3 Vertical(this Vector3 v)
		{
			return new Vector3(0, v.y, 0);
		}


		public static Vector3 LerpDamp(this Vector3 v, Vector3 d, float damping)
		{
			return new Vector3(
				MathUtil.LerpDamp(v.x, d.x, damping),
				MathUtil.LerpDamp(v.y, d.y, damping),
				MathUtil.LerpDamp(v.z, d.z, damping)
			);
		}

		public static Vector3 LerpDampXY(this Vector3 v, Vector3 d, float damping)
		{
			return new Vector3(
				MathUtil.LerpDamp(v.x, d.x, damping),
				MathUtil.LerpDamp(v.y, d.y, damping),
				v.z
			);
		}

		public static Vector3 Lerp(this Vector3 v1, Vector3 v2, float t)
		{
			return Vector3.Lerp(v1, v2, t);
		}

		public static Vector3 Slerp(this Vector3 v1, Vector3 v2, float t)
		{
			return Vector3.Slerp(v1, v2, t);
		}

		public static Vector3 EasedLerp(this Vector3 v1, Vector3 v2, float t)
		{
			return Vector3.Lerp(v1, v2, MathUtil.EasedLerpFactor(t));
		}

		public static Vector3 Towards(this Vector3 v, Vector3 final, float ratio) => v + v.Towards(final) * (v.Distance(final) * ratio); // this is used in many places so I thought we can clear up some of the more convoluted code with that sugar

		public static Vector3 Towards(this Vector3 v, Vector3 final) => (final - v).normalized; // this is used in many places so I thought we can clear up some of the more convoluted code with that sugar

		public static float EasedLerp(this float v1, float v2, float t)
		{
			return Mathf.Lerp(v1, v2, MathUtil.EasedLerpFactor(t));
		}


		public static float LerpDamp(this float v, float target, float damping) =>
			MathUtil.LerpDamp(v, target, damping);

		public static Vector3 DropToGround(this Vector3 p, float retryHeight = 0)
		{
			bool    res;
			Vector3 hit = DropToGround(p, out res);
			if (!res)
			{
				hit = DropToGround(p + Vector3.up * retryHeight, out res);
			}

			return hit;
		}

		public static float SqrDistance(this Vector3 v, Vector3 to)
		{
			return (v - to).sqrMagnitude;
		}

		public static Vector3 DropToGround(this Vector3 p, out bool res)
		{
			RaycastHit rh;
			if (Physics.Raycast(
				p,
				Vector3.down,
				out rh,
				5f,
				Layers.Scenery.mask | Layers.Walkable.mask))
			{
				res = true;
				return rh.point;
				// Debug.Log($"Found ground at {rh.point}");
			}
			else
			{
				res = false;
				// Debug.Log($"Couldn't find ground beneath! {p}");
			}

			return p;
		}

		/// <param name="f">From</param>
		/// <param name="t">To</param>
		/// <returns>Returns the distance between f and t.</returns>
		public static float Distance(this Vector3 f, Vector3 t) => Vector3.Distance(f, t);

		/// <summary>
		/// Rotates the vector3 using euler angles.
		/// Returns a new vector3 thas is the result of the rotation.
		/// </summary>
		public static Vector3 Rotate(this Vector3 pos, float x = 0, float y = 0, float z = 0) => Quaternion.Euler(x, y, z) * pos;

		public static Vector3 Invert(this   Vector3 vec)                 => new Vector3(1     / vec.x,    1     / vec.y,    1     / vec.z);
		public static Vector3 Multiply(this Vector3 vec, Vector3 scalar) => new Vector3(vec.x * scalar.x, vec.y * scalar.y, vec.z * scalar.z);
		public static Vector3 Sign(this     Vector3 vec) => new Vector3(Mathf.Sign(vec.x), Mathf.Sign(vec.y), Mathf.Sign(vec.z));

		public static Vector3 Divide(this Vector3 vec, float scalar) => new Vector3(vec.x / scalar, vec.y / scalar, vec.z / scalar);

		public static Vector3 Step(this Vector3 vec, Vector3 edge)
			=> new Vector3(
				vec.x < edge.x ? 0.0f : 1.0f,
				vec.y < edge.y ? 0.0f : 1.0f,
				vec.z < edge.z ? 0.0f : 1.0f);

		public static bool AnyNAN(this Vector3 vec) => float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z);
		public static bool AllNAN(this Vector3 vec) => float.IsNaN(vec.x) && float.IsNaN(vec.y) && float.IsNaN(vec.z);
	}
}
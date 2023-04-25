using UnityEngine;
using Util;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static bool EqualInt(this Vector2 v1, Vector2 v2) => (int) v1.x == (int) v2.x && (int) v1.y == (int) v2.y;

		public static Vector2Int FloorToInt(this Vector2 v)
		{
			return new Vector2Int(
				v.x.Floor(),
				v.y.Floor()
			);
		}
		public static Vector2Int CeilToInt(this Vector2 v)
		{
			return new Vector2Int(
				v.x.Ceil(),
				v.y.Ceil()
			);
		}

		public static Vector2 Floor(this Vector2 v, float snap = 1)
			=> new Vector2(
				Mathf.FloorToInt(Mathf.FloorToInt(v.x / snap)) * snap,
				Mathf.FloorToInt(Mathf.FloorToInt(v.y / snap)) * snap
			);


		public static Vector2 Ceil(this Vector2 v, float snap = 1) => new Vector2(
			Mathf.CeilToInt(Mathf.CeilToInt(v.x / snap)) * snap,
			Mathf.CeilToInt(Mathf.CeilToInt(v.y / snap)) * snap
		);

		public static Vector2 Round(this Vector2 v, float snap = 1) => new Vector2(
			Mathf.RoundToInt(Mathf.RoundToInt(v.x / snap)) * snap,
			Mathf.RoundToInt(Mathf.RoundToInt(v.y / snap)) * snap
		);

		public static Vector2 LerpDamp(this Vector2 v, Vector2 destination, float damping)
		{
			return new Vector2(
				MathUtil.LerpDamp(v.x, destination.x, damping),
				MathUtil.LerpDamp(v.y, destination.y, damping)
			);
		}

		public static Vector2 Flip(this  Vector2 v, float xm = -1, float ym = -1) => new Vector2(v.x * xm, v.y * ym);
		public static Vector2 FlipX(this Vector2 v) => new Vector2(-v.x, v.y);
		public static Vector2 FlipY(this Vector2 v) => new Vector2(v.x,  -v.y);

		public static Vector2 Minimum(this Vector2 v, float x, float y) => new Vector2(v.x.Minimum(x), v.y.Minimum(y));

		public static float SqrDistance(this Vector2 v, Vector2 to)
		{
			return (v - to).sqrMagnitude;
		}
		/// <summary>
		/// Remap the two components of a vector2 to a vector3.
		///
		/// Examples:
		/// 	new Vector2(3, 5).Map3(1, 0, 1) == new Vector3(3, 0, 5);
		/// 	new Vector2(2, 1).Map3(0, 1, 1) == new Vector3(0, 2, 1);
		///
		/// </summary>
		/// <param name="v"></param>
		/// <param name="ints"></param>
		/// <returns></returns>
		public static Vector3 Map3(this Vector2 v, params int[] ints)
		{
			Vector3 remappedVector = new Vector3();

			for (int idx = 0; idx < ints.Length; idx++)
			{
				int component = ints[idx];

				if (component != 0)
				{
					remappedVector[idx] = v.x * component;
					break;
				}
			}

			for (int idx = ints.Length - 1; idx >= 0; idx--)
			{
				int component = ints[idx];

				if (component != 0)
				{
					remappedVector[idx] = v.y * component;
					break;
				}
			}

			return remappedVector;
		}

		public static float ToDegrees(this Vector2 p_vector2)
		{
			return 360 - Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg + 90;
		}

		public static Vector3[] ToRect3DPoints(this Vector2 v, Quaternion rot)
		{
			return new[]
			{
				rot * new Vector3( -v.x, -v.y, 0),
				rot * new Vector3(  v.x, -v.y, 0),
				rot * new Vector3(  v.x,  v.y, 0),
				rot * new Vector3( -v.x,  v.y, 0),
			};
		}

		/*public static float ToDegrees(this Vector2 p_vector2)
	{
		if (p_vector2.x < 0)
		{
			return 360 - (Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg * -1);
		}
		else
		{
			return Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg;
		}
	*/
	}
}
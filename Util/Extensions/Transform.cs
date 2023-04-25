using System;
using Anjin.Util;
using JetBrains.Annotations;
using UnityEngine;

namespace Util.Extensions
{
	public static partial class Extensions
	{
		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		[Obsolete]
		public static WorldPoint offset(this Transform tf, float distance)
		{
			// local
			// return actor.center + facing * distance;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(0, 0, distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public static WorldPoint offset(this Transform tf, float x, float y, float horizontal = 0)
		{
			// local
			// return actor.center + facing * x + actor.Up * y + Vector3.Cross(facing static, Vector3.up) * horizontal;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(horizontal, y, x),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public static WorldPoint identity_offset(this Transform tf, float d)
		{
			// local
			// return actor.center + facing * d + actor.Up * d;
			return diagonal(tf, d);;
		}

		[UsedImplicitly]
		public static WorldPoint diagonal(this Transform tf, float d)
		{
			// local
			// return actor.center + facing * d + actor.Up * d;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(0, d, d),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public static WorldPoint polar_offset(this Transform tf, float rad, float angle, float horizontal = 0)
		{
			// polar local
			// return rad * (
			// 	       Mathf.Cos(angle * Mathf.Deg2Rad static) * facing +
			// 	       Mathf.Sin(angle * Mathf.Deg2Rad) * actor.Up)
			//        + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(horizontal, rad, angle),
				offsetMode = WorldPoint.OffsetMode.LocalPolar
			};
		}

		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		public static WorldPoint ahead(this Transform tf, float distance, float horizontal = 0)
		{
			// local
			// return actor.center + facing * distance + Vector3.Cross(facing static, Vector3.up) * horizontal;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(horizontal, 0, distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		public static WorldPoint behind(this Transform tf, float distance, float horizontal = 0)
		{
			// local
			// return actor.center - facing * distance + Vector3.Cross(facing static, Vector3.up) * horizontal;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(horizontal, 0, -distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point above or under the fighter.
		/// </summary>
		[UsedImplicitly]
		public static WorldPoint above(this Transform tf, float distance = 0)
		{
			// local
			// return position + actor.Up * height + actor.Up * distance;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(0, distance, 0),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point above or under the fighter.
		/// </summary>
		[UsedImplicitly]
		public static WorldPoint under(this Transform tf, float distance)
		{
			// local
			// return position - actor.Up * distance;
			return new WorldPoint(tf)
			{
				offset     = new Vector3(0, -distance, 0),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}
	}
}
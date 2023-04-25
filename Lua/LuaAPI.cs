using System;
using System.Collections;
using Anjin.Util;
using Combat.Data;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.Assertions;
using Util;
using Util.Collections;
using Util.UniTween.Value;

namespace Anjin.Scripting
{
	[LuaUserdata(StaticName = "luaapi")]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class LuaAPI
	{
		private static WeightMap<DynValue> _weightMap = new WeightMap<DynValue>();

	#region Logging

		[LuaGlobalFunc]
		private static void printerr(string error)
		{
			DebugLogger.LogError(error);
		}

		[LuaGlobalFunc]
		private static void printwarn(string warning)
		{
			DebugLogger.LogWarning(warning);
		}

	#endregion

	#region Easy Constructors

		[LuaGlobalFunc] private static Vector2    vec2(float   x,        float y)          => new Vector2(x, y);
		[LuaGlobalFunc] private static Vector3    vec3(float   x,        float y, float z) => new Vector3(x, y, z);
		[LuaGlobalFunc] private static Vector2Int vec2i(int    x,        int   y)                   => new Vector2Int(x, y);
		[LuaGlobalFunc] public static  Color      rgb(float    r,        float g, float b)          => new Color(r, g, b);
		[LuaGlobalFunc] public static  Color      rgba(float   r,        float g, float b, float a) => new Color(r, g, b, a);
		[LuaGlobalFunc] private static EaserTo    easer(float  duration, Ease  ease)                                            => new EaserTo(duration, ease);
		[LuaGlobalFunc] private static JumperTo   jumper(float duration, float power, int   count = 1, Ease ease = Ease.Linear) => new JumperTo(duration, power, count, ease);
		[LuaGlobalFunc] private static float      ilerp(float  a,        float b,     float v) => Mathf.InverseLerp(a, b, v);
		[LuaGlobalFunc] private static float      lerp(float   a,        float b,     float t) => Mathf.Lerp(a, b, t);
		[LuaGlobalFunc] private static float      scurve(float x,        float k) => MathUtil.jcurve(x, k);
		[LuaGlobalFunc] private static float      jcurve(float x,        float k) => MathUtil.jcurve(x, k);
		[LuaGlobalFunc] private static float      rcurve(float x,        float k) => MathUtil.rcurve(x, k);

	#endregion

	#region Easy Maths

		// [LuaGlobalFunc]
		// public static float distance(Vector2Int a, Vector2Int b)
		// {
		// 	return Vector2Int.Distance(a, b);
		// }
		//
		// [LuaGlobalFunc]
		// public static float distance(Vector2 a, Vector2 b)
		// {
		// 	return Vector2.Distance(a, b);
		// }

		[LuaGlobalFunc]
		public static float dist2i(Vector2Int a, Vector2Int b)
		{
			return Vector2.Distance(a, b);
		}


		[LuaGlobalFunc]
		public static float dist2(Vector2 a, Vector2 b)
		{
			return Vector2.Distance(a, b);
		}

		[LuaGlobalFunc]
		public static float distance(WorldPoint wp1, WorldPoint wp2)
		{
			if (wp1.TryGet(out Vector3 p1) && wp2.TryGet(out Vector3 p2))
				return Vector3.Distance(p1, p2);
			return 0;
		}

		[LuaGlobalFunc]
		public static float clamp(float current, float min, float max)
		{
			return Mathf.Clamp(current, min, max);
		}

	#endregion

	#region Easy Random

		public static object rng(IEnumerable enumerable)   => RNG.Choose(enumerable);
		public static int    rng(int         max)          => RNG.Range(1, max);
		public static int    rng(int         min, int max) => RNG.Range(min, max);

		[LuaGlobalFunc] public static Vector3 rng2(DynValue dvx, DynValue dvy)
		{
			float x = 0;
			float y = 0;
			float z = 0;

			if (dvx.Type == DataType.Number && dvy.IsNil())
			{
				x = dvx.AsFloat();
				y = x;
				z = x;
			}
			else if (dvx.Type == DataType.Number && dvy.Type == DataType.Number)
			{
				x = dvx.AsFloat();
				y = dvy.AsFloat();
			}
			else
			{
				throw new ArgumentException("Invalid inputs to rng2");
			}

			return new Vector3(RNG.FloatSigned * x, RNG.FloatSigned * y, RNG.FloatSigned * z);
		}

		[LuaGlobalFunc] public static Vector3 rng3([NotNull] DynValue dvx, [NotNull] DynValue dvy, [NotNull] DynValue dvz)
		{
			float x = 0;
			float y = 0;
			float z = 0;

			if (dvx.Type == DataType.Number && (dvy.IsNil() && dvz.IsNil()))
			{
				x = dvx.AsFloat();
				y = x;
				z = x;
			}
			else if (dvx.Type == DataType.Number && dvy.Type == DataType.Number && dvz.Type == DataType.Number)
			{
				x = dvx.AsFloat();
				y = dvy.AsFloat();
				z = dvz.AsFloat();
			}
			else
			{
				throw new ArgumentException("Invalid inputs to rng3");
			}

			return new Vector3(RNG.FloatSigned * x, RNG.FloatSigned * y, RNG.FloatSigned * z);
		}

		public static float rngf(DynValue dvmin, DynValue dvmax)
		{
			if (dvmin.IsNil() && dvmax.IsNil()) return RNG.Float;    // rngf() returns 0 to 1
			else return RNG.Range(dvmin.AsFloat(), dvmax.AsFloat()); // rngf(min, max) returns in range [min, max] both inclusive
		}

		[LuaGlobalFunc]
		public static float rng01() => RNG.Float;

		[LuaGlobalFunc]
		public static float rng11() => RNG.FloatSigned;

		[LuaGlobalFunc]
		public static Table shuffle(Table tbl)
		{
			if (tbl.Length == 1) return tbl;

			for (int i = tbl.Length; i >= 1; i--)
			{
				int j = RNG.Range(1, i);

				DynValue vi = tbl.Get(i);
				DynValue vj = tbl.Get(j);

				tbl.Set(j, vi);
				tbl.Set(i, vj);
			}

			return tbl;
		}

		public static DynValue rng([NotNull] Table values)
		{
			int pick = RNG.Int(values.Length);
			return values.Get(pick + 1);
		}

		public static DynValue rng([NotNull] Table values, [CanBeNull] Table weights)
		{
			if (weights == null)
			{
				int pick = RNG.Int(values.Length);
				return values.Get(pick + 1);
			}
			else
			{
				Assert.IsTrue(values.Length == weights.Length);

				_weightMap.Clear();

				for (var i = 1; i <= values.Length; i++)
				{
					DynValue value  = values.Get(i);
					DynValue weight = weights.Get(i);

					_weightMap.Add(value, (float)weight.Number);
				}

				return _weightMap.Choose();
			}
		}

		public static object rng([NotNull] DynValue dv)
		{
			if (dv.AsTable(out Table tbl))
			{
				int i = RNG.Int(tbl.Length);
				return tbl.Get(i + 1);
			}
			else if (dv.AsUserdata(out Target tg))
			{
				int i = RNG.Int(tg.Count);
				return tg[i];
			}
			else if (dv.AsUserdata(out Targeting targeting))
			{
				int i = RNG.Int(targeting.picks.Count);
				return targeting.picks[i];
			}

			return dv;
		}

		public static int int_or_range(DynValue dvalue)
		{
			switch (dvalue.Type)
			{
				case DataType.Number: return (int)dvalue.Number;
				case DataType.Table:
					Table tbl = dvalue.Table;
					if (tbl.Length == 2)
						return rng((int)tbl.Get(1).Number, (int)tbl.Get(2).Number);
					break;
			}

			throw new InvalidOperationException();
		}

		public static float float_or_range(DynValue dvalue)
		{
			switch (dvalue.Type)
			{
				case DataType.Number: return (float)dvalue.Number;
				case DataType.Table:
					Table tbl = dvalue.Table;
					if (tbl.Length == 2)
						return rngf(tbl.Get(1), tbl.Get(2));
					break;
			}

			throw new InvalidOperationException();
		}

		[MoonSharpUserDataMetamethod("__add")]
		public static Vector2Int Add(Vector2Int v2i, int x)
		{
			return v2i * x;
		}

		[MoonSharpUserDataMetamethod("__add")]
		public static Vector2Int Add(Vector2Int v2i, float x)
		{
			return v2i * (int)x;
		}

	#endregion

	#region Utilities

		[LuaGlobalFunc]
		public static bool is_waitable(object obj)
		{
			return obj is ICoroutineWaitable;
		}

	#endregion
	}
}
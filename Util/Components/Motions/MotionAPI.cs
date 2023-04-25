using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.UniTween.Value;

namespace Anjin.Utils
{
	public static class MotionAPI
	{
		/// <summary>
		/// Direct contact handler
		/// </summary>
		/// <param name="dtarget"></param>
		/// <param name="dcallback"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static object ContactDV([NotNull] DynValue dtarget, DynValue dcallback, TargetedContactCallback.Type type)
		{
			// Multiple targets --> return a list of many TCC
			// ----------------------------------------
			if (dtarget.AsTable(out Table tbtarget))
			{
				var list = new List<TargetedContactCallback>();

				for (var i = 1; i <= tbtarget.Length; i++)
				{
					DynValue dv  = tbtarget.Get(i);
					var      tcc = (TargetedContactCallback)ContactDV(dv, dcallback, type);
					list.Add(tcc);
				}

				return list;
			}

			if (dtarget.IsNil())
				return null;

			var ret = new TargetedContactCallback
			{
				type     = type,
				t_object = dtarget.AsObject<GameObject>(),
				t_pos    = dtarget.AsVector3(),
				t_wpid   = dtarget.AsString(),
				callback = CallbackDV(dcallback)
			};

			if (dtarget.AsObject(out Proc proc))
			{
				ret.callback.proc = proc;
			}

			return ret;
		}

		private static ContactCallback CallbackDV(DynValue dcallback)
		{
			if (dcallback.AsObject(out Table _)) throw new NotImplementedException(); // TODO multiple contacts for one filter
			else if (dcallback.AsFunction(out Closure cl)) return new ContactCallback { luaClosure = cl };
			else if (dcallback.AsObject(out Proc proc)) return new ContactCallback { proc          = proc };
			else if (dcallback.AsObject(out ContactCallback cc)) return cc;
			else
				throw new NotImplementedException();
		}

		/// <summary>
		/// Waypoint callback
		/// </summary>
		[LuaGlobalFunc]
		public static object Contact([NotNull] DynValue dtarget, DynValue dcallback)
		{
			return ContactDV(dtarget, dcallback, TargetedContactCallback.Type.Waypoint);
		}

		/// <summary>
		/// Unity collision/trigger contact
		/// </summary>
		[LuaGlobalFunc]
		public static object Contactu([NotNull] DynValue dtarget, DynValue dcallback)
		{
			return ContactDV(dtarget, dcallback, TargetedContactCallback.Type.Unity);
		}

		/// <summary>
		/// Proximity contact
		/// </summary>
		/// <param name="dtarget"></param>
		/// <param name="dcallback"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		[LuaGlobalFunc]
		public static object Contactp([NotNull] DynValue dtarget, DynValue dcallback)
		{
			return ContactDV(dtarget, dcallback, TargetedContactCallback.Type.Proximity);
		}

		/// <summary>
		/// Delayed contact
		/// </summary>
		/// <param name="dtarget"></param>
		/// <param name="dcallback"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		[LuaGlobalFunc]
		public static object Contactd([NotNull] DynValue dtarget, DynValue dcallback)
		{
			throw new NotImplementedException();
		}

		[LuaGlobalFunc]
		public static WorldPoint2 MTarget(DynValue dv)
		{
			if (as_worldpoint2(dv, out WorldPoint2 ret))
			{
				return ret;
			}

			return default;
		}

		[LuaGlobalFunc]
		public static WorldPoint2 Midpoint(DynValue dvt1, DynValue dvt2, float midpoint = 0.5f)
		{
			if (dvt1.AsWorldPoint(out WorldPoint wp1) &&
			    dvt2.AsWorldPoint(out WorldPoint wp2))
			{
				return new WorldPoint2(wp1, wp2, midpoint);
			}

			return default;
		}

		[LuaGlobalFunc]
		public static MotionDef None()
		{
			return new MotionDef
			{
				Type = Motions.None
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Follow()
		{
			return MotionDef.Lock;
		}

		[LuaGlobalFunc]
		public static MotionDef Tween(float duration, Ease ease)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new EaserTo(duration, ease)
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Jumper(float duration, float height, int hops)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new JumperTo(duration, height, hops)
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Tween3(float duration, DynValue xCurve, DynValue yCurve, DynValue zCurve, float xOff, float yOff, float zOff)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new AxesTo(duration, xCurve, yCurve, zCurve, xOff, yOff * 6, zOff * 6)
			};
		}


		[LuaGlobalFunc]
		public static MotionDef Tween2(float duration, DynValue xCurve, DynValue yCurve, float xOff, float yOff)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new AxesTo(duration, xCurve, yCurve, xCurve, xOff, yOff * 6, 0)
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Tween2Polar(float duration, DynValue rCurve, DynValue thetaCurve, float rOff, float thetaOff)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new PolarAxesTo(duration, rCurve, thetaCurve, rOff, thetaOff)
			};
		}


		[LuaGlobalFunc]
		public static MotionDef TweenJump(float duration, float power, int count = 1)
		{
			return new MotionDef
			{
				Type  = Motions.Tween,
				Tween = new JumperTo(duration, power, count)
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Accelerator(float accel, float baseSpeed)
		{
			return new MotionDef
			{
				Type         = Motions.Accelerator,
				Acceleration = accel,
				Speed        = baseSpeed
			};
		}

		[LuaGlobalFunc]
		public static MotionDef Damper(float damping)
		{
			return new MotionDef
			{
				Type    = Motions.Damper,
				Damping = damping
			};
		}

		[LuaGlobalFunc]
		public static MotionDef SmoothDamp(float smoothTime, float maxSpeed = 6.5f)
		{
			return new MotionDef
			{
				Type       = Motions.SmoothDamp,
				SmoothTime = smoothTime,
				MaxSpeed   = maxSpeed,
			};
		}

		public static bool as_worldpoint2(this DynValue dv, out WorldPoint2 wp2)
		{
			if (dv.AsExact(out wp2))
			{
				return true;
			}
			else if (dv.AsExact(out WorldPoint wpExact))
			{
				wp2 = wpExact;
				return true;
			}
			else if (dv.AsWorldPoint(out WorldPoint wp))
			{
				wp2 = new WorldPoint2(wp);
				return true;
			}
			else if (dv.AsObject(out GameObject go)) // waypoint
			{
				wp2 = new WorldPoint2(go);
				return true;
			}
			else if (dv.AsObject(out Vector3 pos))
			{
				wp2 = new WorldPoint2(pos);
				return true;
			}
			else if (dv.AsObject(out Vector2 pos2D))
			{
				wp2 = new WorldPoint2(pos2D.x_y());
				return true;
			}

			wp2 = new WorldPoint2 { Type = WorldPoint2.Types.Previous };
			return false;
		}
	}
}
using System;
using Anjin.Scripting;
using API.PropertySheet;
using Combat.Data.VFXs;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityUtilities;

namespace Combat.Toolkit
{
	public static class VFXAPI
	{
		[LuaGlobalFunc]
		[NotNull]
		public static FXVFX v_fx(DynValue dv, Table conf)
		{
			if (dv.AsGameObject(out var go))
			{
				return new FXVFX(new FX
				{
					prefab = go,
					config = conf
				});
			}
			else if (dv.AsString(out string addr))
			{
				return new FXVFX(new FX
				{
					address = addr,
					config  = conf
				});
			}

			throw new ArgumentException();
		}

		[LuaGlobalFunc]
		[NotNull]
		public static ShakeVFX v_shake(
			DynValue amplitude,
			DynValue speed,
			DynValue randomness)
		{
			var vfx = new ShakeVFX();

			amplitude.AsFloat(out vfx.amplitude, vfx.amplitude);
			speed.AsFloat(out vfx.speed, vfx.speed);
			randomness.AsFloat(out vfx.randomness, vfx.randomness);

			return vfx;
		}

		[LuaGlobalFunc]
		[NotNull]
		public static BlinkVFX v_blink(Table conf)
		{
			var blink = new BlinkVFX();

			conf.TryGet("fill", out blink.fill);
			conf.TryGet("tint", out blink.tint);
			conf.TryGet("speed", out blink.speed);
			conf.TryGet("fill", out blink.fill);
			conf.TryGet("power", out blink.power);

			return blink;
		}

		[LuaGlobalFunc]
		[NotNull]
		public static AnimVFX v_anim(DynValue anim, DynValue startMarker, DynValue endMarker)
		{
			if (anim.AsExact(out string name))
			{
				return new AnimVFX(name);
			} else if (anim.AsExact(out PuppetAnimation puppetanim))
			{
				return new AnimVFX(puppetanim, startMarker.AsString(), endMarker.AsString());
			}

			throw new ArgumentException("Argument to v_anim must be a string or PuppetAnimation");
		}

		[LuaGlobalFunc]
		[NotNull]
		public static FreezeVFX v_freeze()
		{
			return new FreezeVFX();
		}

		[LuaGlobalFunc]
		[NotNull]
		public static ManualVFX v_tint(DynValue r, DynValue g, DynValue b, DynValue a)
		{
			/// TODO use an actual TintVFX that can tween nicely

			if (r.AsExact(out Color color))
				return new ManualVFX { tint = color };

			return new ManualVFX
			{
				tint = new Color(
					r.AsFloat(),
					g.AsFloat(),
					b.AsFloat(),
					a.AsFloat(1))
			};
		}

		[LuaGlobalFunc]
		[NotNull]
		public static ManualVFX v_opacity(float opacity)
		{
			return new ManualVFX { tint = Color.white.Alpha(opacity) };
		}

		[LuaGlobalFunc]
		[NotNull]
		public static ScaleVFX v_scale(DynValue x, DynValue y, DynValue z)
		{
			if (x.AsFloat(out float v)) return new ScaleVFX(v); // v_scale(float) overload
			if (x.AsVector3(out Vector3 vec3))
			{
				return new ScaleVFX(vec3); // v_scale(vector3) overload
			}
			else
			{
				// v_scale(x, y, z) overload
				return new ScaleVFX(new Vector3(
					x.AsFloat(),
					y.AsFloat(),
					z.AsFloat()
				));
			}
		}
	}
}
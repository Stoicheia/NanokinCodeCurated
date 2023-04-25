using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Combat.Data.VFXs;
using Combat.Toolkit;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat
{
	public static class CombatAPI
	{
		[LuaGlobalFunc] private static bool is_fighter([NotNull] DynValue dv) => dv.Type == DataType.UserData && dv.UserData.Object is Fighter;
		[LuaGlobalFunc] private static bool is_slot([NotNull]    DynValue dv) => dv.Type == DataType.UserData && dv.UserData.Object is Slot;
		[LuaGlobalFunc] private static bool is_state([NotNull]   DynValue dv) => dv.Type == DataType.UserData && dv.UserData.Object is State;
		[LuaGlobalFunc] private static bool is_proc([NotNull]    DynValue dv) => dv.Type == DataType.UserData && dv.UserData.Object is Proc;

		[LuaGlobalFunc, NotNull] private static string get_nature(Elements elem) => elem.GetNature().ToString().ToLower();

		public static bool ReadProcVictims(Proc proc, [NotNull] DynValue dv)
		{
			if (dv.IsNil())
				return false;

			dv.AsUserdata(out Fighter victimFighter);
			dv.AsUserdata(out Target victimTarget);
			dv.AsUserdata(out List<Fighter> victimFighters);

			if (victimFighter != null)
			{
				proc.AddVictim(victimFighter);
				return true;
			}

			if (victimTarget != null)
			{
				proc.AddVictims(victimTarget.fighters);
				return true;
			}

			if (victimFighters != null)
			{
				proc.AddVictims(victimFighters);
				return true;
			}

			return false;
		}

		public static void ReadProcEffects(Proc proc, [NotNull] Table effectTable)
		{
			foreach (TablePair pair in effectTable.Pairs)
			{
				if (pair.Value.AsUserdata(out ProcEffect ProcEffect))
					proc.AddEffect(ProcEffect);
				else if (pair.Value.AsUserdata(out State state))
					proc.AddEffect(state);
			}
		}

		public static Statf ReadStatFloats([NotNull] DynValue dynval)
		{
			switch (dynval.Type)
			{
				case DataType.Number: return new Statf((float)dynval.Number, (float)dynval.Number, (float)dynval.Number);
				case DataType.Table:
					Table tbl = dynval.Table;
					return new Statf
					{
						power = tbl.TryGet<float>("power", 1),
						speed = tbl.TryGet<float>("speed", 1),
						will  = tbl.TryGet<float>("will", 1)
					};
				default: return default;
			}
		}

		public static Elementf ReadElementFloats([NotNull] DynValue dynval)
		{
			switch (dynval.Type)
			{
				case DataType.Number:
					var v = (float)dynval.Number;
					return new Elementf(v, v, v, v, v, v);

				case DataType.Table:
					Table tbl = dynval.Table;
					return new Elementf(
						tbl.TryGet<float>("blunt", 1),
						tbl.TryGet<float>("pierce", 1),
						tbl.TryGet<float>("slash", 1),
						tbl.TryGet<float>("gaia", 1),
						tbl.TryGet<float>("astra", 1),
						tbl.TryGet<float>("oida", 1),
						tbl.TryGet<float>("physical", 1),
						tbl.TryGet<float>("magical", 1)
					);

				default: return default;
			}
		}

		[NotNull]
		public static ReactVFX ReadReactVFX(Table conf)
		{
			var vfx = new ReactVFX
			{
				hurtFrames  = 0,
				shakeFrames = 0,
				tintColor   = Color.white,
				flashFrames = 0,
				recoil      = false,
				hurtAnim    = true
			};

			ReadReactVFX(conf, vfx);
			return vfx;
		}

		public static void ReadReactVFX(Table conf, [NotNull] ReactVFX vfx)
		{
			// feature frame counts
			conf.GetInto("freeze", ref vfx.freezeFrames);
			conf.GetInto("hurt", ref vfx.hurtFrames);

			if (conf.TryGet("play_anim", out bool playAnim))
			{
				vfx.hurtAnim = playAnim;
			}

			if (conf.TryGet("shake", out Table tbshake))
			{
				vfx.shake          = true;
				vfx.shakeFrames    = (int)tbshake.Get(1).Number;
				vfx.shakeAmplitude = (float)tbshake.Get(2).Number;
				// ("shake", ref vfx.shakeFrames);
				// ("shake_amplitude", ref vfx.shake);
			}

			if (conf.TryGet("flash", out Table tbflash))
			{
				vfx.flash       = true;
				vfx.flashFrames = (int)tbflash.Get(1).Number;
				vfx.flashColor  = (Color)tbflash.Get(2).UserData.Object;
			}

			if (conf.TryGet("tint", out Table tbtint))
			{
				vfx.tint             = true;
				vfx.tintOut.duration = (float)tbtint.Get(1).Number * 1 / 60f; // Tint is in stored since it's a tween
				vfx.tintColor        = (Color)tbtint.Get(2).UserData.Object;
			}

			if (conf.TryGet("recoil", out float recoilforce))
			{
				vfx.recoil      = true;
				vfx.recoilForce = recoilforce;
			}
			else if (conf.TryGet("recoil", out Table tbrecoil))
			{
				vfx.recoil      = true;
				vfx.recoilForce = tbrecoil.Get("force").AsFloat(vfx.recoilForce);
				vfx.recoilIn    = tbrecoil.Get("in").AsUserdata(vfx.recoilIn);
				vfx.recoilHold  = tbrecoil.Get("hold").AsFloat(vfx.recoilHold);
				vfx.recoilOut   = tbrecoil.Get("out").AsUserdata(vfx.recoilOut);
				// vfx.tintOut.duration = (float) tbrecoil.Get(1).Number * 1/60f; // The tint is in seconds, not frames
				// vfx.tintColor        = (Color) tbrecoil.Get(2).UserData.Object;
			}

			// feature options
		}

		[CanBeNull]
		public static VFX ReadTintVFX(DynValue tb)
		{
			if (tb.AsObject(out Color col))
				return new ManualVFX { tint = col };

			return null;
		}

		[CanBeNull]
		public static VFX ReadFillVFX(DynValue tb)
		{
			if (tb.AsObject(out Color col))
				return new ManualVFX { fill = col };

			return null;
		}

		[NotNull]
		public static BlinkVFX ReadBlinkVFX(Table conf)
		{
			var vfx = new BlinkVFX
			{
				power = 1,
				speed = 1,
				fill  = Color.white
			};

			ReadBlinkVFX(conf, vfx);
			return vfx;
		}

		public static void ReadBlinkVFX(Table conf, [NotNull] BlinkVFX vfx)
		{
			//conf.GetInto("fill_col", ref vfx.fill);
			string fill_color = conf.Get("fill").AsString();
			string tint_color = conf.Get("tint").AsString();

			if (!string.IsNullOrEmpty(fill_color))
			{
				Color fill = (Color)typeof(ColorsXNA).GetField(fill_color).GetValue(null);
				DebugLogger.Log("FILL: " + fill_color + "; values: " + fill, LogContext.Combat, LogPriority.Low);
				vfx.fill = fill;
			}

			if (!string.IsNullOrEmpty(tint_color))
			{
				Color tint = (Color)typeof(ColorsXNA).GetField(fill_color).GetValue(null);
				DebugLogger.Log("TINT: " + tint_color + "; values: " + tint, LogContext.Combat, LogPriority.High);
				vfx.tint = tint;
			}
		}

		[NotNull]
		public static FlashColorVFX ReadFlashColorVFX(Table conf)
		{
			var vfx = new FlashColorVFX
			{
				frames = 6,
				power  = 1,
				speed  = 1,
				fill   = Color.white,
				tint   = Color.clear
			};

			ReadFlashColorVFX(conf, vfx);
			return vfx;
		}

		public static void ReadFlashColorVFX(Table conf, [NotNull] FlashColorVFX vfx)
		{
			if (conf.TryGet("frames", out int frames)) vfx.frames = frames;
			if (conf.TryGet("power", out float power)) vfx.power  = power;
			if (conf.TryGet("speed", out float speed)) vfx.speed  = speed;

			if (conf.TryGet("fill", out string fillColor))
			{
				Color fill = (Color)typeof(ColorsXNA).GetField(fillColor).GetValue(null);
				DebugLogger.Log("FILL: " + fillColor + "; values: " + fill, LogContext.Combat, LogPriority.Low);
				vfx.fill = fill;
			}

			if (conf.TryGet("tint", out string tintColor))
			{
				Color tint = (Color)typeof(ColorsXNA).GetField(tintColor).GetValue(null);
				DebugLogger.Log("TINT: " + tintColor + "; values: " + tint, LogContext.Combat, LogPriority.Low);
				vfx.tint = tint;
			}
		}

		public static BattleBrain Brain(BattleBrains brainType)
		{
			BattleBrain brain;
			switch (brainType)
			{
				case BattleBrains.player:
					brain = new PlayerBrain();
					break;

				case BattleBrains.debug:
					brain = new DebugBrain();
					break;

				case BattleBrains.auto:
					brain = new AutoBrain();
					break;

				case BattleBrains.none:
				case BattleBrains.skip:
					brain = new SkipTurnBrain();
					break;

				case BattleBrains.random:
					brain = new RandomBrain();
					break;

				default:
					brain = new SkipTurnBrain();
					Debug.LogError($"Unknown brain type: {brainType}, default to SkipTurnBrain");
					break;
			}

			return brain;
		}
	}
}
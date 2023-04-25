using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[LuaUserdata(StaticName = "stateapi_randomize")]

	// ReSharper disable once InconsistentNaming
	public static class StateAPI_randomize
	{
		public static StateFunc limb(LimbType      limb) => new StateFunc(StatOp.randomize, StateStat.skill_usable, limb);
		public static StateFunc skill(List<string> tags) => new StateFunc(StatOp.randomize, StateStat.skill_usable, tags);
		public static StateFunc skill(string       tag)  => new StateFunc(StatOp.randomize, StateStat.skill_usable, tag);

		public static StateFunc usetarget(float        chance, Closure weight = null) => new StateFunc(StatOp.randomize, StateStat.use_target_options) { chance   = chance, closure = weight };
		public static StateFunc skilltarget(float      chance, Closure weight = null) => new StateFunc(StatOp.randomize, StateStat.skill_target_options) { chance = chance, closure = weight };
		public static StateFunc usetargetpicks(float   chance, Closure weight = null) => new StateFunc(StatOp.randomize, StateStat.use_target_options) { chance   = chance, closure = weight };
		public static StateFunc skilltargetpicks(float chance, Closure weight = null) => new StateFunc(StatOp.randomize, StateStat.skill_target_options) { chance = chance, closure = weight };

		public static StateFunc stickertarget(Fighter fighter) => new StateFunc(StatOp.randomize, StateStat.sticker_target_options, fighter);
		public static StateFunc stickertarget(Slot    slot)    => new StateFunc(StatOp.randomize, StateStat.sticker_target_options, slot);

		// Utilities
		// ----------------------------------------
		private static Elementf ReadModifierElement([NotNull] DynValue dv)
		{
			if (dv.Type == DataType.Number) return Elementf.Zero; // global modifier, handled with nature instead
			return CombatAPI.ReadElementFloats(dv);
		}
	}
}
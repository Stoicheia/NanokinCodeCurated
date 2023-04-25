using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat
{
	[LuaUserdata(StaticName = "stateapi_forbid")]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class StateAPI_forbid
	{
		public static StateFunc limb(LimbType     limb) => new StateFunc(StatOp.forbid, StateStat.skill_usable, limb);
		public static StateFunc skill(List<string> tags) => new StateFunc(StatOp.forbid, StateStat.skill_usable, tags);
		public static StateFunc skill(string       tag)  => new StateFunc(StatOp.forbid, StateStat.skill_usable, tag);

		public static StateFunc usetarget(Fighter fighter) => new StateFunc(StatOp.forbid, StateStat.use_target_options, fighter) {fighter = fighter};
		public static StateFunc usetarget(Slot    slot)    => new StateFunc(StatOp.forbid, StateStat.use_target_options, slot);

		public static StateFunc skilltarget(Fighter fighter) => new StateFunc(StatOp.forbid, StateStat.skill_target_options, fighter);
		public static StateFunc skilltarget(Slot    slot)    => new StateFunc(StatOp.forbid, StateStat.skill_target_options, slot);

		public static StateFunc stickertarget(Fighter fighter) => new StateFunc(StatOp.forbid, StateStat.sticker_target_options, fighter);
		public static StateFunc stickertarget(Slot    slot)    => new StateFunc(StatOp.forbid, StateStat.sticker_target_options, slot);

		// Utilities
		// ----------------------------------------
		private static Elementf ReadModifierElement([NotNull] DynValue dv)
		{
			if (dv.Type == DataType.Number) return Elementf.Zero; // global modifier, handled with nature instead
			return CombatAPI.ReadElementFloats(dv);
		}
	}
}
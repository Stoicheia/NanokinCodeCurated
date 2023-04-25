using Anjin.Scripting;
using Combat.Data;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[LuaUserdata]
	public static class StateAPI_func
	{
		public static StateCmd points(float v) => new StateCmd(StatOp.scale, StateStat.points, v);
		public static StateCmd hp(float     v) => new StateCmd(StatOp.scale, StateStat.hp, v);
		public static StateCmd sp(float     v) => new StateCmd(StatOp.scale, StateStat.sp, v);
		public static StateCmd op(float     v) => new StateCmd(StatOp.scale, StateStat.op, v);

		public static StateCmd stats(float v) => new StateCmd(StatOp.scale, StateStat.stats, v);
		public static StateCmd power(float v) => new StateCmd(StatOp.scale, StateStat.power, v);
		public static StateCmd speed(float v) => new StateCmd(StatOp.scale, StateStat.speed, v);
		public static StateCmd will(float  v) => new StateCmd(StatOp.scale, StateStat.will, v);

		public static StateCmd res(float          v) => new StateCmd(StatOp.scale, StateStat.res, v);
		public static StateCmd res_blunt(float    v) => new StateCmd(StatOp.scale, StateStat.res_blunt, v);
		public static StateCmd res_slash(float    v) => new StateCmd(StatOp.scale, StateStat.res_slash, v);
		public static StateCmd res_pierce(float   v) => new StateCmd(StatOp.scale, StateStat.res_pierce, v);
		public static StateCmd res_gaia(float     v) => new StateCmd(StatOp.scale, StateStat.res_gaia, v);
		public static StateCmd res_oida(float     v) => new StateCmd(StatOp.scale, StateStat.res_oida, v);
		public static StateCmd res_astra(float    v) => new StateCmd(StatOp.scale, StateStat.res_astra, v);
		public static StateCmd res_physical(float v) => new StateCmd(StatOp.scale, StateStat.res_physical, v);
		public static StateCmd res_magical(float  v) => new StateCmd(StatOp.scale, StateStat.res_magical, v);

		public static StateCmd atk(float          v) => new StateCmd(StatOp.scale, StateStat.atk, v);
		public static StateCmd atk_blunt(float    v) => new StateCmd(StatOp.scale, StateStat.atk_blunt, v);
		public static StateCmd atk_slash(float    v) => new StateCmd(StatOp.scale, StateStat.atk_slash, v);
		public static StateCmd atk_pierce(float   v) => new StateCmd(StatOp.scale, StateStat.atk_pierce, v);
		public static StateCmd atk_gaia(float     v) => new StateCmd(StatOp.scale, StateStat.atk_gaia, v);
		public static StateCmd atk_oida(float     v) => new StateCmd(StatOp.scale, StateStat.atk_oida, v);
		public static StateCmd atk_astra(float    v) => new StateCmd(StatOp.scale, StateStat.atk_astra, v);
		public static StateCmd atk_physical(float v) => new StateCmd(StatOp.scale, StateStat.atk_physical, v);
		public static StateCmd atk_magical(float  v) => new StateCmd(StatOp.scale, StateStat.atk_magical, v);

		public static StateCmd def(float          v) => new StateCmd(StatOp.scale, StateStat.def, v);
		public static StateCmd def_blunt(float    v) => new StateCmd(StatOp.scale, StateStat.def_blunt, v);
		public static StateCmd def_slash(float    v) => new StateCmd(StatOp.scale, StateStat.def_slash, v);
		public static StateCmd def_pierce(float   v) => new StateCmd(StatOp.scale, StateStat.def_pierce, v);
		public static StateCmd def_gaia(float     v) => new StateCmd(StatOp.scale, StateStat.def_gaia, v);
		public static StateCmd def_oida(float     v) => new StateCmd(StatOp.scale, StateStat.def_oida, v);
		public static StateCmd def_astra(float    v) => new StateCmd(StatOp.scale, StateStat.def_astra, v);
		public static StateCmd def_physical(float v) => new StateCmd(StatOp.scale, StateStat.def_physical, v);
		public static StateCmd def_magical(float  v) => new StateCmd(StatOp.scale, StateStat.def_magical, v);

		public static StateCmd use_cost(float     v) => new StateCmd(StatOp.scale, StateStat.use_cost, v);
		public static StateCmd skill_cost(float   v) => new StateCmd(StatOp.scale, StateStat.skill_cost, v);
		public static StateCmd sticker_cost(float v) => new StateCmd(StatOp.scale, StateStat.sticker_cost, v);

		// Function
		// ----------------------------------------

		public static StateFunc points(Closure v) => new StateFunc(StatOp.scale, StateStat.points, v);
		public static StateFunc hp(Closure     v) => new StateFunc(StatOp.scale, StateStat.hp, v);
		public static StateFunc sp(Closure     v) => new StateFunc(StatOp.scale, StateStat.sp, v);
		public static StateFunc op(Closure     v) => new StateFunc(StatOp.scale, StateStat.op, v);

		public static StateFunc stats(Closure v) => new StateFunc(StatOp.scale, StateStat.stats, v);
		public static StateFunc power(Closure v) => new StateFunc(StatOp.scale, StateStat.power, v);
		public static StateFunc speed(Closure v) => new StateFunc(StatOp.scale, StateStat.speed, v);
		public static StateFunc will(Closure  v) => new StateFunc(StatOp.scale, StateStat.will, v);

		public static StateFunc res(Closure          v) => new StateFunc(StatOp.scale, StateStat.res, v);
		public static StateFunc res_blunt(Closure    v) => new StateFunc(StatOp.scale, StateStat.res_blunt, v);
		public static StateFunc res_slash(Closure    v) => new StateFunc(StatOp.scale, StateStat.res_slash, v);
		public static StateFunc res_pierce(Closure   v) => new StateFunc(StatOp.scale, StateStat.res_pierce, v);
		public static StateFunc res_gaia(Closure     v) => new StateFunc(StatOp.scale, StateStat.res_gaia, v);
		public static StateFunc res_oida(Closure     v) => new StateFunc(StatOp.scale, StateStat.res_oida, v);
		public static StateFunc res_astra(Closure    v) => new StateFunc(StatOp.scale, StateStat.res_astra, v);
		public static StateFunc res_physical(Closure v) => new StateFunc(StatOp.scale, StateStat.res_physical, v);
		public static StateFunc res_magical(Closure  v) => new StateFunc(StatOp.scale, StateStat.res_magical, v);

		public static StateFunc atk(Closure          v) => new StateFunc(StatOp.scale, StateStat.atk, v);
		public static StateFunc atk_blunt(Closure    v) => new StateFunc(StatOp.scale, StateStat.atk_blunt, v);
		public static StateFunc atk_slash(Closure    v) => new StateFunc(StatOp.scale, StateStat.atk_slash, v);
		public static StateFunc atk_pierce(Closure   v) => new StateFunc(StatOp.scale, StateStat.atk_pierce, v);
		public static StateFunc atk_gaia(Closure     v) => new StateFunc(StatOp.scale, StateStat.atk_gaia, v);
		public static StateFunc atk_oida(Closure     v) => new StateFunc(StatOp.scale, StateStat.atk_oida, v);
		public static StateFunc atk_astra(Closure    v) => new StateFunc(StatOp.scale, StateStat.atk_astra, v);
		public static StateFunc atk_physical(Closure v) => new StateFunc(StatOp.scale, StateStat.atk_physical, v);
		public static StateFunc atk_magical(Closure  v) => new StateFunc(StatOp.scale, StateStat.atk_magical, v);

		public static StateFunc def(Closure          v) => new StateFunc(StatOp.scale, StateStat.def, v);
		public static StateFunc def_blunt(Closure    v) => new StateFunc(StatOp.scale, StateStat.def_blunt, v);
		public static StateFunc def_slash(Closure    v) => new StateFunc(StatOp.scale, StateStat.def_slash, v);
		public static StateFunc def_pierce(Closure   v) => new StateFunc(StatOp.scale, StateStat.def_pierce, v);
		public static StateFunc def_gaia(Closure     v) => new StateFunc(StatOp.scale, StateStat.def_gaia, v);
		public static StateFunc def_oida(Closure     v) => new StateFunc(StatOp.scale, StateStat.def_oida, v);
		public static StateFunc def_astra(Closure    v) => new StateFunc(StatOp.scale, StateStat.def_astra, v);
		public static StateFunc def_physical(Closure v) => new StateFunc(StatOp.scale, StateStat.def_physical, v);
		public static StateFunc def_magical(Closure  v) => new StateFunc(StatOp.scale, StateStat.def_magical, v);

		public static StateFunc use_cost(Closure     v) => new StateFunc(StatOp.scale, StateStat.use_cost, v);
		public static StateFunc skill_cost(Closure   v) => new StateFunc(StatOp.scale, StateStat.skill_cost, v);
		public static StateFunc sticker_cost(Closure v) => new StateFunc(StatOp.scale, StateStat.sticker_cost, v);
	}
}
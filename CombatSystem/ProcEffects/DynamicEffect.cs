using Anjin.Scripting;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class DynamicEffect : ProcEffect
	{
		private Closure func;

		public DynamicEffect(Closure func)
		{
			this.func = func;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			DynValue   dv = Lua.Invoke(func, LuaUtil.Args(fighter));
			ProcEffect pe = LuaUtil.DynvalueToProcEffect(dv);

			return pe?.TryApplyFighter() ?? ProcEffectFlags.NoEffect;
		}
	}
}
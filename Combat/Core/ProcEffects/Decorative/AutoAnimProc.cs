using Anjin.Scripting;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class AutoAnimProc : ProcEffect
	{
		private readonly Closure _logic;

		public AutoAnimProc(Closure logic)
		{
			_logic = logic;
		}

		public override void BeforeApply()
		{
			Lua.Invoke(_logic, new object[] { ctx });
		}
	}
}
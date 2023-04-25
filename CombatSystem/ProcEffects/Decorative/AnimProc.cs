using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class AnimProc : ProcEffect
	{
		private readonly string  _id;
		private readonly Closure _closure;

		public AnimProc(Closure closure)
		{
			_closure = closure;
		}

		public AnimProc(string id, Closure closure)
		{
			_id      = id;
			_closure = closure;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			if (_id == null)
				ctx.anim(_closure);
			else
				ctx.anim(_id, _closure);

			return base.ApplyFighter();
		}
	}
}
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	[LuaUserdata]
	public class AddState : ProcEffect
	{
		public State   state;
		public Closure func;

		public override bool IsBuffing => true;

		public AddState([NotNull] State state)
		{
			this.state   = state;
			this.chance  = state.chance;
			state.Parent = this;
		}

		public AddState(Closure func)
		{
			this.func = func;
		}

		private void Init()
		{
			if (func != null)
				Lua.Invoke(func).AsObject(out state);

			state.ID      = state.ID ?? $"{proc.ID}/state";
			state.maxlife = Status.Life(state.maxlife);

			state.InitEnvFork(proc);
			state.SetDealer(dealer);
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			Init();

			battle.AddState(fighter, state);
			return ProcEffectFlags.VictimEffect;
		}

		protected override ProcEffectFlags ApplySlot()
		{
			Init();

			battle.AddState(slot, state);
			return ProcEffectFlags.VictimEffect;
		}
	}
}
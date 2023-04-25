using Anjin.Scripting;
using Combat.Toolkit;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class SwapHome : ProcEffect
	{
		public Slot         goal;
		public Closure      selector;
		public MoveSemantic semantic;
		public SwapHome(Slot goal, MoveSemantic semantic = MoveSemantic.Auto)
		{
			this.goal = goal;
			this.semantic = semantic;
		}

		public SwapHome(Closure selector, MoveSemantic semantic = MoveSemantic.Auto)
		{
			this.selector = selector;
			this.semantic = semantic;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			Slot slot = this.goal;

			if (selector != null)
			{
				DynValue dv = Lua.Invoke(selector, new object[] { fighter.home, fighter });
				if (dv.Type == DataType.UserData)
				{
					slot = dv.UserData.Object as Slot ?? (dv.UserData.Object as Fighter)?.home;
				}
			}

			if (slot == null)
			{
				slot = fighter.home;
			}

			Battle.SlotSwap swap = battle.SwapHome(fighter, slot, semantic);
			ctx.resultingFormationSwaps.Add(swap);
			return ProcEffectFlags.VictimEffect;
		}

	}
}
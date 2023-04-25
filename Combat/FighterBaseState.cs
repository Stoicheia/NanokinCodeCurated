using System;
using Anjin.Scripting;
using MoonSharp.Interpreter;

namespace Combat
{
	[Serializable]
	public struct FighterBaseState
	{
		public int  set_hurt;
		public int  set_heal;
		public bool invincible;

		public static FighterBaseState Default => new FighterBaseState
		{
			set_hurt   = -1,
			set_heal   = -1,
			invincible = false
		};

		public void ConfigureTB(Table tb)
		{
			if (tb.TryGet("invincible", out bool invincible))
			{
				this.invincible = invincible;
			}

			set_heal = tb.TryGet("heal", set_heal);
			set_hurt = tb.TryGet("damage", set_hurt);
		}
	}
}
using System.Collections.Generic;

namespace Combat
{
	public class BaseMechanicPlugin : LuaPlugin
	{
		public BaseMechanicPlugin()
		{
			ScriptNames = new List<string>();
		}

		public override void Register(BattleRunner runner, Battle battle)
		{
			ScriptNames = ScriptNames ?? new List<string>();
			ScriptNames.Clear();

			if (GameOptions.current.combat_base_mechanics)
			{
				ScriptNames.Add("regen-op-every-turn");
				ScriptNames.Add("regen-sp-every-round");
				ScriptNames.Add("regen-op-on-cast");
				ScriptNames.Add("regen-sp-on-hold");
				ScriptNames.Add("grid-column-effects");
			}

			base.Register(runner, battle);
		}
	}
}
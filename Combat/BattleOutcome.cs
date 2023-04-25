using Anjin.Scripting;

namespace Combat.Startup
{
	[LuaEnum]
	public enum BattleOutcome
	{
		None,
		Win,
		Flee,
		Lose,
		LoseExit,
		LoseRetry
	}
}
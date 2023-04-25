namespace Combat
{
	public enum CoreOpcode
	{
		None,

		// Special
		Stop,
		Restart,

		// Core
		Wait,
		Execute,
		Emit,
		AwaitExecution,

		// BATTLE FLOW
		// ----------------------------------------
		IntroduceBattle,
		IntroduceTurns,
		PreStart,
		StartBattle,
		StopBattle,

		// TURN FLOW
		// ----------------------------------------
		StartRound,
		StartTurn,
		StartAct,
		ActTurn,
		ActTurnBrain,
		EndTurn,
		StepTurn,

		// BATTLING MECHANICS
		// ----------------------------------------

		/// <summary>
		/// In this instruction, we process all deaths that have occured since the last
		/// occurence. We don't process deaths immediately when they happen since
		/// we can't really animate deaths in the middle of a skill.
		/// </summary>
		FlushDeaths,
		FlushRevives,
		LoseBattle,
		WinBattle,
	}
}
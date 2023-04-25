namespace Combat.Components
{
	/// <summary>
	/// Implements the win handler by simply exiting the battle.
	/// </summary>
	public class ExitWinHandler : Chip
	{
		protected override void RegisterHandlers()
		{
			Handle(CoreOpcode.WinBattle, HandleWin);
		}

		private void HandleWin(ref CoreInstruction msg)
		{
			runner.Stop();
		}
	}
}
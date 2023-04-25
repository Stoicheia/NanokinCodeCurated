using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	public class WinCommand : TurnCommand
	{
		[NotNull]
		public override string Text => "Win";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new InsertInstructionAnim(CoreOpcode.WinBattle);
	}
}
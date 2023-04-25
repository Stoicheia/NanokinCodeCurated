using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	public class LoseCommand : TurnCommand
	{
		[NotNull]
		public override string Text => "Lose";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new InsertInstructionAnim(CoreOpcode.LoseBattle);
	}
}
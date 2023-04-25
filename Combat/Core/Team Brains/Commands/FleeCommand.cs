using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	public class FleeCommand : TurnCommand
	{
		[NotNull]
		public override string Text => "Flee";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new FleeAnim();
	}
}
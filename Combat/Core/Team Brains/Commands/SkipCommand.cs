using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	/// <summary>
	/// A command for debugging. Allows the player to skip their action.
	/// </summary>
	public class SkipCommand : TurnCommand
	{
		[NotNull]
		public override string Text => "Skip";

		[CanBeNull] public override BattleAnim GetAction(Battle battle) => null;
	}
}
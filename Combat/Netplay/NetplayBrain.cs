using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	public class NetplayBrain : BattleBrain
	{
		[CanBeNull] public override BattleAnim OnGrantAction() => null;
	}
}
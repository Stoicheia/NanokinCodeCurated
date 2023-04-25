using System;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;

namespace Combat
{
	/// <summary>
	/// A team brain which ends its action by doing nothing.
	/// </summary>
	[Serializable]
	public class SkipTurnBrain : BattleBrain
	{
		public bool Hangs;
		//public bool NoTurns;

		public override async UniTask<BattleAnim> OnGrantActionAsync()
		{
			await UniTask.WaitUntil(() => !Hangs);
			return null;
		}
	}
}
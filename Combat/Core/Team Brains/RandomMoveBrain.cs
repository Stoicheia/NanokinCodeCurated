using System;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;

namespace Combat
{
	[Serializable]
	public class RandomMoveBrain : BattleBrain
	{
		public override async UniTask<BattleAnim> OnGrantActionAsync()
		{
			return MoveToRandomSlot(fighter);
		}
	}
}
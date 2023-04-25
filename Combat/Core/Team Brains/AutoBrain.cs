using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;

namespace Combat
{
	/// <summary>
	/// Automatically use the default nanokin AI.
	/// </summary>
	[LuaUserdata]
	[Serializable]
	public class AutoBrain : BattleBrain
	{
		private Dictionary<Fighter, UtilityAIBrain> _brains = new Dictionary<Fighter, UtilityAIBrain>();

		public override BattleAnim OnGrantAction()
		{
			if (!_brains.TryGetValue(fighter, out UtilityAIBrain brain))
			{
				_brains[fighter] = brain = new UtilityAIBrain(fighter.info.DefaultAI);
				battle.RegisterBrain(brain);
			}


			brain.fighter = fighter;
			brain.battle  = battle;

			return brain.OnGrantAction();
		}

		public override UniTask<BattleAnim> OnGrantActionAsync()
		{
			if (!_brains.TryGetValue(fighter, out UtilityAIBrain brain))
			{
				_brains[fighter] = brain = new UtilityAIBrain(fighter.info.DefaultAI);
				battle.RegisterBrain(brain);
			}

			brain.fighter = fighter;
			brain.battle  = battle;

			return brain.OnGrantActionAsync();
		}
	}
}
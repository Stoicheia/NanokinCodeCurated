using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.Data;
using Combat.Toolkit;
using UnityEngine;

namespace Combat
{
	/// <summary>
	/// A team brain which picks skills at random.
	/// </summary>
	[Serializable]
	public class RandomBrain : BattleBrain
	{
		public override BattleAnim OnGrantAction()
		{
			var options = new List<SkillOption>();

			foreach (BattleSkill skill in fighter.skills)
			{
				if (skill.IsPassive)
					continue;

				options.AddRange(GetOptions(fighter, skill));
			}

			if (options.Count == 0)
				Debug.LogWarning($"[Brain] No skill options available for {fighter}!");


			if (options.Count > 0)
			{
				SkillOption opt = options.Choose();

				var targeting = new Targeting();
				targeting.AddPick(opt.target);

				BattleSkill skill = battle.GetSkillOrRegister(fighter, opt.skill);
				return new SkillCommand(fighter, skill, targeting).GetAction(battle);
			}
			else
			{
				if (RNG.Chance(0.075f)) // small chance to simply skip action
					return new SkipCommand().GetAction(battle);
				else
					return MoveToRandomSlot(fighter);
			}
		}
	}
}
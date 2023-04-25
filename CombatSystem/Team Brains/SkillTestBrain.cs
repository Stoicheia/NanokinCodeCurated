using System;
using Anjin.Util;
using Combat.Data;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;

namespace Combat
{
	[Serializable]
	public class SkillTestBrain : BattleBrain
	{
		public SkillAsset Skill;

		public SkillTestBrain() { }

		public SkillTestBrain(SkillAsset skill)
		{
			Skill = skill;
		}

		public override async UniTask<BattleAnim> OnGrantActionAsync()
		{
			BattleSkill skill = battle.GetSkillOrRegister(fighter, Skill);

			return new UseSkillAtRandomAnim(fighter, skill);
		}
	}
}
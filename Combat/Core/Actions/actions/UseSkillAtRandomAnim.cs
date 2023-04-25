using Combat.Data;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Toolkit
{
	public class UseSkillAtRandomAnim : BattleAnim
	{
		public BattleSkill skill;

		/// <summary>
		/// Whether or not we should run OnPassive if the skill is a passive.
		/// Normally, OnPassive should run for all passives at the start of
		/// combat.
		/// </summary>
		public bool applyPassive = false;

		public UseSkillAtRandomAnim([CanBeNull] Fighter fighter, BattleSkill skill) : base(fighter)
		{
			this.skill = skill;
		}

		public override void RunInstant()
		{
			BattleAnim anim = ResolveAction();
			if (anim != null)
				RunInstant(anim);
		}

		public override async UniTask RunAnimated()
		{
			BattleAnim anim = ResolveAction();
			if (anim != null)
				await RunAnimated(anim);
		}

		[CanBeNull]
		protected BattleAnim ResolveAction()
		{
			if (skill == null)
			{
				DebugLogger.LogWarning("Cannot use null skill, you must set a skill. Skipping action...", LogContext.Combat, LogPriority.High);
				return null;
			}

			var cannotmsg = $"{fighter} cannot use {skill.asset.name}";

			if (skill.IsPassive)
			{
				if (applyPassive)
					return skill.Passive();

				DebugLogger.LogWarning($"{cannotmsg} because it's a passive.", LogContext.Combat, LogPriority.Low);
				return null;
			}

			if (!skill.Usable().Item1)
			{
				DebugLogger.LogWarning($"{cannotmsg} because it has unmet criteria.", LogContext.Combat, LogPriority.Low);
				return null;
			}

			int skillcost = skill.Cost();
			if (skillcost > fighter.points.sp)
			{
				DebugLogger.LogWarning($"{cannotmsg} because not enough SP! (sp={fighter.points.sp}, cost={skillcost}))", LogContext.Combat, LogPriority.Low);
				return null;
			}

			var targeting = new Targeting();

			battle.GetSkillTargets(skill, targeting);

			if (targeting.options.Count == 0)
			{
				DebugLogger.LogWarning($"{cannotmsg} because No targeters available for skill '{skill.asset.name}'.", skill.asset, LogContext.Combat, LogPriority.Low);
				return null;
			}

			if (targeting.options.Count >= 2)
			{
				// Multi-targeting skills are unsupported, it will probably be unnecessary
				DebugLogger.LogWarning($"{cannotmsg} because multi targeting is not yet supported by the skill tester.", LogContext.Combat, LogPriority.Low);
				return null;
			}

			targeting.PickRandomly($"{cannotmsg} because one of the group has no value target.");


			return new SkillAnim(fighter, skill, targeting);
		}

		public override string ToString() => $"{nameof(UseSkillAtRandomAnim)}({skill.asset.name})";
	}
}
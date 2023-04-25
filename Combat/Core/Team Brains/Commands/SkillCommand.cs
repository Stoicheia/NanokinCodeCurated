using System.Collections.Generic;
using Combat.Data;
using Combat.Toolkit;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;
using UnityEngine;

namespace Combat
{
	public class SkillCommand : TurnCommand
	{
		private readonly Fighter _user;

		public SkillCommand(Fighter user, BattleSkill skill, Targeting target)
		{
			_user  = user;
			Skill  = skill;
			Target = target;
		}

		public SkillCommand([NotNull] PacketReader pr, [NotNull] Battle battle)
		{
			_user = (Fighter) battle.ActiveActer;
			string skillGUID = pr.String();

			var  targetPickIndices = new List<int>();
			byte nTargetPicks      = pr.Byte();
			for (var i = 0; i < nTargetPicks; i++)
			{
				byte idxTarget = pr.Byte();
				targetPickIndices.Add(idxTarget);
			}

			Target = new Targeting();

			// SkillCatalogue.Instance.LoadAssetAsync(skillGUID).ContinueWith(task =>
			// {
			// 	TargetSelectionPicker picker = new TargetSelectionPicker(battle)
			// 	{
			// 		source = _user
			// 	};
			//
			// 	battle.GetSkillInstance(_user, task.Result).OnTarget(Target);
			//
			// 	for (int i = 0; i < Target.targeters.Count; i++)
			// 	{
			// 		Targeter provider = Target.targeters[i];
			//
			// 		picker.ChangeTargeter(provider);
			//
			// 		int    idxPick = targetPickIndices[i];
			// 		Target target  = picker.AvailableTargets[idxPick];
			//
			// 		Target.AddPick(target);
			// 	}
			// });
		}

		public BattleSkill Skill { get; }

		public Targeting Target { get; }

		public override string Text => (!Skill.asset.CustomDisplayName ? Skill.asset.DisplayName : Skill.DisplayName());

		[NotNull] public override BattleAnim GetAction(Battle battle) => new SkillAnim(_user, Skill, Target);

		public override void WritePacket([NotNull] PacketWriter pw)
		{
			DebugLogger.Log(Skill.ToString(), LogContext.Combat, LogPriority.Low);
			DebugLogger.Log(Target.ToString(), LogContext.Combat, LogPriority.Low);
			DebugLogger.Log(Target.picks.ToString(), LogContext.Combat, LogPriority.Low);
			DebugLogger.Log(Skill.Address, LogContext.Combat, LogPriority.Low);

			pw.Byte();
			pw.String(Skill.Address);

			pw.Byte((byte) Target.picks.Count);
			foreach (Target target in Target.picks)
			{
				DebugLogger.Log(target.ToString(), LogContext.Combat, LogPriority.Low);
				pw.Byte((byte) target.index);
			}
		}
	}
}
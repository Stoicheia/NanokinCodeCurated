using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anjin.Actors;
using Anjin.Util;
using Combat.Data;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat
{
	[Serializable]
	public abstract class BattleBrain
	{
		protected bool         isActiveTurn;
		protected FighterActor virtualFighter;

		[NonSerialized] public Team        team;
		[NonSerialized] public Fighter     fighter;
		[NonSerialized] public Battle battle;

		protected FighterActor CastingActor => virtualFighter == null ? fighter.actor : virtualFighter;

		[NonSerialized]
		public CancellationTokenSource cts;

		public BattleRunner Runner => battle.runner;

		public async UniTask<BattleAnim> GrantAction()
		{
			if (fighter.deathMarked && !(this is PlayerBrain))
				return new HoldAnim();

			isActiveTurn = true;
			BattleAnim sync = OnGrantAction();
			if (sync != null)
				return sync;

			BattleAnim ret = await OnGrantActionAsync();

			isActiveTurn = false;
			return ret;
		}

		public virtual void OnRegistered() { }

		[CanBeNull] public virtual BattleAnim OnGrantAction() => null;

		public virtual UniTask<BattleAnim> OnGrantActionAsync() => UniTask.FromResult<BattleAnim>(null);

		public virtual void Cleanup()
		{
			team = null;
		}

		public virtual void Update() { }

		// UTILITIES
		// ------------------------------------------------------------

		[CanBeNull]
		public BattleAnim MoveToRandomSlot([NotNull] Fighter fighter)
		{
			var targeting = battle.GetFormationTargets(fighter, new Targeting());

			List<Target> targets = targeting.options[0];

			return targets.Count > 0
				? new MoveAnim(fighter, targets.Choose()?.Slot)
				: null;
		}

		[ItemNotNull]
		protected IEnumerable<SkillOption> GetOptions(Fighter user, [NotNull] BattleSkill skill, bool multiTarget = false)
		{
			var targeting = new Targeting();
			battle.GetSkillTargets(skill, targeting);

			// Multi-targeting skills are unsupported yet
			if (targeting.options.Count > 1 && !multiTarget)
			{
				Debug.LogWarning("[Brain] Multi-target skills are not yet supported for AI.", skill.asset);
				yield break;
			}

			if (targeting.options.Count == 0)
			{
				Debug.LogWarning($"[Brain] No targets available for skill '{skill.asset.name}'. Skipping...", skill.asset);
				yield break;
			}

			foreach (SkillOption option in targeting.options[0].Select(target => new SkillOption(skill.asset, target)))
			{
				yield return option;
			}
		}
	}

	public readonly struct SkillOption
	{
		public readonly SkillAsset skill;
		public readonly Target     target;

		public SkillOption(SkillAsset skill, Target target)
		{
			this.skill  = skill;
			this.target = target;
		}
	}
}
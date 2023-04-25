using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Anjin.Utils;
using Combat.Data;
using Combat.Skills.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using Pathfinding.Util;
using UnityEngine;

namespace Combat.Toolkit
{
	public static class AnimFlags
	{
		public const string Overdrive = "overdrive";
	}

	/// <summary>
	/// An action in battle that can execute instantaneously or with animation.
	/// Usually, BattleActions are executed in sequence, not simultaneously.
	/// Implement ExecuteInstant, with either ExecuteAnimated or OnStart/OnStop + Animationactive.
	/// </summary>
	public class BattleAnim
	{
		/// <summary>
		/// A title that can be used for a UI to announce what is happening.
		/// </summary>
		public string title;

		/// <summary>
		/// BattleCore we are executing from.
		/// </summary>
		public BattleRunner runner;

		/// <summary>
		/// Battle that we are a part of.
		/// </summary>
		public Battle battle;

		/// <summary>
		/// Fighter that is executing this action.
		/// </summary>
		[CanBeNull]
		public Fighter fighter;

		/// <summary>
		/// Flags to modify the animation.
		/// </summary>
		public List<string> animflags;

		/// <summary>
		/// Flags to skip parts of the animation.
		/// </summary>
		public List<string> skipflags;

		/// <summary>
		/// Cancellation token for the async execution of this animation.
		/// </summary>
		public CancellationTokenSource cts;

		/// <summary>
		/// If the action is canceled through cts, whether or not to end gracefully.
		/// </summary>
		public bool gracefulCancelation;

		public Targeting targeting = new Targeting();

		public UseInfo useInfo = new UseInfo();

		public readonly List<Proc>    procs    = new List<Proc>();
		public readonly List<Trigger> triggers = new List<Trigger>();
		public readonly List<Fighter> fighters = new List<Fighter>();

		public virtual bool Skippable => false;

		public virtual bool Halts => true;

		protected virtual bool AnimationActive => false;

		private List<BattleAnim> _children;

		public BattleAnim(Fighter fighter = null)
		{
			this.fighter = fighter;
		}

		public void CopyContext([NotNull] BattleAnim other)
		{
			runner    = other.runner;
			battle    = other.battle;
			animflags = other.animflags;
			skipflags = other.skipflags;
			cts       = other.cts;

			if (fighter == null)
				fighter = other.fighter;
		}

		public virtual void RunInstant()
		{
			// BUG this could be buggy, this was in InstantAction before
			foreach (Proc proc in procs) battle.Proc(proc);
			foreach (Trigger trigger in triggers) battle.AddTrigger(trigger);
			foreach (Fighter fighter in fighters) battle.AddFighter(fighter);

			// Spawn the fighters
			// This could potentially be moved elsewhere, but this is the only place that does this..
			if (battle.animated)
			{
				foreach (Fighter fter in fighters)
				{
					if (fter.actor == null)
					{
						GameObject o = Object.Instantiate(fter.prefab);
						fter.set_actor(o);
						if (o.TryGetComponent(out FxInfo fxi)) fxi.enabled        = false;
						if (o.TryGetComponent(out MotionBehaviour mb)) mb.enabled = false;

						fter.snap_home();
					}
				}
			}
		}

		public virtual UniTask RunAnimated()
		{
			RunInstant();
			return UniTask.CompletedTask;
		}

		protected void RunInstant([NotNull] BattleAnim child, bool prepare = true)
		{
			if (prepare)
				child.CopyContext(this);

			child.RunInstant();
		}

		/// <summary>
		/// Run an action as a child of this action.
		/// </summary>
		/// <param name="child"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <param name="copyParams"></param>
		protected async UniTask RunAnimated([NotNull] BattleAnim child, bool copyParams = true)
		{
			if (copyParams)
				child.CopyContext(this);

			if (_children == null)
				_children = ListPool<BattleAnim>.Claim(4);

			_children.Add(child);
			await child.RunAnimated();
			_children.Remove(child);

			if (_children.Count == 0)
				ListPool<BattleAnim>.Release(ref _children);
		}

		protected UniTask RunAnimated([NotNull] string skill, Battle b, Targeting t, bool copyParams = true)
		{
			var skillAction = new SkillAnim(fighter, GameAssets.GetSkill(skill), b, t);
			skillAction.CopyContext(this);
			skillAction.fighter = fighter;
			return RunAnimated(skillAction, copyParams);
		}

		public virtual void Update()
		{
			if (_children != null)
			{
				for (var i = 0; i < _children.Count; i++)
				{
					BattleAnim anim = _children[i];
					anim.Update();
				}
			}
		}

		public override string ToString() => GetType().Name;

		public void AddAnimFlags(IList<string> flags)
		{
			if (animflags == null)
				animflags = new List<string>(flags);
			else
				animflags.AddRange(flags);
		}

		public void AddSkipFlags(IList<string> flags)
		{
			if (skipflags == null)
				skipflags = new List<string>(flags);
			else
				skipflags.AddRange(flags);
		}
	}
}
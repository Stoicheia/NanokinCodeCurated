using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Util;
using Assets.Nanokins;
using Data.Nanokin;
using MoonSharp.Interpreter;
using Util.Collections;

namespace Combat.Data
{
	/// <summary>
	/// Dynamic operation on a stat, generally a transient one.
	/// </summary>
	[LuaUserdata]
	public struct StateFunc
	{
		public StatOp       op;
		public StateStat    stat;
		public float        value;
		public float        chance;
		public Fighter      fighter;
		public Slot         slot;
		public string       tag;
		public List<string> tags;
		public LimbType     limb;
		public BattleBrain  brain;
		public Closure      closure;

	#region Constructors

		public StateFunc(StatOp op, StateStat stat, float value = 0) : this()
		{
			this.op   = op;
			this.stat = stat;
		}

		public StateFunc(StatOp op, StateStat stat, Fighter fighter) : this()
		{
			this.stat    = stat;
			this.op      = op;
			this.fighter = fighter;
		}

		public StateFunc(StatOp op, StateStat stat, Slot slot) : this()
		{
			this.stat = stat;
			this.op   = op;
			this.slot = slot;
		}

		public StateFunc(StatOp op, StateStat stat, string tag) : this()
		{
			this.op   = op;
			this.stat = stat;
			this.tag  = tag;
		}

		public StateFunc(StatOp op, StateStat stat, List<string> tags) : this()
		{
			this.op   = op;
			this.stat = stat;
			this.tags = tags;
		}

		public StateFunc(StatOp op, StateStat stat, LimbType limb) : this()
		{
			this.op   = op;
			this.stat = stat;
			this.limb = limb;
		}

		public StateFunc(StatOp op, StateStat stat, Closure closure) : this()
		{
			this.op      = op;
			this.stat    = stat;
			this.closure = closure;
		}

		public StateFunc(StatOp set, StateStat stat, BattleBrain brain) : this()
		{
			this.op    = set;
			this.stat  = stat;
			this.brain = brain;
		}

	#endregion

	#region Matching

		public bool CheckUse(UseInfo info)
		{
			switch (stat)
			{
				case StateStat.skill_usable:
				case StateStat.skill_cost:
				case StateStat.skill_target_picks:
				case StateStat.skill_target_options:
					return (info.type & UseType.Skill) == UseType.Skill;

				case StateStat.sticker_usable:
				case StateStat.sticker_cost:
				case StateStat.sticker_target_picks:
				case StateStat.sticker_target_options:
					return (info.type & UseType.Sticker) == UseType.Sticker;
			}

			return true;
		}

		// public bool Matches(Targeting target, bool @default = false)
		// {
		// 	if (target.fighters.Contains(fighter)) return true;
		// 	if (target.slots.Contains(slot)) return true;
		//
		// 	return @default;
		// }
		//

		public bool Matches(Target target, bool @default = false)
		{
			if (target.fighters.Contains(fighter)) return true;
			if (target.slots.Contains(slot)) return true;

			return @default;
		}

		/// <summary>
		/// Check if the StatFunc applies to this skill
		/// - (optional) Skill has tag (tag field)
		/// - (optional) Skill has tags (tags field)
		/// - (optional) Skill has limb tag (limb field)
		/// </summary>
		/// <param name="skill"></param>
		/// <param name="default"></param>
		/// <returns></returns>
		public bool Matches(BattleSkill skill, bool @default = false)
		{
			if (tags != null)
			{
				foreach (var tag in tags)
				{
					if (skill.HasTag(tag)) // Matches
					{
						return true;
					}
				}
			}

			if (tag != null)
			{
				if (skill.HasTag(tag))
					return true;
			}

			if (limb != LimbType.None)
			{
				var asset = skill.limb as NanokinLimbAsset;
				if (asset != null && asset.Kind == limb) return true;
				if (skill.HasTag(limb.ToString())) return true;
			}

			return false;
		}

	#endregion

		private static WeightMap<Target> _targetmap = new WeightMap<Target>();


		/// <summary>
		/// Closure(original_target, option) is invoked
		/// to get a weight for each option. If the closure is null, it's uniform.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="uinfo"></param>
		/// <param name="targeting"></param>
		public void ModifyTargetOptions(Fighter user, UseInfo uinfo, Targeting targeting)
		{
			if (op == StatOp.randomize)
			{
				if (!CheckUse(uinfo)) return;
				if (!stat.IsTargetOptions()) return;
				if (!RNG.Chance(chance)) return;

				for (var i = 0; i < targeting.picks.Count; i++)
				{
					foreach (Target t in targeting.options[i])
						_targetmap.Add(t, closure != null
							? (float)Lua.Invoke(closure, new object[] { t }).Number
							: 1);

					targeting.picks[i] = _targetmap.Choose();
					_targetmap.Clear();
				}
			}
		}

		public void ModifyTargetPicks(Fighter user, UseInfo uinfo, Targeting targeting)
		{
			if (op == StatOp.randomize)
			{
				if (!CheckUse(uinfo)) return;
				if (!stat.IsTargetOptions()) return;
				if (!RNG.Chance(chance)) return;

				for (var i = 0; i < targeting.picks.Count; i++)
				{
					foreach (Target t in targeting.options[i])
					{
						_targetmap.Add(t, closure != null
							? (float)Lua.Invoke(closure, new object[] { targeting.picks[i], t }).Number
							: 1);
					}

					targeting.picks[i] = _targetmap.Choose();
					_targetmap.Clear();
				}
			}
		}

		public void ModifyCost(UseInfo info, ref float cost) { }

		public bool IsSkillForbidden(BattleSkill skill)
		{
			if (op == StatOp.forbid) return !Matches(skill);
			if (op == StatOp.restrict) return Matches(skill);

			return false;
		}

		public bool IsTargetForbidden(UseInfo info, Target target)
		{
			if (!CheckUse(info)) return false;
			if (!Matches(target)) return false;

			if (op == StatOp.forbid) return Matches(target);
			if (op == StatOp.restrict) return !Matches(target);

			return false;
		}

		public bool RandomizeTarget(UseInfo useInfo, Targeting targeting)
		{
			if (!CheckUse(useInfo)) return false;

			for (var i = 0; i < targeting.options.Count; i++)
			{
				List<Target> options = targeting.options[i];
				targeting.picks.Add(options.Choose());
			}

			return true;
		}

		public BattleBrain ModifyBrain(BattleBrain original)
		{
			if (op == StatOp.set && stat == StateStat.brain)
				return brain;
			return original;
		}
	}
}
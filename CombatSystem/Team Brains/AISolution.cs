using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Data;
using JetBrains.Annotations;

namespace Combat
{
	public struct AISolution : IComparable<AISolution>, IEquatable<AISolution>
	{
		public AIAction    action;
		public BattleSkill skill;
		public Target      target;
		public AITag tag;
		public float       effort;

		public AISolution(AIAction action) : this()
		{
			this.action = action;
		}

		public AISolution(AIAction action, Target target) : this()
		{
			this.action = action;
			this.target = target;
		}

		public AISolution(BattleSkill skill, Target target) : this()
		{
			this.skill  = skill;
			this.target = target;
			tag         = AITag.none;
			action      = AIAction.skill;
		}

		public int CompareTo(AISolution other) => -effort.CompareTo(other.effort);

		public bool Equals(AISolution other) => Equals(skill, other.skill) && Equals(target, other.target) && tag == other.tag && effort.Equals(other.effort);

		public override bool Equals(object obj) => obj is AISolution other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = skill != null ? skill.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (target != null ? target.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int) tag;
				hashCode = (hashCode * 397) ^ effort.GetHashCode();
				return hashCode;
			}
		}

		[NotNull]
		public static List<float> GetWeights([NotNull] List<AISolution> solutions)
		{
			return solutions.Select(x => x.effort).ToList();
		}
	}
}
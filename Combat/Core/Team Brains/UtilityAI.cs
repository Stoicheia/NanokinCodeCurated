using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using UnityEngine;
using Util;
using static Combat.AITag;
using static Combat.UtilityAI.NeedGoal;
using static Combat.UtilityAI.AIProp;

namespace Combat
{
	/// <summary>
	/// Core for the battle AI.
	/// - Needs instill a sense of purpose and identity to the AI.
	/// - Solutions announce to the AI everything it can do.
	/// </summary>
	[LuaUserdata]
	public class UtilityAI
	{
		// IDEA maintain a revenge energy for monsters that just damaged us, decay the energy each action
		// IDEA build probabilistic zones (slots, health, etc. that modulate scoring by deciding how likely we are to lose a portion of HP, gain a portion, etc.)
		// IDEA Add oscillation clocks that can modulate preferences at a high-level. (e.g. naturally oscillate between a preference for burst damage and a preference for stockpiling power/healing/regaining sp)
		// IDEA Keep a store of fighters inflicting procs that have moved us really far away from our goals, and create a need around it. (e.g. I have a really high need for blood, and one fighter just lead to an enemy regaining 50% of its health. Fuck that guy, right) Increase permanency of the memory the further away we were moved from our goal (Small heal = I'll hit you back, but maybe I'll forgive you // Big heal = I'll remember that)
		// IDEA Train a NN with a text encoder to guess danger and safety zones from skill descriptions. (lol)
		// TODO pool needs

		public Battle battle;
		public Fighter     self;

		/// <summary>
		/// Number of sedentarism units to add when we we do not move.
		/// </summary>
		public float sedentaryGain = 0.15f;

		protected float _sedentarism;

		/// <summary>
		/// Weighted random factors on solutions are raised to the power of this number. Lower = more random, higher = more precise.
		/// </summary>
		public float intelligence = 0.5f;

		public List<Need>         needs       = new List<Need>(32);
		public List<Preference>[] needPrefs   = new List<Preference>[32];
		public List<Preference>   globalPrefs = new List<Preference>(32);
		public List<AISolution>   solutions   = new List<AISolution>(512);

		protected bool _hasMove; // Dynamically refreshed in OnSolutionsChanged

		protected readonly Targeting _targeting = new Targeting();

		public UtilityAI() { }

		public UtilityAI([NotNull] Fighter self)
		{
			this.self = self;
			battle    = self.battle;
		}

		public void Reset()
		{
			_targeting.Clear();

			for (var i = 0; i < needs.Count; i++)
				needPrefs[i].Clear();
			globalPrefs.Clear();
			needs.Clear();
			solutions.Clear();
		}

		public void OnSolutionChosen(AISolution sol)
		{
			if (sol.tag == move)
			{
				_sedentarism = 0;
			}
			else
			{
				_sedentarism += sedentaryGain;
			}
		}

		// public void OnTurnStart() { }
		// public void OnTurnEnd() { }
		// public void OnSlotChange()
		// {
		// 	_sedentarism = 0;
		// }

	#region Pre-planning

		[NotNull]
		public NeedEditor AddNeed(Need need)
		{
			int index = needs.Count;
			needs.Add(need);

			if (needPrefs.Length < needs.Count)
				Array.Resize(ref needPrefs, needs.Count);
			needPrefs[index] = needPrefs[index] ?? new List<Preference>();

			NeedEditor editor = NeedEditor.instance;
			editor.ai   = this;
			editor.need = index;
			return editor;
		}

		public void AddSolution(AISolution sol)
		{
			solutions.Add(sol);
		}

		public void AddPref(int i, Preference pref)
		{
			needPrefs[i].Add(pref);
		}

		public void AddPref(Preference pref)
		{
			globalPrefs.Add(pref);
		}

		/// <summary>
		/// Automatically sample all our options and fill the buffers with them.
		/// </summary>
		public virtual void PopulateSolutions()
		{
			// TODO sample more or less of certain options depending on the needs.

			// Skill solutions
			// ----------------------------------------
			foreach (BattleSkill inst in self.skills)
			{
				if (inst == null) continue;
				if (!battle.CanUse(inst).Item1) continue;

				battle.GetSkillTargets(inst, _targeting);
				_targeting.CopyPicksToOptions();

				if (_targeting.options.Count == 0) continue;

				List<Target> targets = _targeting.options[0];
				if (targets.Count == 0) continue;

				foreach (Target target in targets)
				{
					var sol = new AISolution(inst, target);

					if (inst.HasTag("attack")) sol.tag = attack;
					if (inst.HasTag("heal")) sol.tag   = heal;
					if (inst.HasTag("state")) sol.tag   = buff;
					if (inst.HasTag("debuff")) sol.tag = debuff;

					solutions.Add(sol);
				}
			}

			// Formation solutions
			// ----------------------------------------
			battle.GetFormationTargets(self, _targeting);

			foreach (Target target in _targeting.options[0])
			{
				solutions.Add(new AISolution(AIAction.move, target));
			}

			// Hold solution
			// ----------------------------------------

			solutions.Add(new AISolution(AIAction.hold, new Target(self)));

			OnSolutionsChanged();
		}

		/// <summary>
		/// Go over all the solutions and collect some data.
		/// </summary>
		public void OnSolutionsChanged()
		{
			_hasMove = false;

			for (var i = 0; i < solutions.Count; i++)
			{
				AISolution sol = solutions[i];
				_hasMove = _hasMove || sol.tag == move;
			}
		}

	#endregion

	#region Decision

		/// <summary>
		/// Decide the best solution based on our needs at this time.
		/// </summary>
		/// <returns></returns>
		public AISolution Decide()
		{
			NormalizeNeeds();
			EvaluateEfforts();

			return WeightedRandom<AISolution>.Choose(solutions, AISolution.GetWeights(solutions), intelligence, 0.05f);

			solutions.Sort();

			return solutions[0];
		}

		public void NormalizeNeeds()
		{
			float total = 0;

			for (var i = 0; i < needs.Count; i++)
				total += needs[i].weight;

			for (var i = 0; i < needs.Count; i++)
			{
				Need need = needs[i];
				need.weight_normalized = need.weight / total;
				needs[i]               = need;
			}
		}

		/// <summary>
		/// Evaluate effort for every solution.
		/// </summary>
		private void EvaluateEfforts()
		{
			for (var i = 0; i < solutions.Count; i++)
			{
				AISolution sol = solutions[i];
				sol.effort   = GetEffort(ref sol);
				solutions[i] = sol;
			}
		}

		/// <summary>
		/// Get the effort for a solution.
		/// The more effort a solution has, the more likely it is to be picked. (relative to other solutions)
		/// </summary>
		private float GetEffort(ref AISolution sol)
		{
			float total = 0;

			// Find the highest
			// ----------------------------------------

			for (var i = 0; i < needs.Count; i++)
			{
				Need             need  = needs[i];
				List<Preference> prefs = needPrefs[i];

				float effort = 0;

				foreach (ITargetable target in sol.target.all)
				{
					if (!IsApplicable(target, ref need))
						continue;

					AIValue  value = default;
					AIProp   prop  = need.prop;
					NeedGoal goal  = need.goal;

					GetTargetProperty(prop, target, ref value);

					// Intrinsic accomplishment of the need's goal by the solution
					// ------------------------------------------------------------
					float score = 0f;

					// This is an area we need to work on in the future,
					// need more information about the solution to make
					// good calculations

					// TODO predict damage from power
					// TODO increase movement score when no targets and goal reduce hp

					if (goal == reduce && prop == hp && sol.tag == attack) score                    = 1;
					if (goal == change && prop == hp && sol.tag == attack) score                    = 1;
					if (goal == change && prop == hp && sol.tag == heal) score                      = 1;
					if (goal == raise && prop == hp && sol.tag == heal) score                       = 1;
					if (goal == change && prop == slot && sol.action == AIAction.move) score        = 1;
					if (goal == reduce && prop == sedentarism && sol.action == AIAction.move) score = 1;

					// Need preferences
					// ------------------------------------------------------------
					for (var j = 0; j < prefs.Count; j++)
					{
						Preference pref = prefs[j];

						AIValue v = value;
						if (pref.prop != AIProp.none)
							GetTargetProperty(prop, target, ref v);

						score *= CalculateValuePreference(value, ref pref, ref sol);
					}


					// Global preferences
					// ------------------------------------------------------------
					for (var j = 0; j < globalPrefs.Count; j++)
					{
						Preference pref = globalPrefs[j];

						AIValue v = value;
						if (pref.prop != AIProp.none)
							GetTargetProperty(prop, target, ref v);

						score *= CalculateValuePreference(value, ref pref, ref sol);
					}

					effort = Mathf.Max(effort, score);
				}

				total += effort * need.weight_normalized;
			}


			return total;
		}

		private void GetTargetProperty(AIProp target, ITargetable targetable, ref AIValue value)
		{
			switch (targetable)
			{
				case Fighter fighter:
					GetFighterProperty(target, fighter, ref value);
					break;

				case Slot slot:
					GetSlotProperty(target, slot, ref value);
					break;
			}
		}

		private void GetFighterProperty(AIProp prop, Fighter fter, ref AIValue value)
		{
			switch (prop)
			{
				case AIProp.none:
					break;

				case hp:
					value.norm = fter.hp_percent;
					break;

				case sp:
					value.norm = fter.sp_percent;
					break;

				case slot:
					break;

				case sedentarism:
					value.norm = _sedentarism;
					break;

				case safety:
					GetSlotProperty(safety, fter.home, ref value);
					break;

				case danger:
					GetSlotProperty(danger, fter.home, ref value);
					break;

				// case NeedProp.level:
				// 	// TODO level: float between 0 and 1 representing the chunks that this fighter contributes to the team's sum of levels
				// 	break;
				//
				// case NeedProp.vengeance:
				// 	// TODO use decaying memorization energy
				// 	break;
				//
				// case NeedProp.predictive_flight:
				// 	// TODO use probabilistic danger zone
				// 	break;
				//
				// case NeedProp.reactive_flight:
				// 	// TODO use decaying memorization energy
				// 	break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void GetSlotProperty(AIProp prop, Slot slot, ref AIValue val)
		{
			switch (prop)
			{
				case AIProp.none:
					break;

				case safety:
					val.norm = 1 - GetDanger(self, slot);
					break;

				case danger:
					val.norm = GetDanger(self, slot);
					break;
			}
		}


		/// <summary>
		/// Calculate a preference scale for a solution.
		/// </summary>
		/// <returns>Number in range [0, 2] indicating how preferable the value. 1 = no preference, 2 = highly preferred, 0 = disgust./returns>
		private float CalculateValuePreference(AIValue value, ref Preference pref, ref AISolution sol)
		{
			NeedPref type = pref.pref;

			if (!value.norm.HasValue)
			{
				Debug.LogError("Cannot use high/low preference with non-numeric AI value.");
				return 0.5f;
			}

			if (pref.prop == AIProp.none)
			{
				// Same property as the input
				// ----------------------------------------
				float ret = CalculateCurve(ref pref, value.norm.Value);
				if (type == NeedPref.low)
					ret = 1 - ret;

				return ret;
			}
			else
			{
				// Fetch different properties of the solution
				// ----------------------------------------
				float scale = 1;

				foreach (Slot slot in sol.target.slots)
				{
					var val = new AIValue();
					GetSlotProperty(pref.prop, slot, ref val);
					if (val.norm.HasValue)
						scale *= val.norm.Value;
				}

				foreach (Fighter fter in sol.target.fighters)
				{
					var val = new AIValue();
					GetFighterProperty(pref.prop, fter, ref val);
					if (val.norm.HasValue)
						scale *= val.norm.Value;
				}

				return scale;
			}
		}


		private float CalculateCurve(ref Preference pref, float value)
		{
			float p;
			switch (pref.curve)
			{
				case NeedCurve.s_curve:
					p = MathUtil.scurve(value, pref.k);
					break;

				case NeedCurve.j_curve:
					p = MathUtil.jcurve(value, pref.k);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(pref.curve), pref.curve, null);
			}

			return p;
		}

		private float GetDanger(Fighter self, Slot slot)
		{
			int enemies = 0;
			int slots   = 0;

			Slot s = slot;
			while (s != null)
			{
				s = battle.GetSlot(s.coord + s.forward);

				slots++;
				if (battle.IsEnemy(self, slot.owner))
					enemies++;
			}

			if (slots > 0)
				return enemies / (float)slots;

			return 0;
		}

	#endregion

	#region Utils

		public bool IsApplicable(object target, ref Need need)
		{
			switch (need.target)
			{
				case AITarget.None:  break;
				case AITarget.self:  return target == self;
				case AITarget.ally:  return battle.IsAlly(self, target);
				case AITarget.enemy: return battle.IsEnemy(self, target);
				case AITarget.exact: return target == need.fighter;
			}

			return false;
		}

	#endregion


	#region Needs

		public struct AIValue
		{
			public float? norm;
			public Slot   slot;
		}

		public struct Need
		{
			public float    weight;
			public NeedGoal goal;
			public AITarget target;
			public AIProp   prop;
			public Fighter  fighter;

			/// <summary>
			/// Must be calculated with NormalizeNeeds() before usage.
			/// </summary>
			public float weight_normalized;

			public Need(float weight, NeedGoal goal, AITarget target, AIProp prop) : this()
			{
				this.weight = weight;
				this.goal   = goal;
				this.target = target;
				this.prop   = prop;
			}

			public override string ToString() => $"{weight}, {prop}, {target}";
		}

		[LuaUserdata]
		public class NeedEditor
		{
			public UtilityAI ai;
			public int       need;

			internal static readonly NeedEditor instance = new NeedEditor();

			[UsedImplicitly, NotNull]
			public NeedEditor high(float k = 0.3f)
			{
				ai.AddPref(need, new Preference(1, AIProp.none, NeedPref.high, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor low(float k = 0.3f)
			{
				ai.AddPref(need, new Preference(1, AIProp.none, NeedPref.low, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jhigh(float k = 0.3f)
			{
				ai.AddPref(need, new Preference(1, AIProp.none, NeedPref.high, NeedCurve.j_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jlow(float k = 0.3f)
			{
				ai.AddPref(need, new Preference(1, AIProp.none, NeedPref.low, NeedCurve.j_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor high(float scale, AIProp prop, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.high, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor low(float scale, AIProp prop, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.low, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jhigh(float scale, AIProp prop, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.high, NeedCurve.j_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jlow(float scale, AIProp prop, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.low, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor high(float scale, AIProp prop, AITarget target, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.high, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor low(float scale, AIProp prop, AITarget target, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.low, NeedCurve.s_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jhigh(float scale, AIProp prop, AITarget target, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.high, NeedCurve.j_curve, k));
				return this;
			}

			[UsedImplicitly, NotNull]
			public NeedEditor jlow(float scale, AIProp prop, AITarget target, float k = 0.3f)
			{
				ai.AddPref(need, new Preference(scale, prop, NeedPref.low, NeedCurve.s_curve, k));
				return this;
			}


		}

		public struct Preference
		{
			public float     scale;
			public AIProp    prop;
			public NeedPref  pref;
			public NeedCurve curve;
			public float     k;

			public Preference(float scale, AIProp prop, NeedPref pref, NeedCurve curve, float k)
			{
				this.scale = scale;
				this.prop  = prop;
				this.pref  = pref;
				this.curve = curve;
				this.k     = k;
			}

			public Preference(NeedPref pref, NeedCurve needCurve, float k = 0.3f)
			{
				this.scale = 1;
				prop       = AIProp.none;
				this.pref  = pref;
				curve      = needCurve;
				this.k     = k;
			}
		}

		[LuaEnum]
		public enum AITarget
		{
			None,
			self,
			ally,
			enemy,
			exact
		}

		[LuaEnum]
		public enum AIProp
		{
			none,

			/// <summary>
			/// HP percent of a fighter.
			/// </summary>
			hp,

			/// <summary>
			/// SP percent of a fighter.
			/// </summary>
			sp,

			/// <summary>
			/// Home slot of a fighter.
			/// </summary>
			slot,

			/// <summary>
			/// Level contribution of a fighter.
			/// </summary>
			level,

			/// <summary>
			/// Safety coefficient of the solution.
			/// </summary>
			safety,

			/// <summary>
			/// Danger coefficient of the solution.
			/// </summary>
			danger,

			/// <summary>
			/// Revenge coefficient of the fighter.
			/// </summary>
			revenge,

			/// <summary>
			/// Roaming coefficient of the fighter.
			/// </summary>
			sedentarism,

			/// <summary>
			/// Prioritize solutions that get us away from the source of a recent trauma.
			/// </summary>
			// reactive_flight,

			/// <summary>
			/// Prioritize solutions that get us away from predicted danger zones.
			/// </summary>
			// predictive_flight,

			/// <summary>
			/// Prioritize solutions that target fighters inflicting a lot of states.
			/// </summary>
			// buffer,

			/// <summary>
			/// Prioritize solutions that target fighters inflicting a lot of debuffs.
			/// </summary>
			// debuffer
		}

		[LuaEnum]
		public enum NeedGoal
		{
			none,

			/// <summary>
			/// Prioritize solutions that raise the property.
			/// </summary>
			raise,

			/// <summary>
			/// Prioritize solutions that reduce the property.
			/// </summary>
			reduce,

			/// <summary>
			/// Prioritize solutions that change the property.
			/// </summary>
			change,
		}

		[LuaEnum]
		public enum NeedPref
		{
			/// <summary>
			/// Don't prefer solutions based on their current value.
			/// </summary>
			none,

			/// <summary>
			/// Prioritize solutions where the property is lowest.
			/// </summary>
			low,

			/// <summary>
			/// Prioritize solutions where the property is highest.
			/// </summary>
			high,
		}

		[LuaEnum]
		public enum NeedCurve
		{
			s_curve,
			j_curve
		}

	#endregion

	#region Memory

		public struct ProbabilisticZone
		{
			public Pointf points;
		}

	#endregion

	#region Lua API

		[UsedImplicitly, NotNull]
		public NeedEditor need(float weight, NeedGoal goal, AITarget target, AIProp prop)
		{
			return AddNeed(new Need(weight, goal, target, prop));
		}

		[UsedImplicitly]
		public void pref(NeedPref pref, NeedCurve curve, float k)
		{
			AddPref(new Preference(pref, curve, k));
		}

	#endregion
	}
}
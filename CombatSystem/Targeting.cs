using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Scripting;
using Anjin.Util;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Pathfinding.Util;
using UnityEngine;
using Util;

namespace Combat.Data
{
	public struct EffectiveSlot
	{
		/// <summary>
		/// The slot.
		/// </summary>
		public Slot slot;

		/// <summary>
		/// Number between 0 and 1 indicating how effectively this slot can be targeted.
		/// This could be used to deal more damage to targets closer, less to targets
		/// further away, etc.
		/// </summary>
		public float efficiency;
	}

	/// <summary>
	/// A package for passing targeting options around.
	/// </summary>
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Targeting : IEnumerable<Target>
	{
		/// <summary>
		/// Dictates whether or not the selection reticle should show when selecting slots
		/// </summary>
		public bool showSlotReticle = false;

		/// <summary>
		/// All options (populated a priori) to be
		/// available for choosing.
		/// </summary>
		public readonly List<List<Target>> options = new List<List<Target>>();

		/// <summary>
		/// Effective range where
		/// </summary>
		public readonly List<List<EffectiveSlot>> range = new List<List<EffectiveSlot>>();

		/// <summary>
		/// Holds the user's/brain/etc. choices.
		/// </summary>
		public readonly List<Target> picks = new List<Target>();

		/// <summary>
		/// All fighters involved in the choices.
		/// </summary>
		public readonly List<Fighter> fighters = new List<Fighter>();

		/// <summary>
		/// All slots involved in the choices.
		/// </summary>
		public readonly List<Slot> slots = new List<Slot>();

		public Target this[int i] => picks[i];
		public int count => picks.Count;

		/// <summary>
		/// The centroid of all the targets. (which are themselves centroids)
		/// </summary>
		public Vector3 Centroid
		{
			get
			{
				var calc = new Centroid();

				foreach (Target selectionsValue in picks)
				{
					calc.add(selectionsValue.position);
				}

				return calc.get();
			}
		}

		public Fighter PickedEntity => picks[0].Fighter;

		public void Clear()
		{
			options.Clear();
			picks.Clear();
			range.Clear();
			fighters.Clear();
		}

		[UsedImplicitly]
		public void AddOptions([NotNull] DynValue dv)
		{
			if (dv.IsNil())
			{
				options.Add(new List<Target>());
				return;
			}

			if (dv.AsObject(out Fighter fighter))
			{
				options.Add(new List<Target> { new Target(fighter) });
				return;
			}

			if (dv.AsObject(out Slot slot))
			{
				Target newSlot = new Target(slot);
				newSlot.showSlotReticle = showSlotReticle;
				options.Add(new List<Target> { newSlot });
				return;
			}

			if (dv.AsTable(out Table tbl))
			{
				AddOptions(tbl);
				return;
			}


			// Debug.LogError($"Unsupported DynValue type for Targeting.AddGroup: {dv}");
		}

		[UsedImplicitly]
		public void AddOptions([NotNull] Table table)
		{
			var glist = new List<Target>();
			for (var i = 1; i <= table.Length; i++)
			{
				DynValue dv = table.Get(i);

				if (!dv.AsObject(out Target tg))
				{
					if (dv.AsTable(out Table tbl))
					{
						tg                 = new Target(tbl);
						tg.showSlotReticle = showSlotReticle;
					}
					else if (dv.AsObject(out Fighter fighter)) tg = new Target(fighter);
					else if (dv.AsObject(out Slot slot))
					{
						tg                 = new Target(slot);
						tg.showSlotReticle = showSlotReticle;
					}
				}

				if (tg.IsEmpty)
				{
					// Debug.LogWarning("Skipping adding of empty target group");
					continue;
				}

				tg.RefreshInfo();
				glist.Add(tg);
			}

			if (glist.Count > 0)
			{
				// Debug.LogWarning("Skipping adding of empty target group");
				options.Add(glist);
			}
		}

		/// <summary>
		/// Add a set of options.
		///
		/// WARNING:
		/// This implicitly adds a group of several targets, i.e. a whole step of targeting.
		/// This is a utility to avoid needing to wrap lone targets in a table.
		/// Use AddOption instead if adding a single additional option to the current set of the targeting step.
		/// </summary>
		/// <param name="target"></param>
		[UsedImplicitly]
		public void AddOptions([NotNull] Target target)
		{
			target.showSlotReticle = showSlotReticle;
			options.Add(new List<Target> { target });
		}

		public void AddOption([NotNull] Target target)
		{
			if (options.Count == 0)
				options.Add(new List<Target>());

			target.showSlotReticle = showSlotReticle;

			options[options.Count - 1].Add(target);
		}

		public void AddOptions([NotNull] List<Target> targets)
		{
			foreach (Target t in targets)
			{
				t.showSlotReticle = showSlotReticle;
				AddOption(t);
			}
		}

		[UsedImplicitly]
		public void AddRange([NotNull] Table tbl)
		{
			var list = new List<EffectiveSlot>();

			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);

				if (dv.AsObject(out Slot slot))
				{
					list.Add(new EffectiveSlot
					{
						slot       = slot,
						efficiency = 1
					});
				}
				else if (dv.AsObject(out Fighter fter))
				{
					list.Add(new EffectiveSlot
					{
						slot       = fter.HomeTargeting,
						efficiency = 1
					});
				}
			}

			range.Add(list);
		}

		[UsedImplicitly]
		public void AddRange(List<Fighter> fighters)
		{
			throw new NotImplementedException(); // TODO
		}

		public void AddPick([NotNull] Target target)
		{
			target.index           = picks.Count;
			target.showSlotReticle = showSlotReticle;
			picks.Add(target);
			target.RefreshInfo();
			fighters.AddRange(target.fighters);
			slots.AddRange(target.slots);
		}

		public Friendliness FindAverageFriendliness([NotNull] Battle battle, [NotNull] Fighter relativeTo)
		{
			Friendliness maxValue = Friendliness.Self;
			var          maxSum   = 0;

			var alignmentValues = (Friendliness[])Enum.GetValues(typeof(Friendliness));
			var alignmentSums   = new int[alignmentValues.Length];

			foreach (Target target in picks)
			{
				for (var i = 0; i < alignmentValues.Length; i++)
				{
					Friendliness alignment = alignmentValues[i];

					foreach (Fighter entity in target.fighters)
					{
						if (alignment.Matches(battle, relativeTo, entity)) Increment(i);
					}

					foreach (Slot slot in target.slots)
					{
						if (alignment.Matches(battle, relativeTo, slot)) Increment(i);
					}
				}
			}

			void Increment(int alignmentIndex)
			{
				alignmentSums[alignmentIndex]++;

				int sum = alignmentSums[alignmentIndex];
				if (sum >= maxSum)
				{
					maxSum   = sum;
					maxValue = alignmentValues[alignmentIndex];
				}
			}

			return maxValue;
		}

		private static List<DistanceTarget> _scratchDistance = new List<DistanceTarget>();

		public static Target FindBest([NotNull] List<Target> options, [CanBeNull] Fighter user = null)
		{
			if (options.Count == 0) return null;

			const int FIGHTER_SCORE = 110;
			const int SLOT_SCORE    = 100;
			const int USER_SCORE    = 150;

			for (var i = 0; i < options.Count; i++)
			{
				_tmpScores.Add(0);
			}

			// Calculate center
			// ----------------------------------------
			var center = new Vector3();
			for (var i = 0; i < options.Count; i++)
			{
				Target tg = options[i];
				center += tg.position;
			}

			center /= options.Count;

			// Privilege targets closer to center
			// ----------------------------------------
			for (var i = 0; i < options.Count; i++)
			{
				Target option = options[i];
				_tmpScores[i] -= Vector3.Distance(center, option.position);
			}

			for (var i = 0; i < options.Count; i++)
			{
				Target option = options[i];

				// Privilege non-empty targets
				// ----------------------------------------
				if (option.slots.Count > 0)
				{
					// Privilege taken slots
					// ----------------------------------------
					foreach (Slot s in option.slots)
					{
						if (s.taken)
						{
							_tmpScores[i] += FIGHTER_SCORE;
							break;
						}
					}
				}

				foreach (Fighter fighter in option.fighters)
				{
					_tmpScores[i] += FIGHTER_SCORE;
					if (fighter == user) _tmpScores[i] += USER_SCORE;
				}

				foreach (Slot slot in option.slots)
				{
					if (slot.taken) _tmpScores[i] += FIGHTER_SCORE;
					else _tmpScores[i]            += SLOT_SCORE;

					if (slot.owner == user) _tmpScores[i] += USER_SCORE;
				}
			}

			// Privilege user targets
			// ----------------------------------------

			// if (relativeto != null)
			// {
			// 	for (var i = 0; i < options.count; i++)
			// 	{
			// 		target option = options[i];
			// 		_tmpscores[i] -= vector3.distance(center, option.center);
			// 	}
			//
			// 	_tmpscores[i] -= vector3.distance(center, option.center);
			// }

			float highest  = -int.MaxValue;
			int   ihighest = -int.MaxValue;
			for (var i = 0; i < _tmpScores.Count; i++)
			{
				float score = _tmpScores[i];
				if (highest < score)
				{
					highest  = score;
					ihighest = i;
				}
			}

			_tmpScores.Clear();
			return options[ihighest];
		}

		/// <summary>
		/// Pick the target nearest towards a particular direction.
		/// Use this function when the user presses the arrow keys for example to choose a target.
		/// </summary>
		public static Target FindTowards(Target current, Vector3 direction, [NotNull] List<Target> all)
		{
			if (all.Count == 0) return current;
			if (all.Count == 1) return all.First();

			foreach (Target target in all)
			{
				target.RefreshInfo();
			}

			// Find ray and spherical distances
			// ----------------------------------------
			const float RAY_ALIGNMENT_THRESHOLD = 0.5f;

			Vector3 origin = current.position;

			foreach (Target target in all)
			{
				if (target == current) continue;

				float dot = Vector3.Dot(current.position.Towards(target.position), direction);
				if (dot < 0.25f)
					continue;

				float d2 = MathUtil.DistanceToRay(target.position, new Ray(origin, direction));
				float d3 = Vector3.Distance(origin, target.position);

				if (d2 < RAY_ALIGNMENT_THRESHOLD) d2 = 0;

				_scratchDistance.Add(new DistanceTarget(target, d2, d3));
			}

			// Determine best result
			// ----------------------------------------

			try
			{
				if (_scratchDistance.Count == 0) return current;
				if (_scratchDistance.Count == 1) return _scratchDistance[0].target;

				_scratchDistance.Sort((a, b) => a.raydist.CompareTo(b.raydist));

				// Best-case scenario: none of the targets are aligned.
				DistanceTarget best = _scratchDistance[0];

				if (best.raydist < RAY_ALIGNMENT_THRESHOLD)
				{
					// Worst-case scenario:
					// We must consider that more than one target
					// can be aligned on the ray. In that case,
					// we choose the nearest slot to our current.
					for (var i = 0; i < _scratchDistance.Count; i++)
					{
						DistanceTarget dt = _scratchDistance[i];

						if (dt.spheredist < best.spheredist) best = dt;
						if (dt.raydist > RAY_ALIGNMENT_THRESHOLD) break;
					}
				}

				return best.target;
			}
			finally
			{
				// Forgetting to clear this has caused approximately ~7 bugs and repairs to this function.
				// So now it's wrapped in try/finally, a product of the universe's failings
				_scratchDistance.Clear();
			}
		}

		[NotNull]
		public string ToStringPicks()
		{
			StringBuilder sb = ObjectPoolSimple<StringBuilder>.Claim();
			sb.Append("[");
			foreach (Target pick in picks)
			{
				sb.Append(pick.ToStringPretty());
			}

			sb.Append("]");

			var ret = sb.ToString();

			sb.Clear();
			ObjectPoolSimple<StringBuilder>.Release(ref sb);
			return ret;
		}

		private readonly struct DistanceTarget
		{
			public readonly float  raydist;
			public readonly float  spheredist;
			public readonly Target target;

			public DistanceTarget(Target target, float raydist, float spheredist)
			{
				this.target     = target;
				this.raydist    = raydist;
				this.spheredist = spheredist;
			}
		}

		IEnumerator<Target> IEnumerable<Target>.GetEnumerator() => picks.GetEnumerator();

		public IEnumerator GetEnumerator() => picks.GetEnumerator();


		[NotNull]
		public Targeting Clone()
		{
			var ret = new Targeting();

			foreach (List<Target> g in options)
				ret.options.Add(g.ToList());

			ret.picks.AddRange(picks);
			ret.fighters.AddRange(fighters);
			return ret;
		}

		public override string ToString()
		{
			string ret = "Targeting(";

			for (var i = 0; i < options.Count; i++)
			{
				ret += options[i].Count;
				if (i < options.Count - 1)
					ret += ", ";
			}

			ret += ")";

			return ret;
		}

		private static List<float> _tmpScores = new List<float>();

		public bool PickRandomly([CanBeNull] string no_option = null)
		{
			picks.Clear();

			foreach (List<Target> group in options)
			{
				// Prioritize taken slots, if the target is a slot target
				Target target = @group.Where(g => g.slots.Count == 0 || g.slots.Any(s => s.taken)).Choose();
				if (target == null)
				{
					if (no_option != null)
					{
						DebugLogger.LogError(no_option, LogContext.Combat, LogPriority.Low);
					}
					else
					{
						DebugLogger.LogError("A target group has no options.", LogContext.Combat, LogPriority.Low);
					}

					return false;
				}

				AddPick(target);
			}

			return true;
		}

		public void CopyPicksToOptions()
		{
			for (var i = 0; i < picks.Count; i++)
			{
				Target target = picks[i];

				if (options.Count < i + 1)
					options.Add(new List<Target>());
				else
					options[i].Clear();

				options[i].Add(target);
			}
		}
	}
}
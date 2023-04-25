using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Anjin.Scripting;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.Utilities;
using UnityEngine;

namespace Combat.Data
{
	/// <summary>
	/// A transient object that is used when applying a proc, and serves to
	/// hold the context about the application process of the proc. Interacting
	/// with this context can alter the rest of the process in some ways, e.g.
	/// hooking animations or modding the proc with BuffStats or ProcStats
	/// THIS IS CURRENTLY BEING UPDATED TWICE PER SKILL CAST. ONCE IN
	/// SkillAction.OnBefore() and once in Battle.Proc().
	/// </summary>
	[LuaUserdata]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class ProcContext
	{
		public Proc                  proc;
		public object                victim;
		public List<ProcAnimation>   animators               = new List<ProcAnimation>();
		public List<Battle.SlotSwap> resultingFormationSwaps = new List<Battle.SlotSwap>();
		public ProcStatus            status                  = new ProcStatus();
		public List<ProcEffect>      extendedEffects         = new List<ProcEffect>();

		[CanBeNull]
		public Fighter dealer => proc.dealer;

		public bool dealt => proc.dealer != null;

		public List<ProcEffect> effects => proc.effects;

		public bool physical  => effects.Any(eff => eff.IsPhysical);
		public bool magical   => effects.Any(eff => eff.IsMagical);
		public bool hurts     => effects.Any(eff => eff.IsHurting);
		public bool heals     => effects.Any(eff => eff.IsHealing);
		public bool attacking => hurts && proc.dealer != null;

		public ProcContext()
		{
			Reset();
		}

		public void Reset()
		{
			victim = null;
			extendedEffects.Clear();
			status.Reset();
			animators.Clear();
			resultingFormationSwaps.Clear();
		}

		/// <summary>
		/// Check if the proc matches the element.
		/// </summary>
		/// <param name="elem"></param>
		/// <returns></returns>
		public bool element(Elements elem)
		{
			return effects.Any(eff => eff.Element == elem);
		}

		public HashSet<Elements> get_elements()
		{
			return effects.Select(x => x.Element).ToHashSet();
		}

		public bool has_tag([NotNull] Table tags)
		{
			for (var i = 1; i < tags.Length; i++)
			{
				if (tags.Get(i).AsString(out string tag) && proc.tags?.Contains(tag) == true)
					return true;
			}

			return false;
		}

		public bool has_tags([NotNull] Table tags)
		{
			for (var i = 1; i < tags.Length; i++)
			{
				if (!tags.Get(i).AsString(out string tag) || proc.tags?.Contains(tag) == false)
					return false;
			}

			return true;
		}

		public bool has_tag(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		{
			// Slightly more performant than passing a table, probably won't need more than 6... maybe
			if (proc.tags == null) return false;

			if (t1 != null && proc.tags.Contains(t1)) return true;
			if (t2 != null && proc.tags.Contains(t2)) return true;
			if (t3 != null && proc.tags.Contains(t3)) return true;
			if (t4 != null && proc.tags.Contains(t4)) return true;
			if (t5 != null && proc.tags.Contains(t5)) return true;
			if (t6 != null && proc.tags.Contains(t6)) return true;

			return false;
		}

		public bool has_tags(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		{
			// Slightly more performant than passing a table, probably won't need more than 6... maybe
			bool ret = true;

			if (proc.tags == null) return false;

			if (t1 != null && !proc.tags.Contains(t1)) ret = false;
			if (t2 != null && !proc.tags.Contains(t2)) ret = false;
			if (t3 != null && !proc.tags.Contains(t3)) ret = false;
			if (t4 != null && !proc.tags.Contains(t4)) ret = false;
			if (t5 != null && !proc.tags.Contains(t5)) ret = false;
			if (t6 != null && !proc.tags.Contains(t6)) ret = false;

			return ret;
		}

		/// <summary>
		/// Check if the proc has a matching effect.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool has(string name)
		{
			return effects.Any(eff => eff.ToString().Contains(name));
		}

		/// <summary>
		/// Get a proc effect by name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[CanBeNull]
		[UsedImplicitly]
		public ProcEffect get(string name)
		{
			return effects.FirstOrDefault(eff => Regex.IsMatch(eff.ToString().ToLower(), name));
		}

		/// <summary>
		/// Extend the proc with additional effects, but only for this specific application of it.
		/// </summary>
		/// <param name="tbl"></param>
		public void extend([NotNull] Table tbl)
		{
			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);

				// Expand recursively
				while (dv.Type == DataType.Function)
					dv = Lua.Invoke(dv.Function);

				if (dv.AsUserdata(out ProcEffect procEffect))
				{
					extendedEffects.Add(procEffect);
				}
				else if (dv.AsUserdata(out State state))
				{
					extendedEffects.Add(new AddState(state));
				}
				else
				{
					DebugLogger.LogError(dv.Type == DataType.UserData
						? $"Unsupported type '{dv.UserData.Object.GetType().Name}' for ProcApplication.extend!"
						: $"Unsupported type '{dv.Type}' for ProcApplication.extend!", LogContext.Combat, LogPriority.High);
				}
			}
		}

	#region Convenient API for ProcStats

		public void set(ProcStat stat, float v)
		{
			status.set(stat, v);
		}

		public void up(ProcStat stat, float v)
		{
			status.up(stat, v);
		}

		public void down(ProcStat stat, float v)
		{
			status.down(stat, v);
		}

		public void scale(ProcStat stat, float v)
		{
			status.scale(stat, v);
		}

	#endregion

	#region Animation

		public void AddImplicitAnimations()
		{
			if (proc.GetEnv().TryGet("__proc", out Closure c1)) anim(c1);
			if (proc.GetEnv().TryGet($"__{proc.ID}", out Closure c2)) anim(c2);
		}

		[UsedImplicitly]
		public void anim([CanBeNull] Closure closure)
		{
			if (closure == null)
			{
				this.LogError($"Attempting to add null animation closure.", nameof(anim));
				return;
			}

			animators.Add(new ProcAnimation
			{
				closure = closure,
				type    = ProcAnimation.Type.Victim
			});
		}

		[UsedImplicitly]
		public void anim([CanBeNull] string id, [CanBeNull] Closure closure)
		{
			if (closure == null)
			{
				this.LogError($"Attempting to add null animation closure with id='{id}'.", nameof(anim));
				return;
			}

			if (id == null)
			{
				anim(closure);
				return;
			}

			for (var i = 0; i < animators.Count; i++)
			{
				if (animators[i].id == id)
				{
					animators.RemoveAt(i);
					break;
				}
			}

			animators.Add(new ProcAnimation
			{
				id      = id,
				closure = closure,
				type    = ProcAnimation.Type.Victim
			});
		}

		[UsedImplicitly]
		public void autoanim(Closure closure)
		{
			Lua.Invoke(closure, new object[] { this });
		}

	#endregion
	}
}
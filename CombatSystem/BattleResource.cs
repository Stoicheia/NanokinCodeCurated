using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat.Toolkit
{
	public abstract class BattleResource : ILogger
	{
		public Battle battle;

		protected LuaEnv env;
		protected bool   hasSpecifiedTargets;

		/// <summary>
		/// Unique identifier for this resource, defines its 'identity'.
		/// </summary>
		public virtual string ID { get; set; }

		/// <summary>
		/// A truly unique number that identifies this exact resource object.
		/// </summary>
		public int UID;

		/// <summary>
		/// Execution environment of the resource.
		/// </summary>
		public LuaEnv Env
		{
			set => env = value;
		}

		/// <summary>
		/// Contextual parent of the resource.
		/// </summary>
		public BattleResource Parent { get; set; }

		public bool RequiresParent => false;

		/// <summary>
		/// Is the resource still having an effect somehow on the battle?
		/// </summary>
		public virtual bool IsAlive => false;

		/// <summary>
		/// Proc stats to applied by recursing up from a child.
		/// </summary>
		public ProcStatus pstats = new ProcStatus();

		/// <summary>
		/// Whether an attempt was made to specify the resource's targets.
		/// </summary>
		public virtual bool HasSpecifiedTargets => hasSpecifiedTargets;

		protected BattleResource()
		{
			UID = RNG.Int();
		}

		/// <summary>
		/// Execution environment of the resource.
		/// If none is assigned to us, we will use our parent's, so on and so forth.
		/// </summary>
		public LuaEnv GetEnv() => env.Or(Parent);

		/// <summary>
		/// Add a target to the resource if the target does stuff.
		/// </summary>
		/// <param name="slot"></param>
		public virtual void AddTarget(Slot slot) { }

		public virtual void AddTarget(Fighter fighter) { }

		public virtual void ConfigureTB([CanBeNull] Table tbl) { }

		public virtual void ConfigureDV([CanBeNull] DynValue dv) { }

		/// <summary>
		/// Check the parents recursively to see if we the given parent somewhere above.
		/// </summary>
		public bool HasParent(BattleResource parent)
		{
			if (Parent == null) return false;
			if (Parent == parent) return true;
			else return Parent.HasParent(parent);
		}

	#region Logging

		private bool _logSilent;

		public string LogID => ID;

		public bool LogSilenced
		{
			get => _logSilent || battle?.LogSilenced == true;
			set => _logSilent = value;
		}

	#endregion

		public void AutoTarget(object obj)
		{
			var eff = new MetaTarget();
			eff.Set(obj);
			AutoTarget(eff);
		}

		public void AutoTarget(MetaTarget metaTarget)
		{
			if (this.HasSpecifiedTargets) return;

			if (metaTarget.state != null)
			{
				AutoTarget(metaTarget.state);
			}
			else if (metaTarget.targeting != null)
			{
				AutoTarget(metaTarget.targeting);
			}
			else if (metaTarget.target != null)
			{
				AutoTarget(metaTarget.target);
			}
			else if (metaTarget.dv != null)
			{
				AutoTarget(metaTarget.dv);
			}
			else if (metaTarget.fighter != null)
			{
				this.AddTarget(metaTarget.fighter);
			}
			else if (metaTarget.slot != null)
			{
				this.AddTarget(metaTarget.slot);
			}
			else if (metaTarget.fighters != null)
			{
				foreach (Fighter fter in metaTarget.fighters)
					this.AddTarget(fter);
			}
			else if (metaTarget.slots != null)
			{
				foreach (Slot fter in metaTarget.slots)
					this.AddTarget(fter);
			}
		}

		public void AutoTarget([NotNull] Target target)
		{
			foreach (Slot slot in target.slots) this.AddTarget(slot);
			foreach (Fighter fter in target.fighters) this.AddTarget(fter);
		}

		public void AutoTarget([NotNull] Targeting targeting)
		{
			foreach (Target tget in targeting.picks)
			{
				AutoTarget(tget);
			}
		}

		public void AutoTarget([NotNull] State state)
		{
			foreach (IStatee statee in state.statees)
			{
				if (statee is Fighter ft)
				{
					this.AddTarget(ft);
				}
				else if (statee is Slot slot)
				{
					this.AddTarget(slot);
				}
			}
		}

		public void AutoTarget(DynValue dv)
		{
			if (dv.AsObject(out object obj))
			{
				AutoTarget(obj);
			}
		}
	}

	public static class LuaEnvExtensions
	{
		public static void InitEnvRef([NotNull] this BattleResource res, LuaEnv env)
		{
			if (!res.GetEnv().lua)
				res.Env = env;
		}

		public static void InitEnvRef([NotNull] this BattleResource res, [NotNull] BattleResource other)
		{
			InitEnvFork(res, other.GetEnv());
		}

		public static void InitEnvFork([NotNull] this BattleResource res, LuaEnv env)
		{
			if (!res.GetEnv().lua)
				res.Env = new LuaEnv(env, res.ID) { owner = res };
		}

		public static void InitEnvFork([NotNull] this BattleResource res, [NotNull] BattleResource other)
		{
			InitEnvFork(res, other.GetEnv());
		}
	}

	public struct MetaTarget
	{
		public Fighter       fighter;
		public List<Fighter> fighters;
		public Slot          slot;
		public List<Slot>    slots;
		public State         state;
		public Targeting     targeting;
		public Target        target;
		public DynValue      dv;

		public void Reset()
		{
			fighter   = null;
			fighters  = null;
			slot      = null;
			slots     = null;
			state     = null;
			targeting = null;
			target    = null;
			dv        = null;
		}

		public void Set([CanBeNull] object obj)
		{
			Reset();

			if (obj == null) return;

			switch (obj)
			{
				case Fighter fighter:
					this.fighter = fighter;
					break;

				case List<Fighter> fighters:
					this.fighters = fighters;
					break;

				case Slot slot:
					this.slot = slot;
					break;

				case List<Slot> slots:
					this.slots = slots;
					break;

				case State state:
					this.state = state;
					break;

				case Targeting targeting:
					this.targeting = targeting;
					break;

				case Target target:
					this.target = target;
					break;

				case DynValue dv:
					this.dv = dv;
					break;

				default:
					throw new ArgumentException("Unknown type to EffectTarget.Set");
			}
		}
	}
}
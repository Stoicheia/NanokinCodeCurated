using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using Microsoft.Win32;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Toolkit
{
	/// <summary>
	/// A state-machine object for creating animations that do stuff.
	/// This is what allows the proc { ... }, state { ... } APIs in combat function calls dispatched
	/// from the engine, like trigger handlers and skill use.
	/// </summary>
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class AnimSM
	{
		private const string ENV_SLOT_TARGET  = "slot";
		private const string ENV_SLOTS_TARGET = "slots";
		private const string ENV_ONTO_TARGET  = "onto";

		/// <summary>
		/// The resource that this animation is being created for.
		/// All the resources for the animation will be parented to this resource.
		/// </summary>
		public BattleResource parentResource;

		/// <summary>
		/// Default target for objects.
		/// </summary>
		[UsedImplicitly]
		public MetaTarget defaultTarget = new MetaTarget();

		/// <summary>
		/// Default animation for a popped CoplayerAnimation with no specified animation.
		/// </summary>
		public string defaultAnimation = "__anim";

		public LuaEnv genv;
		public LuaEnv env;

		private ProcKinds            _nextProcKind;
		private string               _animation = null;
		private List<BattleResource> _resources = new List<BattleResource>();
		private Stack<LuaEnv>        _savedEnvs = new Stack<LuaEnv>();
		private bool                 _started   = false;
		private bool                 _error     = false;

		public AnimSM()
		{
			parentResource = null;
		}

		public AnimSM(BattleResource parent)
		{
			parentResource = parent;
		}

		private bool NotStartedError()
		{
			if (!_started)
			{
				AjLog.LogError($"The {nameof(AnimSM)} hasn't been started.", nameof(AnimSM), nameof(EndInstant));
				return true;
			}

			return false;
		}

		public void Reset()
		{
			genv       = new LuaEnv();
			env        = new LuaEnv();
			_animation = null;
			_resources.Clear();
			_error = false;
		}

		public void Start(LuaEnv env)
		{
			if (_started)
				this.LogWarn("EnvAPI is already started. Most likely this is related to a previous error, otherwise it is a BIG bug in the battle systems.");

			Reset();

			if (env.lua)
			{
				genv     = env;
				this.env = env;
				this.env.Push(this);

				env.table[LuaEnv.OBJ_ANIMSM] = this;
			}

			_started = true;
		}


		[CanBeNull]
		public BattleAnim End([CanBeNull] BattleAnim anim = null)
		{
			if (env.lua)
			{
				env.Pop(this);
			}

			Reset();
			_started = false;

			AjLog.LogTrace("--", $"EnvAPI.End({anim})");
			return anim;
		}

	#region Environment State Machine

		public void ipush()
		{
			_savedEnvs.Push(env);
		}

		public void ipop()
		{
			// TODO commit inames
			env = _savedEnvs.Pop();
		}

		public void ienv()
		{
			env = new LuaEnv(genv, null);
		}

		public void ienv([NotNull] BattleResource res)
		{
			res.InitEnvFork(genv);
			env = res.GetEnv();
			// Too risky, we'll use iget() instead for now.
			// env.iunpack(genv);
		}

		public void iset(string key, DynValue dv)
		{
			env.iset(key, dv);
		}

		public DynValue iget(string key) => env.iget(key);

		public void ionto(DynValue onto)
		{
			iset(ENV_ONTO_TARGET, onto);
		}

		public void islot(DynValue onto)
		{
			iset(ENV_SLOT_TARGET, onto);
			_nextProcKind = ProcKinds.Slot;
		}

		public void islots(DynValue onto)
		{
			iset(ENV_SLOTS_TARGET, onto);
			_nextProcKind = ProcKinds.Slot;
		}

		public bool iis_proc() => env.owner is Proc;

		private void iautotarget([NotNull] BattleResource res)
		{
			if (!res.HasSpecifiedTargets)
			{
				if (env.ihas(ENV_SLOT_TARGET)) res.AutoTarget(env.iget(ENV_SLOT_TARGET));
				if (env.ihas(ENV_SLOTS_TARGET)) res.AutoTarget(env.iget(ENV_SLOTS_TARGET));
				if (env.ihas(ENV_ONTO_TARGET)) res.AutoTarget(env.iget(ENV_ONTO_TARGET));

				res.AutoTarget(defaultTarget);
			}
		}

	#endregion


		[UsedImplicitly]
		[NotNull]
		[MoonSharpHidden]
		public Proc proc([NotNull] Proc v)
		{
			if (_nextProcKind != ProcKinds.Default)
			{
				v.kind        = _nextProcKind;
				_nextProcKind = ProcKinds.Default;
			}

			v.Env = env;

			iautotarget(v);
			if (string.IsNullOrEmpty(v.ID))
				v.ID = $"{env.id}/proc";

			_resources.Add(v);
			return v;
		}

		[UsedImplicitly]
		[NotNull]
		public Proc proc(DynValue conf)
		{
			if (conf.AsExact(out Proc proc))
				return this.proc(proc);

			var v = new Proc(genv.battle)
			{
				ID     = $"{env.id}/proc",
				dealer = env.dealer,
				kind = _nextProcKind == ProcKinds.Default
					? ProcKinds.Fighter
					: _nextProcKind
			};

			v.ConfigureDV(conf);
			this.proc(v);
			return v;
		}

		[UsedImplicitly]
		[NotNull]
		public State state([CanBeNull] Table conf = null)
		{
			var v = new State
			{
				Env = env,
				ID  = $"{env.id}"
			};

			v.ConfigureTB(conf);

			_resources.Add(v);
			return v;
		}

		[UsedImplicitly]
		[NotNull]
		public Fighter fighter([CanBeNull] Table conf)
		{
			var info = new MockInfo
			{
				points = Pointf.One
			};
			info.ConfigureTB(conf);

			Fighter fter = new Fighter(info);
			// fter?.battle = parentResource.battle;
			fter.ConfigureTB(conf);

			_resources.Add(fter);
			return fter;
		}

		[UsedImplicitly]
		[NotNull]
		public Fighter ofighter([CanBeNull] Table conf)
		{
			Fighter fter = fighter(null);
			var     info = new MockInfo();
			fter.turnEnable         = false;
			info.baseState.set_heal = 0;
			info.baseState.set_hurt = 1;

			// This is the object fighter state, it's very special
			fter.states.Add(fter.baseState
					.Add(new StateFunc(StatOp.forbid, StateStat.state_tag, Tags.STATE_STATUS)) // cannot get any status
					.Add(new StateFunc(StatOp.forbid, StateStat.state_tag, Tags.STATE_DOT))    // cannot get any dot
					.Add(new StateFunc(StatOp.forbid, StateStat.state_tag, Tags.STATE_GOOD))   // cannot get any good state
					.Add(new StateFunc(StatOp.forbid, StateStat.state_tag, Tags.STATE_BAD))    // cannot get any bad state
			);
			fter.ConfigureTB(conf);

			return fter;
		}

		[UsedImplicitly]
		[NotNull]
		public Trigger trigger([CanBeNull] Table conf = null)
		{
			var v = new Trigger { Env = env };
			v.ConfigureTB(conf);
			if (!conf.ContainsKey("id")) // We have to do this after since the signal could be set in configure
				v.SetIDWithSignal(env.id);

			_resources.Add(v);
			return v;
		}

		/// <summary>
		/// - Remove children resources (trigger inside a state, state inside a trigger, etc.)
		/// - Convert state to procs.
		/// </summary>
		public void ProcessResources()
		{
			if (NotStartedError()) return;

			// Convert the states to procs
			for (var i = 0; i < _resources.Count; i++)
			{
				BattleResource res = _resources[i];
				if (res.Parent != null)
					continue;
				if (res is State state)
				{
					res = to_proc(state, i);
					_resources.RemoveAt(i);
					_resources.Insert(i, res);
				}
			}

			// Only keep the root resources, and set the parent
			for (var i = 0; i < _resources.Count; i++)
			{
				BattleResource res = _resources[i];

				res.InitEnvFork(env);
				if (res.Parent != null)
					_resources.RemoveAt(i--);
				else if (parentResource != null)
					res.Parent = parentResource;
			}

			foreach (BattleResource res in _resources)
			{
				// Assign the environment
				if (!res.GetEnv().lua)
					res.Env = env;
			}

			foreach (BattleResource res in _resources)
			{
				env.dependencies.Add(res);
			}

			foreach (BattleResource res in _resources)
			{
				if (res is Trigger tg)
				{
					// Use the default target (e.g. the user in passive, target in use, etc.)
					if (tg.filter as string == Trigger.FILTER_AUTO)
					{
						tg.AutoTarget(defaultTarget);
					}

					// Still nothing, default to ANY
					if (tg.filter as string == Trigger.FILTER_AUTO)
					{
						tg.filter = Trigger.FILTER_ANY;
					}
				}
			}
		}

		[NotNull]
		private BattleResource to_proc([NotNull] State state, int i)
		{
			// Wrap the state with a proc
			var proc = new Proc(genv.battle)
			{
				ID     = $"{env.id}/proc",
				dealer = env.dealer,
				Env    = state.GetEnv(),
			};

			proc.AddEffect(new AddState(state));

			// Transfer statees to proc victims
			proc.AddVictims(state.statees);
			state.statees.Clear();

			proc.kind = state.slotRegistrar
				? ProcKinds.Slot
				: ProcKinds.Fighter;

			return proc;
		}

		[CanBeNull]
		public BattleAnim EndCoplayer(string funcname = null, object[] args = null, bool require_anim = false)
		{
			if (NotStartedError()) return null;
			if (_error) return EndInstant();

			funcname = _animation ?? funcname ?? defaultAnimation;

			AjLog.LogVisual("--", $"Animating coplayer action with {funcname}");
			if (!env.ContainsKey(funcname))
			{
				if (require_anim)
					DebugLogger.LogError($"Could not find '{funcname}' animation function for the combat function call. Logic will be applied instantly.", LogContext.Combat, LogPriority.Low);

				return EndInstant(); // The animation couldn't be found
			}

			ProcessResources();

			CoplayerAnim anim = new CoplayerAnim(env.table, funcname, args);
			AddToAnim(anim);

			anim.animsm = this;
			anim.env    = env;

			return End(anim);
		}

		[CanBeNull]
		public BattleAnim EndInstant()
		{
			if (NotStartedError()) return null;

			var action = new BattleAnim();
			ProcessResources();
			AddToAnim(action);

			return End(action);
		}

		private void AddToAnim(BattleAnim anim)
		{
			foreach (BattleResource effect in _resources)
			{
				switch (effect)
				{
					case Proc proc:
						anim.procs.Add(proc);
						break;

					case Fighter fter:
						anim.fighters.Add(fter);
						break;

					case Trigger trigger:
						anim.triggers.Add(trigger);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(effect));
				}
			}
		}

		public void SetAutoTarget(object target)
		{
			defaultTarget.Set(target);
		}

		public bool Invoke(string name, out DynValue result, [CanBeNull] object[] args = null, bool optional = false)
		{
			try
			{
				result = Lua.Invoke(env.table, name, args, optional);
				return true;
			}
			catch (Exception e)
			{
				DebugLogger.LogError(e.ToString(), LogContext.Combat);
				_error = true;
			}

			result = DynValue.Nil;
			return false;
		}
	}
}
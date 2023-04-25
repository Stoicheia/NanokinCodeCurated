using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Data
{
	public class Trigger : BattleResource
	{
		private const int    LIFE_INF    = -1;
		public const  string FILTER_AUTO = "auto";
		public const  string FILTER_ANY  = "any";

		/// <summary>
		/// Unique readable identifier of the trigger.
		/// </summary>
		public string id_anim;

		/// <summary>
		/// Animation for the trigger.
		/// </summary>
		public Closure anim;

		/// <summary>
		/// The particular signal to listen on.
		/// </summary>
		public Signals signal = Signals.none;

		/// <summary>
		/// A filter to be tested against TriggerEvent.me
		/// which must pass for the trigger to fire.
		/// </summary>
		public object filter = FILTER_AUTO;

		/// <summary>
		/// Clause that make up this trigger.
		/// </summary>
		public readonly List<Handler> clauses = new List<Handler>();

		/// <summary>
		/// Whether or not the trigger is enabled.
		/// Can be used for toggle effects (with the life kept at -1)
		/// </summary>
		public bool enabled = true;

		/// <summary>
		/// Is a countdown timer? (only fire its actions when life reaches 0)
		/// </summary>
		public bool isTimer;

		/// <summary>
		/// The state to decay.
		/// This prevents any handler from firing.
		/// </summary>
		public State stateDecay;

		/// <summary>
		/// The state of the trigger, state values are expected to change over the course of the battle.
		/// Everything else is expected to be untouched once the trigger is registered.
		/// </summary>
		public TriggerState state;

		/// <summary>
		/// The BattleAnim builder state machine to use.
		/// </summary>
		public AnimSM asm;

		/// <summary>
		/// The current life of the trigger.
		/// -1 for infinite/immortal
		///
		/// When the life falls to 0, the trigger can no longer handle any event and will be removed.
		/// A contextual number that can be in any way to control the lifetime and activation count of the trigger.
		/// For example, settings this to 5 with a 'start-handler' signals means the life will correspond
		/// to the number of turns left before expiring. (and by extension, the number of turns the trigger will fire)
		/// </summary>
		public int maxlife;

		/// <summary>
		/// <see cref="maxlife"/>
		/// </summary>
		public int life
		{
			get => state.life;
			set => state.life = value;
		}

		public Trigger()
		{
			state = new TriggerState(this, LIFE_INF);
			asm   = new AnimSM(this);
		}

		public Trigger(Signals signal, int life = LIFE_INF) : this()
		{
			this.signal = signal;
			this.life   = life;
		}

		/// <summary>
		/// A singleton handler for state decay triggers.
		/// </summary>
		public static readonly Action<TriggerEvent> BuffDecaySingleton = x =>
		{
			if (x.state != null)
				x.state.Decay();
		};

		/// <summary>
		/// A singleton handler for state expire triggers.
		/// </summary>
		public static readonly Action<TriggerEvent> BuffExpireSingleton = x =>
		{
			if (x.state != null)
				x.state.Expire();
		};

		/// <summary>
		/// A singleton handler for state consume triggers.
		/// </summary>
		public static readonly Action<TriggerEvent> BuffConsumeSingleton = x =>
		{
			if (x.state != null)
				x.state.Consume();
		};

		/// <summary>
		/// Whether or not the animation function exists for the configured id_anim.
		/// </summary>
		public bool AnimExists => GetEnv().lua && id_anim != null && GetEnv().ContainsKey(id_anim);

		/// <summary>
		/// whether or not the trigger is alive based on its hp.
		/// </summary>
		public override bool IsAlive => life != 0;

		/// <summary>
		/// Whether or not the trigger should be killed.
		/// </summary>
		public bool ShouldDie => life == 0;

		/// <summary>
		/// Whether or not the trigger should be shown in the handler order UI.
		/// </summary>
		public bool enableTurnUI = true;

	#region BattleResource

		public override bool HasSpecifiedTargets => filter != null && filter as string != FILTER_AUTO;

		public override void AddTarget(Slot slot)
		{
			filter = slot;
		}

		public override void AddTarget(Fighter fighter)
		{
			filter = fighter;
		}

	#endregion

		/// <summary>
		/// Find out if the trigger would fire now after if it's a match.
		/// </summary>
		public bool WillFireNow(int? life = null) => !isTimer || (life ?? this.life) == 0;

		private Fighter GetContextDealer()
		{
			return (Parent as State)?.dealer;
		}

		public void AddHandlerDV(Handler handler, int idx = LIFE_INF)
		{
			if (idx >= 0)
				clauses.Insert(idx, handler);
			else
				clauses.Add(handler);
		}

		public Trigger AddHandlerDV(Closure closure)
		{
			clauses.Add(new Handler(HandlerType.action, closure));
			return this;
		}

		public Trigger AddHandlerDV(Action<TriggerEvent> action)
		{
			clauses.Add(new Handler(HandlerType.action, action));
			return this;
		}

		public Trigger AddHandlerDV(ProcEffect effect)
		{
			clauses.Add(new Handler(effect));
			return this;
		}

		/// <summary>
		/// Refresh to max life.
		/// </summary>
		public void Refresh()
		{
			life = maxlife;
		}

		/// <summary>
		/// Refresh and set the maxlife.
		/// </summary>
		/// <param name="life"></param>
		public void Refresh(int life)
		{
			maxlife   = life;
			this.life = life;
		}

		/// <summary>
		/// Decay the trigger. The life is clamped to minimum 0
		/// </summary>
		/// <returns></returns>
		public Trigger Decay()
		{
			if (life <= 0)
				return this;

			life = (life - 1).Minimum(0);
			this.LogEffect("--", $"decay:{life}");
			return this;
		}

		/// <summary>
		/// Expire the trigger, meaning it cannot handle anything anymore.
		/// </summary>
		public void Expire()
		{
			life = 0;
			this.LogEffect("--", $"expire:{life}");
		}

		/// <summary>
		/// Verify whether the object matches this trigger's filter.
		/// </summary>
		public bool MatchesFilter([CanBeNull] object me)
		{
			if (filter as string == FILTER_AUTO)
			{
				Debug.LogError($"FILTER_AUTO should've already been transformed. This is a bug and {this} will never fire in order to bring all your attention to it.");
				return false;
			}

			if (me == null) return true;
			if (filter == null) return true;                 // No filter acts as FILTER_ANY
			if (filter as string == FILTER_ANY) return true; // Match for any 'me'

			// Match for slot-fighter conversion
			// This allows to watch for fighter triggers on slots instead
			if (MatchesFilterFighterSlotConversion(me))
				return true;

			// Actually matching against the specific filter
			return MatcheFilterExact(me);
		}

		public bool MatchesFilterFighterSlotConversion(object me)
		{
			if (me is Fighter fighter)
			{
				if (fighter.home != null && MatcheFilterExact(fighter.home))
					return true;
			}

			return false;
		}

		public bool MatcheFilterExact(object me)
		{
			return filter == me || filter is IList list && list.Contains(me);
		}

		public void Fire([CanBeNull] TriggerEvent ev, List<BattleAnim> actions, HandlerType stopHandler = HandlerType.none)
		{
			state.numHandled++;

			if (stateDecay != null)
			{
				stateDecay.Decay();
				return;
			}

			ev.catalyst = this;

			bool   animable = false;
			string idanim   = id_anim;
			if (string.IsNullOrEmpty(id_anim) && ID != null)
				idanim = $"__{ID.Replace("/", "_").Replace("-", "_")}";

			if (GetEnv().ContainsKey(idanim))
			{
				animable = true;
			}
			else if (GetEnv().ContainsKey($"__{signal}"))
			{
				animable = true;
				idanim   = $"__{signal}";
			}

			if (GetEnv().lua) // Clearly we aren't gonna animate anything created on the C# side
			{
				this.LogVisual("--", animable
					? $"trigger_fire: animating with {idanim}"
					: $"trigger_fire: no animation for {idanim}");
			}

			object[] argsev = LuaUtil.Args(ev);
			foreach (Handler clause in clauses)
			{
				if (stopHandler != HandlerType.none && clause.type > stopHandler)
					break;

				env.dealer = env.dealer ?? GetContextDealer();

				asm.env = env;
				asm.Start(GetEnv());
				asm.SetAutoTarget(ev.me);

				RunHandler(ev, clause, argsev);
				BattleAnim auto = animable
					? asm.EndCoplayer(idanim, new[] { ev, ev.me })
					: asm.EndInstant();

				if (auto != null)
				{
					auto.AddAnimFlags(ev.coflags);
					auto.AddSkipFlags(ev.coskips);

					foreach (Proc proc in auto.procs)
					{
						proc.AutoTarget(ev.me);
					}

					actions.Add(auto);
				}
			}

			asm.defaultTarget = new MetaTarget();
		}

		private void RunHandler(TriggerEvent ev, Handler hnd, object[] argsev)
		{
			if (hnd.action != null)
			{
				hnd.action.Invoke(ev);
			}
			else if (hnd.closure != null)
			{
				this.LogEffect(">>", "fire");
				Lua.Invoke(hnd.closure, argsev);
				// DispatchAction(result, @event);
			}
			else if (hnd.proc != null)
			{
				this.LogEffect(">>", "proc");
				Proc proc = hnd.proc;
				proc.Parent = null;

				asm.proc(proc);
			}
			else if (hnd.effect != null)
			{
				var proc = new Proc(battle);
				proc.AddEffect(hnd.effect);
				asm.proc(proc);

				this.LogEffect(">>", "proc");
			}
			else if (hnd.effects != null)
			{
				var proc = new Proc(battle);
				proc.AddEffects(hnd.effects);
				asm.proc(proc);

				this.LogEffect(">>", "proc");
			}
		}

		public override string ToString() =>
			filter as string == FILTER_ANY
				? $"any<{ID}>({signal})"
				: $"on<{ID}>({signal}, {filter})";

		public void SetIDWithSignal(string id)
		{
			ID = $"{id}/{signal}";
		}

		public override void ConfigureTB(Table tb)
		{
			if (tb == null) return;

			tb.TryGet("signal", out signal, signal);
			if (tb.TryGet("life", out state.life, state.life))
				maxlife = state.life;

			if (tb.TryGet("id", out string id))
			{
				ID = id;
			}

			// FILTER
			// ----------------------------------------
			DynValue dvfilter = tb.Get("filter");
			if (dvfilter.IsNotNil())
			{
				FilterDV(dvfilter);
			}

			// CLAUSES
			// ----------------------------------------
			for (var i = 1; i <= tb.Length; i++)
			{
				DynValue dvelem = tb.Get(i);
				if (dvelem.IsNotNil()) AddHandlerDV(dvelem);
			}
		}

		public void FilterDV(DynValue dv)
		{
			switch (dv.Type)
			{
				case DataType.String when dv.String == "auto":
					this.filter = FILTER_AUTO;
					break;

				case DataType.String when dv.String == FILTER_ANY:
					this.filter = FILTER_ANY;
					break;

				case DataType.UserData:
					this.filter = dv.UserData.Object;
					break;

				case DataType.Table:
					var filter = new List<object>();
					for (var i = 1; i <= dv.Table.Length; i++)
						filter.Add(dv.Table.Get(i).UserData.Object);

					this.filter = filter;
					break;
			}

			return;
		}

		public void Filter(object obj)
		{
			if (obj is DynValue dv)
			{
				FilterDV(dv);
				return;
			}
			else
			{
				switch (obj as string)
				{
					// doesn't work without this.. wat
					case FILTER_AUTO:
						filter = FILTER_AUTO;
						break;
					case FILTER_ANY:
						filter = FILTER_ANY;
						break;
				}
			}
		}

		public void AddHandlerDV(DynValue dv)
		{
			if (GetClause(dv, out Handler val))
			{
				AddHandlerDV(val);
			}
		}

		public void PrependDV(DynValue dv)
		{
			if (GetClause(dv, out Handler val))
			{
				AddHandlerDV(val, 0);
			}
		}

		public bool GetClause([NotNull] DynValue dv, out Handler ret)
		{
			switch (dv.Type)
			{
				case DataType.Table:
					if (dv.Table.Get("__chainctr").IsNil()
					    || dv.Table.Get("value").Type != DataType.Table
					    || dv.Table.Get("value").Table.Get("expr").IsNil())
					{
						ret = new Handler();
						DebugLogger.LogError("Passed a table to TriggerProxy.configure(tbl), but it does not seem to contain a handler.", LogContext.Combat, LogPriority.Low);
						return false;
					}

					Table tbl = dv.AsTable();
					GetClause(tbl.Get("expr").String, tbl.Get("func"), out ret);
					return true;

				case DataType.Function:
					// A simple handler-less function implies a fire handler
					ret = new Handler(HandlerType.action, dv.Function);
					return true;
			}

			ret = new Handler();
			return false;
		}

		private bool GetClause(string clause_name, DynValue dvfunc, out Handler ret)
		{
			if (!Enum.TryParse(clause_name, out HandlerType clause))
			{
				ret = new Handler();
				this.LogError($"Trigger: Cannot use unsupported handler name {clause_name}!");
				return false;
			}

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (dvfunc.Type)
			{
				case DataType.Function:
					ret = new Handler(clause, dvfunc.Function);
					return true;

				case DataType.Table:
					ret = new Handler(clause, dvfunc.Table);
					return false;

				default:
					ret = new Handler();
					this.LogError($"Cannot use a {dvfunc.Type} dynvalue to construct a handler!");
					return false;
			}
		}


		public void DecideAnimID()
		{
			if (id_anim == null && ID != null)
			{
				// Yes there is a bit of crazy here.
				// Alas, this is the most optimal way.
				// Otherwise you will have to create two new strings
				char[] chars = new char[2 + ID.Length];
				chars[0] = '_';
				chars[1] = '_';

				// Replace symbols with underscore
				for (var i = 2; i < ID.Length; i++) // start at 2 to skip the first two underscores
				{
					char c = ID[i];

					if (char.IsSymbol(c) || c == '-') c = '_';
					chars[i] = c;
				}

				id_anim = new string(chars);
				if (!AnimExists)
				{
					id_anim = "__trigger";
					if (!AnimExists)
						id_anim = null;
				}
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public enum HandlerType
		{
			/// <summary>
			/// A not very useful handler type.
			/// </summary>
			none,

			/// <summary>
			/// Handler produces a BattleAction with potentially some animation.
			/// Expects values of type Table, Proc, Proc effects, State, Triggers.
			/// </summary>
			action
		}

		/// <summary>
		/// A handler for a combat event.
		/// </summary>
		public readonly struct Handler : IComparable<Handler>
		{
			public readonly HandlerType          type;
			public readonly Closure              closure;
			public readonly Action<TriggerEvent> action;
			public readonly Proc                 proc;
			public readonly ProcEffect           effect;
			public readonly List<ProcEffect>     effects;
			public readonly Table                chain;

			public Handler(HandlerType type, Closure closure)
			{
				this.type    = type;
				this.closure = closure;

				action  = null;
				chain   = null;
				proc    = null;
				effect  = null;
				effects = null;
			}

			public Handler(HandlerType type, Action<TriggerEvent> function)
			{
				this.type = type;
				closure   = null;

				action  = function;
				chain   = null;
				proc    = null;
				effect  = null;
				effects = null;
			}

			public Handler(HandlerType type, Table chain)
			{
				this.type  = type;
				this.chain = chain;

				closure = null;
				action  = null;
				proc    = null;
				effect  = null;
				effects = null;
			}

			public Handler(Proc proc)
			{
				type      = HandlerType.action;
				this.proc = proc;

				closure = null;
				action  = null;
				proc    = null;
				effects = null;
				chain   = null;
				effect  = null;
			}

			public Handler(ProcEffect effect)
			{
				type        = HandlerType.action;
				this.effect = effect;

				closure = null;
				action  = null;
				proc    = null;
				effects = null;
				chain   = null;
			}

			public Handler(List<ProcEffect> effects) : this()
			{
				type         = HandlerType.action;
				this.effects = effects;
			}

			public int CompareTo(Handler other) => type.CompareTo(other.type);
		}
	}
}
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data.VFXs;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Data
{
	public class State : BattleResource
	{
		/// <summary>
		/// The current health of the state.
		/// DO NOT modify directly, use expire(), decay(), etc.
		/// </summary>
		public int life = -1;

		/// <summary>
		/// The max health of the state. by default it has -1 or infinite. (manual expire)
		/// DO NOT modify directly, use configure(), refresh(), etc.
		/// </summary>
		public int maxlife = -1;

		/// <summary>
		/// Tags to categorize this state for filtering operations.
		/// E.g. mark, debuff, counter, status...
		/// </summary>
		[NotNull]
		public readonly List<string> tags = new List<string>();

		/// <summary>
		/// State stats.
		/// DO NOT modify while the state is added to a fighter.
		/// </summary>
		public readonly Status status = new Status();

		/// <summary>
		/// Triggers to add customized battle interactions to the state.
		/// </summary>
		public readonly List<Trigger> triggers = new List<Trigger>();

		/// <summary>
		/// VFXs to apply on the affected entities along with this state.
		/// </summary>
		public readonly List<VFX> vfx = new List<VFX>();

		/// <summary>
		/// Handler operators to apply at the start of each action.
		/// </summary>
		public readonly List<TurnFunc> turnops = new List<TurnFunc>();

		/// <summary>
		/// Fighter which sourced the state.
		/// Can be used to keep a link to the dealer,
		/// pairing it to procs fired through sub-triggers.
		/// (note: this must be done explicitly)
		/// </summary>
		public Fighter dealer;

		/// <summary>
		/// Fighters affected by the state.
		/// If this list is pre-filled before the state is added,
		/// they will be included when the state is added to the battle.
		/// Once the state is registered, this list is automatically maintained
		/// and kept updated.
		///
		/// Do not add or remove fighters from this list manually, use AddStatee functions instead.
		/// </summary>
		public readonly List<IStatee> statees = new List<IStatee>();

		/// <summary>
		/// The state is registered to slots instead of fighters.
		/// </summary>
		public bool slotRegistrar = false;

		/// <summary>
		/// The max number of stacks for this state. Default is just 1.
		/// </summary>
		public int stackMax = 1;

		/// <summary>
		/// Whether or not all stacks will be refreshed when a new one is added.
		/// When stackMax is 1, this is used to refresh an existing instance of this
		/// state as well.
		/// </summary>
		public bool stackRefresh = true;

		/// <summary>
		/// A table of random properties for the state.
		/// Use sparingly.
		/// </summary>
		public Table props = null;

		/// <summary>
		/// Allows the state to appear in UI. (Added, expired, consumed, etc.)
		/// </summary>
		public bool enableUI = true;

		/// <summary>
		/// Whether or not the state is still active in combat. (life > 0)
		/// Managed by Battle, do not assign.
		/// </summary>
		public bool isActive;

		/// <summary>
		/// For AddState conversion
		/// </summary>
		public float chance = 1;

		public bool   dead;      // Whether or not the state is dead. (life == 0)
		public int    deathLife; // life when the state was killed
		public Deaths deathMode; // how the state was killed

		public enum Deaths { None, Expire, Consume }

		public override string ToString() => $"state<{ID}>";

		public State(string id = null)
		{
			ID    = id;
			props = new Table(Lua.envScript);
		}

		private static List<Trigger> _tmpTriggers = new List<Trigger>();

	#region IBattleResource

		public override bool IsAlive => !dead;

		public override void AddTarget([NotNull] Slot slot)
		{
			if (slot.owner != null)
				AddStatees(slot.owner);
		}

		public override void AddTarget(Fighter fighter)
		{
			AddStatees(fighter);
		}

	#endregion

		public bool has_tag(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		{
			// Slightly more performant than passing a table, probably won't need more than 6... maybe
			if (t1 != null && tags.Contains(t1)) return true;
			if (t2 != null && tags.Contains(t2)) return true;
			if (t3 != null && tags.Contains(t3)) return true;
			if (t4 != null && tags.Contains(t4)) return true;
			if (t5 != null && tags.Contains(t5)) return true;
			if (t6 != null && tags.Contains(t6)) return true;

			return false;
		}

		public bool has_tags(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		{
			// Slightly more performant than passing a table, probably won't need more than 6... maybe
			bool ret = true;

			if (t1 != null) ret &= tags.Contains(t1);
			if (t2 != null) ret &= tags.Contains(t2);
			if (t3 != null) ret &= tags.Contains(t3);
			if (t4 != null) ret &= tags.Contains(t4);
			if (t5 != null) ret &= tags.Contains(t5);
			if (t6 != null) ret &= tags.Contains(t6);

			return ret;
		}

		[UsedImplicitly]
		public DynValue this[string key]
		{
			get => props.Get(key);
			set => props[key] = value;
		}

		public State AddTrigger(Trigger trigger)
		{
			if (isActive)
			{
				Debug.LogError("Cannot add trigger to a state while the state is already active!");
				return this;
			}

			trigger.Parent = this;

			if (string.IsNullOrWhiteSpace(ID))
				trigger.SetIDWithSignal(ID);

			if (trigger.filter as string == Trigger.FILTER_AUTO)
			{
				switch (trigger.signal)
				{
					case Signals.enable:
					case Signals.decay:
					case Signals.expire:
					case Signals.consume:
						trigger.filter = this;
						break;

					default:
						trigger.filter = statees;
						break;
				}
			}

			triggers.Add(trigger);
			return this;
		}

		/// <summary>
		/// Add a decay action to a trigger and add it to the state.
		/// </summary>
		public State AddEnable([NotNull] Trigger tg)
		{
			if (tg.signal == Signals.none)
				tg.signal = Signals.enable;

			tg.enableTurnUI = false;
			AddTrigger(tg);
			return this;
		}

		/// <summary>
		/// Add a decay action to a trigger and add it to the state.
		/// </summary>
		public State AddDecay([NotNull] Trigger tg)
		{
			if (tg.signal == Signals.none)
				tg.signal = Signals.decay;

			tg.enableTurnUI = false;
			tg.AddHandlerDV(Trigger.BuffDecaySingleton);
			AddTrigger(tg);
			return this;
		}

		/// <summary>
		/// Add a decay action to a trigger and add it to the state.
		/// </summary>
		public State AddExpire([NotNull] Trigger tg)
		{
			if (tg.signal == Signals.none)
				tg.signal = Signals.expire;

			tg.enableTurnUI = false;
			tg.AddHandlerDV(Trigger.BuffExpireSingleton);
			AddTrigger(tg);
			return this;
		}

		/// <summary>
		/// Add a decay action to a trigger and add it to the state.
		/// </summary>
		public State AddConsume([NotNull] Trigger tg)
		{
			if (tg.signal == Signals.none)
				tg.signal = Signals.consume;

			tg.enableTurnUI = false;
			tg.AddHandlerDV(Trigger.BuffConsumeSingleton);
			AddTrigger(tg);
			return this;
		}

		public State Add(TurnFunc turnop)
		{
			turnops.Add(turnop);
			return this;
		}

		public State Add(StateCmd cmd)
		{
			status.Add(cmd);
			return this;
		}

		public State Add(StateFunc func)
		{
			status.Add(func);
			return this;
		}

		public State Add(VFX vfx)
		{
			this.vfx.Add(vfx);
			return this;
		}

		[NotNull]
		public State Add([CanBeNull] Trigger trigger)
		{
			if (trigger == null) return this;
			AddTrigger(trigger);
			return this;
		}

		public State Clear()
		{
			triggers.Clear();
			turnops.Clear();
			status.Clear();
			vfx.Clear();
			return this;
		}

		/// <summary>
		/// Refresh the state to max life.
		/// </summary>
		public void Refresh(int life = 0)
		{
			if (life == -1) this.maxlife = -1;
			if (life > 0) this.maxlife   = life;

			this.life = this.maxlife;
			dead      = false;
		}

		/// <summary>
		/// Decay the state (decrement life)
		/// </summary>
		public void Decay()
		{
			if (maxlife == -1)
				return;

			life--;
			this.LogEffect("--", $"{this} decay, now={life}");

			if (!dead && life == 0)
			{
				deathLife = 1;
				deathMode = Deaths.Expire;
				battle.RemoveState(this);
			}
		}

		/// <summary>
		/// Expire the state (set life to 0)
		/// </summary>
		public void Expire()
		{
			if (!dead) // Already dead
			{
				dead      = true;
				deathLife = life;
				deathMode = Deaths.Expire;
				life      = 0;
				battle.RemoveState(this);
			}
		}

		/// <summary>
		/// Consume the state (forcibly expired, detonation)
		/// Used for marks
		/// </summary>
		public void Consume()
		{
			if (!dead) // Already dead
			{
				dead      = true;
				deathLife = life;
				deathMode = Deaths.Consume;
				life      = 0;
				battle.RemoveState(this);
			}
		}

		public void AddStatees([CanBeNull] IStatee statee)
		{
			if (statee == null)
			{
				this.LogError("Cannot add null fighter to statees.");
				return;
			}

			if (statees.Contains(statee))
				return;

			statees.Add(statee);
			hasSpecifiedTargets = true;
		}

		public void AddStatees([NotNull] List<Fighter> fighters)
		{
			for (var i = 0; i < statees.Count; i++)
			{
				Fighter ftr = fighters[i];
				if (statees.Contains(ftr)) continue;
				statees.Add(ftr);
			}

			hasSpecifiedTargets = true;
		}

		[UsedImplicitly]
		public override void ConfigureDV(DynValue dv)
		{
			if (dv == null) return;
			if (dv.IsNil()) this.LogError("Configure(dv) argument is nil.");
			else if (dv.AsTable(out Table tb)) ConfigureTB(tb);
			else if (dv.AsString(out string iget)) ConfigureDV(GetEnv().iget(iget));
			else if (dv.AsEnum(out EngineFlags eflag)) Add(new StateCmd(eflag));
			else if (dv.AsUserdata(out Trigger triggerEntry)) Add(triggerEntry);
			else if (dv.AsExact(out StateFunc mod)) Add(mod);
			else if (dv.AsExact(out StateCmd prop)) Add(prop);
			else if (dv.AsExact(out TurnFunc turnop)) Add(turnop);
			else if (dv.AsExact(out VFX vfx)) Add(vfx);
			else if (dv.AsExact(out Targeting targeting))
			{
				if (targeting.picks.Count > 0)
				{
					var slots = targeting.picks[0].slots;

					foreach (Slot slot in slots)
					{
						AddStatees(slot);
					}

					slotRegistrar = true;
				}
			}
			else if (dv.AsExact(out Fighter fter)) AddStatees(fter);
			else if (dv.AsExact(out Slot slot))
			{
				AddStatees(slot);
				slotRegistrar = true;
			}
			else
				this.LogError($"Configure(dv) argument is not a valid type: {dv.Type},{dv.UserData?.Object.GetType().Name}");
		}


		public bool has_tag(string tag)
		{
			return tags.Contains(tag);
		}

		private static void AddTriggerDV([NotNull] DynValue dv, Signals default_signal)
		{
			_tmpTriggers.Clear();
			if (dv.IsNil()) return;
			else if (dv.AsExact(out Trigger trig)) _tmpTriggers.Add(trig);
			else if (dv.AsFunction(out Closure cl)) _tmpTriggers.Add(new Trigger(default_signal).AddHandlerDV(cl));
			else if (dv.AsObject(out Signals signal)) _tmpTriggers.Add(new Trigger(signal));
			else if (dv.AsTable(out Table tblist))
				for (var i = 1; i <= tblist.Length; i++)
					AddTriggerDV(tblist.Get(i), default_signal);
		}

		public void AddEnableDV([NotNull] DynValue dv)
		{
			AddTriggerDV(dv, Signals.enable);
			foreach (Trigger tg in _tmpTriggers)
				AddEnable(tg);
			_tmpTriggers.Clear();
		}

		public void AddDecayDV([NotNull] DynValue dv)
		{
			AddTriggerDV(dv, Signals.decay);
			foreach (Trigger tg in _tmpTriggers)
				AddDecay(tg);
			_tmpTriggers.Clear();
		}

		public void AddExpireDV(DynValue dv)
		{
			AddTriggerDV(dv, Signals.expire);
			foreach (Trigger tg in _tmpTriggers)
				AddExpire(tg);
			_tmpTriggers.Clear();
		}

		public void AddConsumeDV(DynValue dv)
		{
			AddTriggerDV(dv, Signals.consume);
			foreach (Trigger tg in _tmpTriggers)
				AddConsume(tg);
			_tmpTriggers.Clear();
		}

		public override void ConfigureTB([CanBeNull] Table tb)
		{
			if (tb == null) return;

			// Setup the state from a table
			ID = tb.TryGet("id", ID);

			if (tb.TryGet("life", out int maxLife))
			{
				maxlife = maxLife;
				Refresh();
			}

			stackMax      = tb.TryGet("stack_max", stackMax);
			stackRefresh  = tb.TryGet("stack_refresh", stackRefresh);
			enableUI      = tb.TryGet("enable_UI", enableUI); // Deprecated
			enableUI      = tb.TryGet("ui", enableUI);
			slotRegistrar = tb.TryGet("slot", slotRegistrar);

			// Clean API to pass decay triggers
			AddEnableDV(tb.Get("enable"));
			AddDecayDV(tb.Get("decay"));
			AddExpireDV(tb.Get("expire"));
			AddConsumeDV(tb.Get("consume"));

			if (tb.TryGet("props", out Table tbl))
				foreach (TablePair pair in tbl.Pairs)
					props.Set(pair.Key, pair.Value);

			if (tb.TryGet("tag", out DynValue dvtag))
			{
				switch (dvtag.Type)
				{
					case DataType.Table:
					{
						Table tagtable = dvtag.Table;
						for (var i = 1; i <= tagtable.Length; i++)
						{
							DynValue dv = tagtable.Get(i);
							tags.Add(dv.String);
						}

						break;
					}

					case DataType.String:
					{
						if (dvtag.AsString(out string tagname))
							tags.Add(tagname);
						break;
					}
				}
			}

			VFX tint = CombatAPI.ReadTintVFX(tb.Get("tint"));
			VFX fill = CombatAPI.ReadFillVFX(tb.Get("fill"));

			if (tint != null) Add(tint);
			if (fill != null) Add(fill);

			for (var i = 1; i <= tb.Length; i++)
				ConfigureDV(tb.Get(i));
		}


		/// <summary>
		/// Decay at the end of every action.
		/// </summary>
		/// <returns></returns>
		public static Trigger MakeDefaultDecay() => MakeDefaultDecaySignal().AddHandlerDV(Trigger.BuffDecaySingleton);

		/// <summary>
		/// Decay at the end of every action.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static Trigger MakeDefaultDecaySignal() => new Trigger(DefaultDecaySignal);

		public const Signals DefaultDecaySignal = Signals.end_turn;

		public void SetDealer(Fighter fighter)
		{
			dealer     = fighter;
			env.dealer = fighter;
		}

		public bool has_tags([NotNull] Table tbl)
		{
			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue delem = tbl.Get(i);

				if (!delem.AsObject(out string str))
				{
					delem.UnexpectedTypeError("BuffEvent.HasTags");
					continue;
				}

				if (!tags.Contains(str))
					return false;
			}

			return true;
		}

		public bool has_tag([NotNull] Table tbl)
		{
			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue delem = tbl.Get(i);

				if (!delem.AsObject(out string str))
				{
					delem.UnexpectedTypeError("BuffEvent.has_tag");
					continue;
				}

				if (tags.Contains(str))
					return true;
			}

			return false;
		}
	}
}
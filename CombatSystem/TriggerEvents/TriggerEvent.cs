using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Combat.Features.TurnOrder;
using Combat.Toolkit;
using JetBrains.Annotations;
using UnityEngine;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat.Data
{
	[LuaUserdata(Descendants = true)]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TriggerEvent
	{
		/// <summary>
		/// The object which is performing the signal's verb.
		/// Typically is a fighter.
		/// </summary>
		[CanBeNull]
		public object me;

		/// <summary>
		/// The object representing the signal's noun.
		/// </summary>
		public object noun;

		/// <summary>
		/// The resource which is causing this event.
		/// </summary>
		[CanBeNull]
		public BattleResource catalyst;

		/// <summary>
		/// Whether or not the event is cancelable.
		/// Cancellation must be handled by the code which emits the signal.
		/// accept-* signals are typically the ones which can be canceled.
		/// </summary>
		public bool cancelable;

		/// <summary>
		/// Whether or not the event is canceled.
		/// This is the value that will be handled externally to cancel the verb.
		/// </summary>
		public bool canceled;

		public List<string> coflags = new List<string>();
		public List<string> coskips = new List<string>();

		public TriggerEvent() { }

		public TriggerEvent(object me)
		{
			this.me = me;
		}

		/// <summary>
		/// Mark the event as canceled. Only relevant for certain events which implement cancellation.
		/// Typically, the events that can be canceled are 'accept-*' signals.
		/// Past tense is never cancelable. (2real4me)
		/// </summary>
		/// <returns></returns>
		public bool Cancel()
		{
			bool saved = canceled;

			if (canceled)
			{
				Reset();
				canceled = true;
			}

			return saved;
		}

		//public bool Cancel(bool b) => b && Cancel();
		public bool Cancel(bool b)
		{
			canceled = b;

			return Cancel();
		}

		/// <summary>
		/// Get the first state in the chain, otherwise the trigger.
		/// The logic is that in the context of a trigger packaged within a state,
		/// it's more typical the treat the trigger as being an inherent property of
		/// the state rather than its own actor which can decay or expire separately.
		/// </summary>
		[CanBeNull]
		public BattleResource auto => (BattleResource)FindState() ?? FindTrigger();

		public State buff
		{
			get
			{
				State s = state;
				return s?.tags.Contains("bad") == true ? s : null;
			}
		}

		public State debuff
		{
			get
			{
				State s = state;
				return s?.tags.Contains("bad") == true ? s : null;
			}
		}


		/// <summary>
		/// Get the first state in the chain.
		/// </summary>
		[CanBeNull]
		public State state
		{
			get
			{
				State state = FindState();

				if (state == null)
				{
					Debug.LogWarning("Could not find any parent in the chain for 'state'");
					return null;
				}

				return state;
			}
		}

		/// <summary>
		/// Get the first trigger in the chain
		/// </summary>
		[CanBeNull]
		public Trigger trigger
		{
			get
			{
				Trigger trig = FindTrigger();
				if (trig == null)
				{
					Debug.LogWarning("Could not find any parent in the chain for 'trigger'");
					return null;
				}

				return trig;
			}
		}

		public int life
		{
			get => FindState()?.life ?? FindTrigger().life;
			set
			{
				State state = FindState();
				if (state != null)
				{
					state.life = value;
					return;
				}

				Trigger trigger = FindTrigger();
				if (trigger != null)
				{
					trigger.life = value;
					return;
				}
			}
		}

		/// <summary>
		/// Get the statee for the first state in the chain.
		/// </summary>
		[CanBeNull]
		public object statee
		{
			get
			{
				State state = this.state;
				if (state == null)
				{
					Debug.LogWarning("Could not find any parent in the chain for 'statee'");
					return null;
				}

				if (state.statees.Count > 0)
					this.LogWarn("Using 'statee' property to access a state that has several statees.");

				return state.statees.FirstOrDefault();
			}
		}

		/// <summary>
		/// Get the statees for the first state in the chain.
		/// </summary>
		public object statees
		{
			get
			{
				State state = this.state;
				if (state != null)
					return state.statees;

				return null;
			}
		}

		public virtual void Reset()
		{
			coflags.Clear();
			coskips.Clear();
			canceled = false;
		}

		public void decay()
		{
			BattleResource p = auto;
			switch (p)
			{
				case State state:
					state.Decay();
					return;

				case Trigger trig:
					trig.Decay();
					return;
			}

			Debug.LogWarning("Could not find any parent in the chain for 'decay()'");
		}

		public void decay(bool b)
		{
			if (b) decay();
		}

		public void decay(string id)
		{
			throw new NotImplementedException(); // TODO
		}

		public void expire()
		{
			BattleResource p = auto;

			switch (p)
			{
				case State state:
					state.Expire();
					return;

				case Trigger trig:
					trig.Expire();
					return;
			}

			Debug.LogWarning("Could not find any parent in the chain for 'expire()'");
		}

		public void expire(bool b)
		{
			if (b) expire();
		}

		public void expire(string id)
		{
			throw new NotImplementedException(); // TODO
		}

		public void consume()
		{
			BattleResource p = auto;

			switch (p)
			{
				case State state:
					state.Consume();
					return;
			}

			Debug.LogWarning("Could not find any parent in the chain for 'consume()'");
		}

		public bool expired
		{
			get
			{
				BattleResource p = auto;

				switch (p)
				{
					case State state:  return state.life == 0;
					case Trigger trig: return trig.life == 0;
					default:
						Debug.LogWarning("Could not find any parent in the chain for 'expired'");
						return false;
				}
			}
		}

		public bool alive
		{
			get
			{
				BattleResource p = auto;

				switch (p)
				{
					case State state:  return state.IsAlive;
					case Trigger trig: return trig.IsAlive;
					default:
						Debug.LogWarning("Could not find any parent in the chain for 'expired'");
						return false;
				}
			}
		}

		[CanBeNull]
		public Fighter fighter
		{
			get
			{
				if (me is Fighter fter) return fter;
				if (me is Action turn && turn.acter is Fighter turnFighter) return turnFighter;
				return null;
			}
		}

		[CanBeNull]
		private State FindState()
		{
			BattleResource p     = catalyst;
			State          state = null;

			while (p != null)
			{
				state = p as State;
				if (state != null)
					break;

				p = catalyst.Parent;
			}

			return state;
		}

		[CanBeNull]
		private Trigger FindTrigger()
		{
			BattleResource p    = catalyst;
			Trigger        trig = null;

			while (p != null)
			{
				trig = p as Trigger;
				if (trig != null)
					break;

				p = catalyst.Parent;
			}

			return trig;
		}

		public void coflag(string flag)
		{
			coflags.Add(flag);
		}


		public void coskip(string flag)
		{
			coskips.Add(flag);
		}
	}
}
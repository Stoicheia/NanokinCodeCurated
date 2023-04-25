using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Proxies
{
	// TODO we cannot access a trigger's id in a script because of the id(...) function.
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TriggerProxy : LuaProxy<Trigger>
	{
		public int life
		{
			get => proxy.life;
			set => proxy.life = value;
		}

		public bool timer
		{
			get => proxy.isTimer;
			set => proxy.isTimer = value;
		}

		public bool enabled
		{
			get => proxy.enabled;
			set => proxy.enabled = value;
		}

		public bool expired => proxy.life == 0;

		public bool alive => proxy.IsAlive;

		public void refresh(DynValue dv)
		{
			if (dv.IsNil())
				proxy.Refresh();
			else if (dv.AsInt(out int life))
				proxy.Refresh(life);
			else
			{
				Debug.LogError($"Invalid argument for refresh: {dv}");
			}
		}

		public Trigger id(string id)
		{
			proxy.ID = id;
			return proxy;
		}

		public Trigger act(Proc proc)
		{
			proc.Parent = proxy;
			proxy.AddHandlerDV(new Trigger.Handler(proc));
			return proxy;
		}

		public Trigger ui(bool b = true)
		{
			proxy.enableTurnUI = b;
			return proxy;
		}

		[NotNull]
		public Trigger act(ProcEffect effect)
		{
			proxy.AddHandlerDV(effect);
			return proxy;
		}

		[NotNull]
		public Trigger act(Table tbl)
		{
			var effects = new List<ProcEffect>();

			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);
				if (dv.AsObject(out ProcEffect effect))
				{
					effects.Add(effect);
				}
			}

			proxy.AddHandlerDV(new Trigger.Handler(effects));
			return proxy;
		}

		public Trigger act([CanBeNull] Closure func)
		{
			if (func != null)
				proxy.AddHandlerDV(new Trigger.Handler(Trigger.HandlerType.action, func));

			return proxy;
		}

		public Trigger anim([CanBeNull] Closure func)
		{
			if (func != null)
				proxy.anim = func;

			return proxy;
		}

		public Trigger anim([CanBeNull] string id)
		{
			if (id != null)
				proxy.id_anim = id;

			return proxy;
		}

		public void signal(Signals signal)
		{
			proxy.signal = signal;
		}

		public void filter(DynValue dv)
		{
			proxy.Filter(dv);
		}

		public void prepend(DynValue dv)
		{
			proxy.PrependDV(dv);
		}

		public void configure([CanBeNull] Table tb)
		{
			proxy.ConfigureTB(tb);
		}

		public void add_clause(DynValue dv)
		{
			proxy.AddHandlerDV(dv);
		}
	}
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anjin.Scripting;
using Combat.Data;
using Combat.Data.VFXs;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Proxies
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class StateProxy : LuaProxy<State>
	{
		public string id
		{
			get => proxy.ID;
			set => proxy.ID = value;
		}

		public int life
		{
			get => proxy.life;
			set => proxy.life = value;
		}

		public int max_life
		{
			get => proxy.maxlife;
			set => proxy.maxlife = value;
		}

		public Fighter dealer => proxy.dealer;

		public IStatee statee => proxy.statees.First(); // TODO rename to statee

		public ProcStatus pstats => proxy.pstats;

		public bool expired => proxy.life == 0;
		public bool alive   => proxy.life > 0 || proxy.life == -1;

		public DynValue this[string key]
		{
			get => proxy.props.Get(key);
			set => proxy.props.Set(key, value);
		}

		public Table props => proxy.props;

		public DynValue fx
		{
			get => proxy.props.Get("fx");
			set => proxy.props.Set("fx", value);
		}

		public DynValue this[int key]
		{
			get => proxy.props.Get(key);
			set => proxy.props.Set(key, value);
		}

		public State ui(bool enable)
		{
			proxy.enableUI = enable;
			return proxy;
		}

		public DynValue get(string key) => proxy.props.Get(key);

		public void set(string key, DynValue dv)
		{
			proxy.props.Set(key, dv);
		}

		public bool has_tag(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null) => proxy.has_tag(t1, t2, t3, t4, t5, t6);

		public bool has_tags(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null) => proxy.has_tag(t1, t2, t3, t4, t5, t6);

		public bool has_tag(Table tbl) => proxy.has_tag(tbl);

		public bool has_tags(Table tbl) => proxy.has_tags(tbl);

		public State add_tag(string tag)
		{
			proxy.tags.Add(tag);
			return proxy;
		}

		public State add_statee(string s)
		{
			DynValue dv = proxy.GetEnv().iget(s);
			if (dv.AsObject(out Fighter ft))
				add_statee(dv.AsObject(ft));
			else if (dv.AsObject(out Slot slot))
				add_statee(dv.AsObject(slot));

			return proxy;
		}

		public State add_statee(Fighter statee)
		{
			if (statee == null) return proxy;
			proxy.AddStatees(statee);
			return proxy;
		}

		public State add_statee(Slot slot)
		{
			if (slot?.owner == null) return proxy;
			proxy.AddStatees(slot.owner);
			return proxy;
		}

		public void refresh()
		{
			proxy.Refresh();
		}

		public void decay()
		{
			proxy.Decay();
		}

		public void expire()
		{
			proxy.Expire();
		}

		public void replace()
		{
			proxy.enableUI = false;
			proxy.Expire();
		}

		public void consume()
		{
			proxy.Consume();
		}

		public State chance(float chance)
		{
			proxy.chance = chance;
			return proxy;
		}

		public void scale_life(float scale)
		{
			proxy.life    = Mathf.FloorToInt(proxy.life * scale);
			proxy.maxlife = Mathf.FloorToInt(proxy.maxlife * scale);
		}

		public State configure([CanBeNull] List<VFX> vfx)
		{
			if (vfx == null) return proxy;
			proxy.vfx.AddRange(vfx);
			return proxy;
		}

		public State configure(Table tbl)
		{
			proxy.ConfigureTB(tbl);
			return proxy;
		}

		public State configure(DynValue dv)
		{
			proxy.ConfigureDV(dv);
			return proxy;
		}

		public override string ToString() => $"{nameof(StateProxy)}({proxy})";
	}
}
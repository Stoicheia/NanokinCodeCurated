using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.Proxies
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ProcProxy : LuaProxy<Proc>
	{
		public string id
		{
			get => proxy.ID;
			set => proxy.ID = value;
		}

		[CanBeNull]
		public Fighter dealer
		{
			get => proxy.dealer;
			set => proxy.dealer = value;
		}

		public Vector3 pos => proxy.pos;

		public Vector3 center => proxy.center;

		public List<Fighter> Fighters => proxy.fighters;

		public Fighter victim => proxy.fighters[0];

		public List<ProcEffect> effects
		{
			get => proxy.effects;
			set => proxy.effects = value;
		}

		// public bool autotarget
		// {
		// 	get => proxy.allowAutoTarget;
		// 	set => proxy.allowAutoTarget = value;
		// }

		public bool has(string eff)
		{
			return proxy.effects.Any(e => e.ToString().ToLower().Contains(eff));
		}

		public Proc add_victim([CanBeNull] Table table)
		{
			if (table == null) return proxy;
			for (var i = 1; i <= table.Length; i++)
			{
				DynValue dv = table.Get(i);
				if (dv.AsObject(out Fighter fighter))
				{
					proxy.AddVictim(fighter);
				}
				else if (dv.AsObject(out Slot slot))
				{
					proxy.AddVictim(slot);
				}
			}

			return proxy;
		}

		public Proc add_victim([CanBeNull] Fighter fighter)
		{
			if (fighter == null) return proxy;
			proxy.AddVictim(fighter);
			return proxy;
		}

		public Proc add_victim([CanBeNull] Battle battle)
		{
			if (battle == null) return proxy;

			foreach (Fighter fter in battle.fighters)
			{
				add_victim(fter);
			}

			return proxy;
		}

		public Proc remove_victim([CanBeNull] Fighter fighter)
		{
			if (fighter == null) return proxy;
			proxy.RemoveVictim(fighter);
			return proxy;
		}

		public Proc remove_dealer()
		{
			proxy.RemoveVictim(dealer);
			return proxy;
		}

		public Proc add_victim([CanBeNull] Target target)
		{
			if (target == null) return proxy;

			proxy.AddVictims(target.slots);
			proxy.AddVictims(target.fighters);

			return proxy;
		}

		public Proc add_victim([CanBeNull] Slot slot)
		{
			if (slot == null) return proxy;
			proxy.AddVictim(slot);
			return proxy;
		}

		public void configure([NotNull] State state)
		{
			state.Parent = proxy;
			proxy.AddEffect(state);
		}

		public void configure([CanBeNull] Target target)
		{
			if (target == null) return;
			foreach (Fighter targetFighter in target.fighters)
				proxy.AddVictim(targetFighter);

			foreach (Slot slot in target.slots)
			{
				if (slot.owner != null)
				{
					add_tag("slot_target");
					proxy.AddVictim(slot.owner);
				}
			}
		}

		private void add_tag(string tag)
		{
			proxy.tags = proxy.tags ?? new List<string>();
			proxy.tags.Add(tag);
		}


		public void configure([NotNull] Table tb)
		{
			proxy.ConfigureTB(tb);
		}

		public void configure(DynValue dv)
		{
			proxy.ConfigureDV(dv);
		}

	#region Offset Positioning Functions
		public WorldPoint anchor(string id) => proxy.anchor(id);

		public WorldPoint rel_offset(Vector3 from, float fwd, float y, float horizontal) => proxy.rel_offset(from, fwd, y, horizontal);

		public WorldPoint xy_offset(float fwd, float y, float horizontal = 0) => proxy.xy_offset(fwd, y, horizontal);

		public WorldPoint offset(float z, float y, float x = 0) => proxy.offset(z, y, x);

		public Vector3 offset3(float z, float y, float x) => proxy.offset3(z, y, x);

		public WorldPoint identity_offset(float d) => proxy.identity_offset(d);

		public WorldPoint polar_offset(float rad, float angle, float horizontal = 0) => proxy.polar_offset(rad, angle, horizontal);

		public WorldPoint ahead(float distance, float horizontal = 0) => proxy.ahead(distance, horizontal);

		public WorldPoint behind(float distance, float horizontal = 0) => proxy.behind(distance, horizontal);

		public WorldPoint above(float distance = 0) => proxy.above(distance);

		public WorldPoint under(float distance) => proxy.under(distance);
	#endregion
	}
}
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class BattleProxy : LuaProxy<Battle>
	{
		[CanBeNull]
		public Arena arena => proxy.arena;

		public List<Slot>         slots    => proxy.slots;
		public List<State>        States   => proxy.states;
		public List<Trigger>      triggers => proxy.triggers;
		public List<Fighter>      fighters => proxy.fighters;
		public List<Battle.Death> deaths   => proxy.deaths;
		public List<Team>         teams    => proxy.teams;
		public int                round_id => proxy.RoundID;

		public Vector3 center   => arena.transform.position;
		public Vector3 pos      => arena.transform.position;
		public Vector3 position => arena.transform.position;


		[CanBeNull] public Slot get_slot(int x, int y) => proxy.GetSlot(new Vector2Int(x, y));

		public Slot get_slot(DynValue dv)
		{
			if (dv.AsUserdata(out Vector2Int v2i)) return proxy.GetSlot(v2i);
			if (dv.AsObject(out Fighter fighter)) return proxy.GetSlot(fighter);
			if (dv.AsObject(out Slot slot)) return slot;

			return null;
		}


		public bool has_flag(Fighter fighter, EngineFlags flag) => proxy.HasFlag(fighter, flag);
		public bool has_flag(Slot    slot,    EngineFlags flag) => proxy.HasFlag(slot, flag);

		public bool is_ally(object  me, object other) => proxy.GetTeam(me) == proxy.GetTeam(other);
		public bool is_enemy(object me, object other) => proxy.GetTeam(me) != proxy.GetTeam(other);
		public bool cam_motions() => proxy.cameraMotions;

		[NotNull] public IEnumerable<State> states_by_tag(string            tag)                           => proxy.GetStatesByTag(tag);
		[NotNull] public IEnumerable<State> states_by_tag([NotNull] Fighter fighter, [NotNull] string tag) => proxy.GetStatesByTag(fighter, tag);
		[NotNull] public IEnumerable<State> states_by_key([NotNull] string  key)                           => proxy.GetStatesByID(key);
		[NotNull] public IEnumerable<State> buffs_by_key([NotNull]  Fighter fighter, [NotNull] string key) => proxy.GetStatesByID(fighter, key);

		public bool has_buff(Fighter target, string key) => proxy.HasState(target, key);
		public bool has_buff(Slot    target, string key) => proxy.HasState(target, key);

		public bool has_tag(Fighter target, string key) => proxy.HasTag(target, key);

		public int GetStackCount([NotNull] Fighter fighter, [NotNull] string key) => proxy.CountStates(fighter, key);

		public void mark_revival([NotNull] Fighter corpse, float healAmount) => proxy.MarkRevival(corpse, healAmount);

		public Pointf   get_points([NotNull]       Fighter     holder)  => holder.points;
		public Pointf   get_maxpoints([NotNull]    Fighter     victim)  => proxy.GetMaxPoints(victim);
		public Statf    get_stats([NotNull]        Fighter     fighter) => proxy.GetStats(fighter);
		public Elementf get_efficiencies([NotNull] Fighter     fighter) => proxy.GetResistance(fighter);
		public Pointf   get_skillcost([NotNull]    BattleSkill skill)   => proxy.GetSkillCost(skill);
		public Status   get_buffstate([NotNull]    Fighter     fighter) => fighter.status;

		public bool status_applied([NotNull] Fighter fighter, DynValue dv) => proxy.StatusApplied(fighter, dv);

		[CanBeNull] public Team GetTeam(object       member)  => proxy.GetTeam(member); [CanBeNull] public Fighter GetSlotOwner(Slot slot) => proxy.GetOwner(slot);
		[CanBeNull] public Slot GetOwnedSlot(Fighter fighter) => proxy.GetHome(fighter);

		public void ClaimSlot([NotNull] Fighter owner, [NotNull] Slot slot) => slot.battle.SetHome(owner, slot);

		// public             void                 FreeSlot([NotNull]  Fighter owner)          => proxy.SetS(owner);
		// public             void                 FreeSlot([NotNull]  Slot    slot)           => slot.FreeSlot();
		public Battle.SlotSwap Swap([NotNull] Fighter me, Slot slot2) => proxy.SwapHome(me, slot2);

		public float GetComboCount(Fighter             fighter)                    => proxy.GetComboCount(fighter);
		public void  AddToComboCount([NotNull] Fighter entity, float value = 0.5f) => proxy.IncrementCombo(entity, value);
	} }
using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using Combat.Data.VFXs;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Unity.Mathematics;
using UnityEngine;

namespace Combat
{
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Slot : ILogger, ITargetable, IStatee
	{
		/// <summary>
		/// Custom tags that can be customized to do filtering on the slots
		/// and implement specialized slots or mechanics.
		/// </summary>
		[NotNull]
		public readonly List<string> tags = new List<string>();

		/// <summary>
		/// Should highlighting of this slot persist? (i.e. for selecting one slot at a time for certain skills)
		/// </summary>
		public bool persistentHighlight = false;

		/// <summary>
		/// The battle this slot is part of.
		/// </summary>
		public Battle battle;

		/// <summary>
		/// The grid coordinate mapped to this slot.
		/// </summary>
		public Vector2Int coord;

		/// <summary>
		/// Forward direction of the slot. (grid space)
		/// </summary>
		public Vector2Int forward;

		/// <summary>
		/// World-space position of the slot.
		/// </summary>
		public Vector3 position;

		/// <summary>
		/// World-space facing direction of the slot.
		/// </summary>
		public Vector3 facing;

		/// <summary>
		/// The group the slot is contained in (if any)
		/// </summary>
		[CanBeNull]
		public SlotGrid grid;

		/// <summary>
		/// The team this slot is part of.
		/// </summary>
		[CanBeNull]
		public Team team;

		/// <summary>
		/// Current owner of the slot.
		/// </summary>
		[CanBeNull]
		public Fighter owner;

		/// <summary>
		/// A physical view of the slot in the world.
		/// </summary>
		[CanBeNull]
		public BattleActor actor;

		public List<State> states = new List<State>();

		public Status status = new Status();

		public Slot() { }
		public Slot(int2 coord, int2 forward) : this(new Vector2Int(coord.x, coord.y), new Vector2Int(forward.x, forward.y)) { }

		public Slot(Vector2Int coord, Vector2Int forward)
		{
			this.coord   = coord;
			this.forward = forward;
		}

		[UsedImplicitly]
		public bool taken => owner != null;

		public int x => coord.x;
		public int y => coord.y;

		[UsedImplicitly]
		public Vector3 gcenter => grid.center;

		[UsedImplicitly]
		public Vector3 gcenter_xz => grid.center_xz;


		public WorldPoint identity_offset(float d)                                           => actor.identity_offset(d);
		public WorldPoint polar_offset(float    rad,      float angle, float horizontal = 0) => actor.polar_offset(rad, angle, horizontal);
		public WorldPoint ahead(float           distance, float horizontal = 0) => actor.ahead(distance, horizontal);
		public WorldPoint behind(float          distance, float horizontal = 0) => actor.behind(distance, horizontal);
		public WorldPoint above(float           distance = 0) => actor.above(distance);
		public WorldPoint under(float           distance)     => actor.under(distance);

		[UsedImplicitly]
		public Vector3 offset(float z)
		{
			return position + facing * z;
		}

		[UsedImplicitly]
		[Obsolete]
		public Vector3 xy_offset(float z, float y)
		{
			// Technically this is zy_offset
			return position + facing * z + y * new Vector3(0, 6, 0);
		}

		[UsedImplicitly]
		[Obsolete]
		public Vector3 xyz_offset(float z, float y, float x)
		{
			return position + facing * z + y * new Vector3(0, 6, 0) + x * Vector3.Cross(facing, new Vector3(0, 1, 0)).normalized;
		}

		public Vector3 offset(float z, float y)
		{
			return position + facing * z + y * new Vector3(0, 1, 0);
		}

		[UsedImplicitly]
		public Vector3 offset(float z, float y, float x)
		{
			return position + facing * z + y * new Vector3(0, 1, 0) + x * Vector3.Cross(facing, new Vector3(0, 1, 0)).normalized;
		}

		[UsedImplicitly] //For some reason Lua doesn't like default variables?
		public Vector3 offset3(float z, float y, float x)
		{
			return actor.center + facing * z + actor.Up * y + Vector3.Cross(facing, Vector3.up) * x;
		}

	#region Logging

		private bool _logSilent;

		public string LogID { get; }

		public bool LogSilenced => battle.LogSilenced;

		public override string ToString() => $"Slot({coord.x}, {coord.y})";

	#endregion

		// public Vector3 Position => actor != null ? actor.position : Vector3.zero;
		//
		// public Vector3 Center => actor != null ? actor.position : Vector3.zero;

		public void FreeSlot()
		{
			if (owner != null) owner.home = null;
			owner = null;
		}

		/// <summary>
		/// Check that the fighter has the specified state. (by id)
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[UsedImplicitly]
		public bool has_state(string id) => battle.HasState(this, id);

		/// <summary>
		/// Check that the fighter has the specified state. (by tag)
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		[UsedImplicitly]
		public bool has_tag(string tag) => battle.HasTag(this, tag);

		/// <summary>
		/// Get the states for a tag or id.
		/// </summary>
		/// <param name="tbl">Table of ID strings</param>
		/// <returns>Temporary result, do not cache it.</returns>
		[UsedImplicitly]
		public List<State> get_states([NotNull] string tag_or_id)
		{
			return battle.GetStates(this, tag_or_id);
		}

		[UsedImplicitly]
		public State get_state([NotNull] string tag_or_id)
		{
			List<State> states = battle.GetStates(this, tag_or_id);
			return states.Count > 0 ? states[0] : null;
		}

		public Vector3 GetTargetPosition() => Center;

		public           Vector3    GetTargetCenter() => Center;
		[NotNull] public GameObject GetTargetObject() => actor.gameObject;

		public WorldPoint Center => actor != null
			? new WorldPoint { mode = WorldPoint.WorldPointMode.GameObject, gameobject = actor.gameObject, actor = actor }
			: new WorldPoint(Vector3.zero);

		public List<State> States => states;
		public Status      Status => status;

		[CanBeNull]
		public BattleActor Actor => actor;

		public void AddVFX(VFX vfx) { }

		public void RemoveVFX(VFX vfx) { }
	}
}
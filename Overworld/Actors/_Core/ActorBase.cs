using System;
using Anjin.Util;
using Anjin.Utils;
using Combat.Data.VFXs;
using Drawing;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Util.Components;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	/// <summary>
	/// Most basic definition of an actor.
	/// Only specifies some properties that have to be programmed/assigned
	/// by the implementing actor.
	///
	/// A lot of functionalities only need this much, and so we can
	/// have a lot of common ground between combat fighters and overworld actors
	/// by basing both of them under this core definition of an actor.
	/// </summary>
	public class ActorBase : AnjinBehaviour
	{
		// STATE
		// ----------------------------------------
		[NonSerialized] public bool actorActive;

		/// <summary>
		/// Facing at the time of spawning.
		/// Can be considered the actor's default state.
		/// </summary>
		protected Vector3 initialFacing;

		/// <summary>
		/// Current facing direction of the character.
		/// Setting this directly has no effect for many actor types,
		/// and is available for tests and maths only.
		/// Use SetFacing instead.
		/// </summary>
		[NonSerialized]
		[ShowInPlay]
		[TitleGroup("Debug", order:1000)]
		public Vector3 facing = Vector3.forward;

		[NonSerialized]
		public Vector3 upward = Vector3.up;

		/// <summary>
		/// The current position.
		/// Setting this directly has no effect for many actor types,
		/// and is available for tests and maths only. Using this
		/// is the most performant way to get the position.
		/// </summary>
		[NonSerialized]
		public Vector3 position;

		/// <summary>
		/// The current velocity.
		/// Setting this directly has no effect for many actor types,
		/// and is available for tests and maths only.
		/// </summary>
		[NonSerialized]
		[TitleGroup("Debug")]
		public Vector3 velocity;

		/// <summary>
		/// Current facing direction of the character.
		/// Setting this directly has no effect for many actor types,
		/// and is available for tests and maths only.
		/// Use SetFacing instead.
		/// </summary>
		[NonSerialized]
		[ShowInPlay]
		[TitleGroup("Debug")]
		public Vector3 targetFacing = Vector3.forward;

		/// <summary>
		/// Center of the actor.
		/// Can be used for positioning.
		/// </summary>
		[NonSerialized]
		public Vector3 center;

		/// <summary>
		/// Radius of the actor.
		/// Can be used for positioning.
		/// </summary>
		[NonSerialized]
		public float radius = 0.5f;

		/// <summary>
		/// Height of the actor.
		/// Can be used for positioning.
		/// </summary>
		[NonSerialized]
		public float height = 1.5f;

		/// <summary>
		/// The actor's currently active brain.
		/// Do not assign directly.
		/// </summary>
		[ShowInPlay]
		[NonSerialized]
		[CanBeNull]
		public ActorBrain activeBrain;

		/// <summary>
		/// Whether the actor currently has an active brain.
		/// Do not assign directly.
		/// More efficient than null-checking activeBrain.
		/// </summary>
		[NonSerialized]
		public bool hasBrain;

		[NonSerialized]
		public int layer;

		/// <summary>
		/// Request that physics don't run for this actor.
		/// </summary>
		[NonSerialized]
		public bool disablePhysics;

		[NonSerialized]
		public TimeScalable timescale;
		[NonSerialized]
		public VFXManager vfx;

		[TitleGroup("Actor", order:-50)]
		public Transform visualTransform;

		public Vector3 Position => transform.position;

		public bool HasVelocity => !Mathf.Approximately(velocity.magnitude, 0);

		public virtual Vector3 Up => transform.up; // Good enough for most cases

		public virtual Vector3 VisualUp => visualTransform.up; // Good enough for most cases

		[TitleGroup("Debug")]
		[ShowInPlay]
		public virtual bool IsMotorStable => true;

		[TitleGroup("Debug")]
		[ShowInPlay]
		public virtual bool IsRunning => false;

		protected virtual void Awake()
		{
			if (timescale == null)
				timescale = gameObject.GetOrAddComponent<TimeScalable>();

			// TEMP, I am unsure whether the VFXManager is intended to be on the main actor object or the view
			if (vfx == null)
			{
				vfx = gameObject.GetComponentInChildren<VFXManager>();
				if (vfx == null) vfx = gameObject.GetOrAddComponent<VFXManager>();
			}

			facing = Vector3.forward;
			upward = Vector3.up;
		}

		protected virtual void Start()
		{
			center = position;
		}

		public void FaceTowards(Vector3 position)
		{
			if ((transform.position - position).magnitude < Mathf.Epsilon) return;
			facing = transform.position.Towards(position).normalized;
			upward = transform.up;
		}

		public virtual bool IsSameFacing(Vector3 lookpos)
		{
			const float fudging = 0.075f;
			return Vector3.Distance(transform.position, lookpos) < fudging;
		}

		protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
		private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			using (Draw.InLocalSpace(transform))
			{
				Draw.ArrowheadArc(float3.zero, facing, 0, Color.yellow);
			}
		}

		[CanBeNull]
		public virtual Transform GetAnchorTransform(string id)
		{
			return null;
		}

		[UsedImplicitly]
		[CanBeNull]
		public WorldPoint anchor(string id)
		{
			return GetAnchorTransform(id);
		}

		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		[Obsolete]
		public WorldPoint offset(float distance)
		{
			// local
			// return actor.center + facing * distance;
			return new WorldPoint(this)
			{
				offset     = new Vector3(0, 0, distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public WorldPoint rel_offset(Vector3 from, float fwd, float y, float horizontal)
		{
			// local
			// return actor.center + facing * distance;
			var wp = new WorldPoint(this)
			{
				offset     = y * Up + horizontal * (center + y * Up + fwd * facing - from).normalized + fwd * facing,
				offsetMode = WorldPoint.OffsetMode.World
			};
			return wp;
		}

		[UsedImplicitly]
		[Obsolete]
		public WorldPoint xy_offset(float fwd, float y, float horizontal = 0)
		{
			// local
			// return actor.center + facing * x + actor.Up * y * 6 + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(this)
			{
				offset     = new Vector3(horizontal, y * 6, fwd),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public WorldPoint offset(float x, float y, float horizontal = 0)
		{
			// local
			// return actor.center + facing * x + actor.Up * y + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(this, new Vector3(horizontal, y, x))
			{
				offset     = new Vector3(horizontal, y, x),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public WorldPoint identity_offset(float d)
		{
			// local
			// return actor.center + facing * d + actor.Up * d;
			return new WorldPoint(this)
			{
				offset     = new Vector3(0, d, d),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		[UsedImplicitly]
		public WorldPoint polar_offset(float rad, float angle, float horizontal = 0)
		{
			// polar local
			// return rad * (
			// 	       Mathf.Cos(angle * Mathf.Deg2Rad) * facing +
			// 	       Mathf.Sin(angle * Mathf.Deg2Rad) * actor.Up)
			//        + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(this)
			{
				offset     = new Vector3(horizontal, rad, angle),
				offsetMode = WorldPoint.OffsetMode.LocalPolar
			};
		}

		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		public WorldPoint ahead(float distance, float horizontal = 0)
		{
			// local
			// return actor.center + facing * distance + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(this)
			{
				offset     = new Vector3(horizontal, 0, distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point ahead or behind the fighter.
		/// </summary>
		[UsedImplicitly]
		public WorldPoint behind(float distance, float horizontal = 0)
		{
			// local
			// return actor.center - facing * distance + Vector3.Cross(facing, Vector3.up) * horizontal;
			return new WorldPoint(this)
			{
				offset     = new Vector3(horizontal, 0, -distance),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point above or under the fighter.
		/// </summary>
		[UsedImplicitly]
		public WorldPoint above(float distance = 0)
		{
			// local
			// return position + actor.Up * height + actor.Up * distance;
			return new WorldPoint(this)
			{
				offset     = new Vector3(0, distance, 0),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}

		/// <summary>
		/// Get a point above or under the fighter.
		/// </summary>
		[UsedImplicitly]
		public WorldPoint under(float distance)
		{
			// local
			// return position - actor.Up * distance;
			return new WorldPoint(this)
			{
				offset     = new Vector3(0, -distance, 0),
				offsetMode = WorldPoint.OffsetMode.Local
			};
		}
	}
}
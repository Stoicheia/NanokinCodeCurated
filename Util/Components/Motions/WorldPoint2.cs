using System;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Skills.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Utils
{
	/// <summary>
	/// A more complex worldpoint that can be made of 2 worldpoints.
	/// </summary>
	[Inline(true, true)]
	[DarkBox]
	[Serializable]
	[LuaUserdata]
	public struct WorldPoint2
	{
		public enum Types
		{
			/// <summary>
			/// The WP2 represents/is a proxy to a single WP.
			/// </summary>
			Point,

			/// <summary>
			/// The WP2 represents/is a proxy to a line between 2 WPs, the point on that line guided by the 'Midpoint' float.
			/// </summary>
			Midpoint,

			/// <summary>
			/// Is a special value that will proxy to the previous value of the variable when it is set. (keep)
			/// </summary>
			Previous
		}

		/// <summary>
		/// Specifies what the target points at.
		/// </summary>
		[HideInInspector]
		public Types Type;

		[OnValueChanged("OnPointChanged", true)]
		[SerializeField]
		[Inline]
		public WorldPoint P1;

		[NonSerialized]
		public WorldPoint P2;

		[NonSerialized]
		public float Midpoint;

		public WorldPoint2(WorldPoint wp) : this()
		{
			Type = Types.Point;
			P1   = wp;
		}

		public WorldPoint2(Vector3 pos) : this()
		{
			Type = Types.Point;
			P1   = new WorldPoint(pos);
		}

		public WorldPoint2([NotNull] GameObject go) : this()
		{
			Type = Types.Point;
			P1   = new WorldPoint(go);
		}

		public WorldPoint2([CanBeNull] Transform transform) : this()
		{
			Type = Types.Point;
			P1   = new WorldPoint(transform);
		}

		public WorldPoint2(WorldPoint start, WorldPoint end, float midpoint = 0.5f) : this()
		{
			Type     = Types.Midpoint;
			P1       = new WorldPoint(start);
			P2       = new WorldPoint(end);
			Midpoint = midpoint;
			// Type          = Types.Midpoint;
			// this.start    = start;
			// this.end      = end;
			// this.midpoint = midpoint;
		}

		public bool IsValid()
		{
			switch (Type)
			{
				case Types.Point:    return P1.TryGet(out Vector3 _);
				case Types.Midpoint: return P1.TryGet(out Vector3 _) && P2.TryGet(out Vector3 _);
				case Types.Previous: return true;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static implicit operator WorldPoint2(WorldPoint wp)
		{
			return new WorldPoint2(wp);
		}

		public static implicit operator WorldPoint2([CanBeNull] Transform t)
		{
			return new WorldPoint2(t);
		}

#if UNITY_EDITOR
		[UsedImplicitly]
		private void OnPointChanged()
		{
			Type = Types.Point;
		}
#endif
	}

	// [Serializable]
	// public struct WPTarget
	// {
	// 	// [Inline]
	// 	[SimpleWorldPoint]
	// 	[HideLabel]
	// 	[SerializeField]
	// 	public WorldPoint Value;
	//
	// 	/// <summary>
	// 	/// Offset for the point.
	// 	/// </summary>
	// 	[NonSerialized]
	// 	public Vector3 baseOffset;
	//
	// 	/// <summary>
	// 	/// Offset for the point.
	// 	/// </summary>
	// 	[NonSerialized]
	// 	public Vector3 localOffset;
	//
	// 	[NonSerialized] public bool       valid;
	// 	[NonSerialized] public GameObject gameObject;
	// 	[NonSerialized] public ActorBase  actor;
	// 	[NonSerialized] public bool       hasActor;
	//
	// 	public WPTarget([NotNull] ActorBase actor)
	// 	{
	// 		this.actor  = actor;
	// 		gameObject  = actor.gameObject;
	// 		hasActor    = true;
	// 		valid       = true;
	// 		Value       = WorldPoint.Default;
	// 		baseOffset  = Vector3.zero;
	// 		localOffset = Vector3.zero;
	// 	}
	// }
}
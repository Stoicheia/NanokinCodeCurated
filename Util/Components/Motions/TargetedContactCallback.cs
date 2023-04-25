using System;
using Anjin.Actors;
using Anjin.Scripting;
using JetBrains.Annotations;
using UnityEngine;

namespace Anjin.Utils
{
	[LuaUserdata]
	public class TargetedContactCallback
	{
		public int id;

		public enum Type
		{
			/// <summary>
			/// Contact by reaching some wyapoint goal which is the objCheck target here
			/// </summary>
			Waypoint,

			/// <summary>
			/// Contact by some radius/distance proximity check.
			/// </summary>
			Proximity,

			/// <summary>
			/// Contact using Unity OnTriggerEnter.
			/// </summary>
			Unity
		}

		/// <summary>
		/// Events to trigger on for a unity contact.
		/// </summary>
		public enum UnityEvents { Enter, Exit }

		/// <summary>
		/// Contact type.
		/// </summary>
		public Type type;

		/// <summary>
		/// ContactCallback to run
		/// </summary>
		public ContactCallback callback;

		// Targets
		// ----------------------------------------

		[CanBeNull]
		public string t_wpid;
		[CanBeNull]
		public GameObject t_object;
		[CanBeNull]
		public Vector3? t_pos;
		public float uradius = 0.875f;

		// Unity
		// ----------------------------------------
		public UnityEvents uevent = UnityEvents.Enter;

		// State data
		// ----------------------------------------
		public bool      hasActor;
		public ActorBase actor;

		public bool overlaped;

		public Vector3 Position
		{
			get
			{
				// TODO this is extremely weird
				switch (type)
				{
					case Type.Waypoint:
						return t_object.transform.position;
					case Type.Proximity:
						return t_pos.Value;
					case Type.Unity:
						return actor.transform.position;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (type == Type.Waypoint)
					return t_object.transform.position;
				else
					return t_pos ?? throw new InvalidOperationException();
			}
		}

		// ReSharper disable once RedundantCast
		public float? Radius
		{
			get
			{
				if (hasActor)
					return actor.radius;

				switch (type)
				{
					case Type.Waypoint:
						return (float?)null;
					case Type.Proximity:
						return null;
					case Type.Unity:
						return 0;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Cache some metadata about our checks.
		/// </summary>
		/// <param name="self"></param>
		public void InitCache([NotNull] GameObject self)
		{
			hasActor = t_object != null && t_object.TryGetComponent(out actor);
			if (self.TryGetComponent(out Collider c))
			{
				uradius = c.bounds.extents.magnitude;
			}
		}
	}
}
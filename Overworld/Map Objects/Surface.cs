using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Nanokin.Map
{
	public class Surface : MonoBehaviour
	{
		public static readonly Dictionary<int, Surface> all = new Dictionary<int, Surface>();

		public enum Behaviors
		{
			/// <summary>
			/// Forces actors to be able to stand on the surface.
			/// </summary>
			ForceStand,

			/// <summary>
			/// Forces actor to slide on the surface.
			/// </summary>
			ForceSlope,

			/// <summary>
			/// Cannot gain any traction to the surface in any way, simply falls.
			/// </summary>
			ForceUnstable
		}

		public enum AccelerationDirections
		{
			/// <summary>
			/// Towards the slope of the surface. = (normal x up) x normal
			/// </summary>
			LocalSlope,

			/// <summary>
			/// Towards a world-space direction.
			/// </summary>
			World
		}

		/// <summary>
		/// How the surface should behaves.
		/// </summary>
		[EnumToggleButtons]
		public Behaviors Behavior;

		/// <summary>
		/// Acceleration
		/// </summary>
		[Space]
		public Vector3 Acceleration;

		/// <summary>
		/// The direction to accelerate towards.
		/// </summary>
		public AccelerationDirections AccelerationDirection;

		/// <summary>
		/// A friction multiplier to make the surface more slippery.
		/// </summary>
		[HideInInspector]
		[Space]
		// ReSharper disable once NotAccessedField.Global
		public float Friction = 1;

		// OLD
		// ----------------------------------------

		/// <summary>
		/// on = Force stable ground
		/// off = Force unstable ground
		///
		/// In reality we should have made this an enum but w.e, too late and I think the enum aint grow anyway
		/// </summary>
		[FormerlySerializedAs("IsStableGround")]
		[Obsolete]
		[HideInInspector]
		public bool ForceStable;

		private bool Migrated;

		private void OnEnable()
		{
			all.Add(gameObject.GetInstanceID(), this);
		}

		private void OnDisable()
		{
			all.Remove(gameObject.GetInstanceID());
		}

		private void OnValidate()
		{
			if (Migrated) return;

#pragma warning disable 612
			Behavior = ForceStable ? Behaviors.ForceStand : Behaviors.ForceUnstable;
			Migrated = true;
#pragma warning restore 612
		}
	}
}
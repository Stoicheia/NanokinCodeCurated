using System;
using Anjin.Util;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Anjin.Actors
{
	/// <summary>
	/// A utility struct for applying inertial force to a state.
	/// E.g. JumpState and DiveDashState
	/// </summary>
	public class InertiaForce
	{
		private readonly ActorKCC _actor;

		public  Settings settings;
		public  float    force;
		public  Vector3  direction;
		private float    _startForce;

		public InertiaForce(ActorKCC actor)
		{
			_actor = actor;
		}

		/// <summary>
		/// The current vector of inertia.
		/// </summary>
		public Vector3 inertiaVector;

		/// <summary>
		/// The current vector of control, which is usually added to the inertia vector.
		/// </summary>
		public Vector3 controlVector;

		public Vector3 vector;

		public void Reset(Settings settings, Vector3 vel)
		{
			Vector3 hvel = vel.Horizontal();
			Vector3 dir  = hvel.normalized;

			float convertedForce1 = hvel.magnitude * settings.VelocityToInertia;
			float convertedForce2 = settings.VelocityToInertia2?.Evaluate(hvel.magnitude) ?? 0;

			Reset(settings, dir, convertedForce1 + convertedForce2);
		}

		public void Reset(Settings settings, Vector3 direction, float force)
		{
			this.settings  = settings;
			this.direction = direction;
			this.force     = force.Maximum(settings.MaxInertiaFromVelocity);

			_startForce = force;

			controlVector = Vector3.zero;
		}

		/// <summary>
		/// Useful dot product to know how much the joystick is pointing towards the inertia.
		/// dot = 0.0 : The joystick is pushed to the opposite of the inertia.
		/// dot = 0.5 : The joystick is pushed 90 degree to the side of the jump direction.
		/// dot = 1.0 : The joystick is pushed perfectly towards the inertia.
		/// </summary>
		public float NormalizedJoystickInertiaDot
		{
			get
			{
				if (_actor.inputs.moveMagnitude < 0.1f)
					return 1;

				float angleDot = Vector3.Dot(_actor.inputs.move, direction); // [-1, 1]
				angleDot += 1;                                               // [0, 2]
				angleDot *= 0.5f;                                            // [0, 1]

				return angleDot;
			}
		}

		public void Update(float dt)
		{
			float dot            = NormalizedJoystickInertiaDot;
			float dot_circle     = (1 - dot).Minimum(Mathf.Epsilon);
			float dot_hemisphere = ((1 - dot) * 2).Maximum(1);

			// DECELERATION
			// ----------------------------------------
			float deceleration;
			if (_actor.inputs.hasMove)
			{
				float sharpenedDot = Mathf.Pow(dot_hemisphere.Minimum(0.001f), settings.JoystickSharpness);

				// Mix active and passive deceleration.
				deceleration = settings.ActiveDeceleration * sharpenedDot + settings.PassiveDeceleration * (1 - sharpenedDot);
			}
			else
			{
				deceleration = settings.PassiveDeceleration;
			}

			force -= deceleration * dt;
			force =  force.Minimum(0);


			// CONTROL
			// ----------------------------------------
			float amplitude = settings.ControlAmplitude + (settings.ControlAmplitudeByInertia?.Evaluate(_startForce) ?? 0);
			float influence = settings.ControlInfluenceRange.InverseLerp(1 - dot);

			if (amplitude > 0.01f)
			{
				// When the inertia is no longer fulfilling the full amplitude, ramp the influence back up
				float fulfillment = Mathf.Clamp01(force / amplitude);
				influence = Mathf.Lerp(influence, 1, 1 - fulfillment);
			}


			float cForce = amplitude * influence;

			// if (force < amplitude)
			// {
			// 	// When the force is no longer fulfilling the base control amplitude, ramp up the control
			// 	cForce = amplitude - force;
			// }

			// Debug.Log($"{force}, {cForce}, {amplitude}, {influence}");

			// FINAL UPDATE
			// ----------------------------------------

			inertiaVector = force * direction;
			controlVector = cForce * _actor.inputs.move;

			// Debug.Log($"{influence:0.00} | {cForce:0.00} | {inertiaVector.magnitude + controlVector.magnitude:0.00} | {amplitude}");

			vector = inertiaVector + controlVector;
		}

		[Serializable]
		public struct Settings
		{
			// public static Settings Default => new Settings
			// {
			// 	VelocityToInertia  = 0.75f,
			// 	VelocityToInertia2 = AnimationCurve.Linear(0, 0, 5, 0),
			//
			// 	PassiveDeceleration = 0.05f,
			// 	ActiveDeceleration  = 0.1f,
			//
			// 	ControlSharpness = 3,
			// 	ControlAmplitude = 1.5f,
			// 	ControlMinSpeed  = 0.125f
			// };

			public float          VelocityToInertia;
			public AnimationCurve VelocityToInertia2;

			[FormerlySerializedAs("MaxSpeedFromInertia"), FormerlySerializedAs("maxSpeedFromInertia")]
			public float MaxInertiaFromVelocity;

			[Space]
			[Tooltip("The inertia decelerates over time no matter what.")]
			public float PassiveDeceleration;

			[FormerlySerializedAs("deceleration")]
			[Tooltip("The inertia decelerates proportionally to the joystick in the opposite direction.")]
			public float ActiveDeceleration;

			[FormerlySerializedAs("controlAmplitude")]
			public float ControlAmplitude;

			[Tooltip("Dynamic control amplitude depending on the initial inertia. (does not change as the inertia decreases or increases)")]
			[FormerlySerializedAs("InertiaToControlAmplitude")]
			public AnimationCurve ControlAmplitudeByInertia;

			public FloatRange ControlInfluenceRange;

			[FormerlySerializedAs("ControlMinSpeed"), FormerlySerializedAs("controlMinSpeed")]
			public float ControlAmplitudeMinimum;


			[FormerlySerializedAs("ControlSharpness"), Space]
			[Tooltip("Allows curving the joystick direction.")]
			[FormerlySerializedAs("controlSharpness")]
			public float JoystickSharpness;

			public float GetDeceleration(float joystickDot)
			{
				float frontalDot = (joystickDot * 2).Maximum(1); // We want the entire frontal hemisphere to not cause deceleration.

				float sharpenedDot = Mathf.Pow(1 - frontalDot.Minimum(0.001f), JoystickSharpness);
				return ActiveDeceleration * sharpenedDot;
			}
		}
	}
}
using System;
using Anjin.Util;
using Drawing;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;

namespace Anjin.Actors
{
	public class SlideState : StateKCC
	{
		public readonly Settings settings;

		private Vector3 _lastDescentVector;
		private float   _descentSign;
		// private Vector3 _descentNormal;
		private Vector3 _groundNormal;
		private float   _groundDot;
		private float   _descent;

		public SlideState([NotNull] Settings settings)
		{
			this.settings                         = settings;
			settings.SpeedToTurnRate.preWrapMode  = WrapMode.ClampForever;
			settings.SpeedToTurnRate.postWrapMode = WrapMode.ClampForever;
		}

		public override bool IsGround => Motor.GroundingStatus.FoundAnyGround;
		public override bool IsAir    => !Motor.GroundingStatus.FoundAnyGround;

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			if (!Mathf.Approximately(actor.velocity.magnitude, 0))
				facing = actor.velocity.Change3(y: 0).normalized;

			// SLOPE DESCENT
			// ----------------------------------------
			if (IsGround)
			{
				float hspeed = actor.velocity.Horizontal().magnitude;

				Vector3 steerDir   = actor.slopeDir;
				float   steerSpeed = settings.SpeedToDescentTurnRate.Evaluate(hspeed);

				steerSpeed *= _descent;                                                               // Steer
				steerSpeed *= Mathf.Lerp(1f, 1 - settings.MaxDescentInfluence, inputs.moveMagnitude); // Allow player to take over

				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + steerDir.normalized * _descent, Color.green);

				if (!Mathf.Approximately(steerDir.magnitude, 0))
				{
					// Rotate towards descent
					RotateHoriz(ref facing, steerDir, steerSpeed, dt);
				}
			}
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float dt)
		{
			float   hspeed = currentVelocity.Horizontal().magnitude;
			Vector3 vvel   = currentVelocity.Vertical();
			Vector3 hdir   = actor.VelocityOrFacing.Horizontal().normalized;
			_groundNormal = Motor.GroundingStatus.GroundNormal;
			_groundDot    = Math3d.ProjectVectorOnPlane(_groundNormal, hdir).y;

			float frictionMultiplier = 1;
			if (actor.SpeedBoost)
				frictionMultiplier = settings.SpeedBoostFrictionMulti;

			// TODO get rotation speed
			// ^ slope angle = ^ descent rot
			// ^ speed = v descent rot
			_descent = settings.DescentCurve.Evaluate(1 - Mathf.Abs(-Vector3.Dot(actor.GravityVector.normalized, _groundNormal)));

			if (DebugSystem.Opened)
			{
				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + actor.slopeDir * (actor.Gravity * _descent / Time.fixedDeltaTime), Color.green);
				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + _groundNormal, Color.yellow);
				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + Vector3.Cross(actor.GravityVector, _groundNormal), Color.magenta);
				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + Vector3.up, ColorsXNA.Orange);
			}

			// MOMENTUM/SLOPE INTERACTION
			// ----------------------------------------
			if (IsGround)
			{
				Vector3 groundNormal    = Motor.GroundingStatus.GroundNormal;
				float   normalizedAngle = Math3d.ProjectVectorOnPlane(groundNormal, currentVelocity.normalized).y;
				hspeed += settings.SlopeConversion.Evaluate(normalizedAngle); // Speed up or speed down according to slope angle.

				if (airMetrics.justLanded)
				{
					float gain = settings.VerticalConversion.Evaluate(airMetrics.yDelta.Abs());
					hspeed += gain;
				}

				// Physical Acceleration
				// ----------------------------------------

				float angleToGround = Mathf.PI/2 - Math3d.AngleToGround(groundNormal, Vector3.up);
				float sinAngle = 0;
				float cosAngle = 0;
				if (!float.IsNaN(angleToGround))
				{
					cosAngle = Mathf.Cos(angleToGround);
					sinAngle = Mathf.Sin(angleToGround);
				}

				hspeed += settings.WheelAcceleration * Mathf.Max(0, sinAngle) * dt; //this part models wheel mechanics

				hspeed -= dt * settings.AirResistance * Mathf.Pow(Mathf.Abs(hspeed), settings.AirResistanceDamp); //this part models air resistance
				hspeed -= settings.CoefficientOfFriction * Physics.gravity.magnitude * actor.GravityVector.magnitude * cosAngle * frictionMultiplier * dt; //this part models ground friction (sort of)

				//hspeed -= settings.Deceleration * dt;
			}

			if (actor.SpeedBoostContact) {
				hspeed += settings.SpeedBoostContactAccel * dt;
			}

			hspeed = hspeed.Clamp(0, actor.SpeedBoost ? settings.SpeedBoostMaxSpeed : settings.SpeedMax);

			// MOMENTUM STEERING
			// ----------------------------------------
			if (inputs.hasMove)
			{
				var   steerDir   = Vector3.Slerp(currentVelocity.normalized, inputs.move, settings.JoystickCurve.Evaluate(actor.NormalizedJoystickFacingDot));
				float steerSpeed = settings.SpeedToTurnRate.Evaluate(hspeed);
				RotateHoriz(ref hdir, steerDir, steerSpeed, dt);
			}

			// SLOPE PULL
			// ----------------------------------------
			if (IsGround)
			{
				Vector3 steerDir   = actor.slopeDir;
				float   steerSpeed = settings.SpeedToDescentTurnRate.Evaluate(hspeed);
				float   overshoot  = settings.SpeedToDescentOvershoot.Evaluate(hspeed);

				steerSpeed *= _descent;                                                               // Steer
				steerSpeed *= Mathf.Lerp(1f, 1 - settings.MaxDescentInfluence, inputs.moveMagnitude); // Allow player to take over

				if (Vector3.Distance(_lastDescentVector, actor.slopeDir) > 0.01f)
				{
					// We have to store the descent sign, otherwise once we pass the
					_descentSign       = Mathf.Sign(Vector3.Dot(hdir, Vector3.Cross(actor.slopeDir, Vector3.up)));
					_lastDescentVector = actor.slopeDir;
				}

				// Rotate the steer dir by the overshoot (yaw degrees)
				steerDir = Quaternion.Euler(0, overshoot * _descentSign, 0) * steerDir;

				Draw.editor.Line(Motor.GroundingStatus.GroundPoint, Motor.GroundingStatus.GroundPoint + steerDir.normalized * _descent, Color.green);

				if (!Mathf.Approximately(steerDir.magnitude, 0))
				{
					// Rotate towards descent
					RotateHoriz(ref hdir, steerDir, steerSpeed, dt);
				}
			}


			Vector3 hvel = hdir.normalized.Horizontal() * hspeed;

			// AIR PHYSICS
			// ----------------------------------------
			if (IsAir)
			{
				vvel += actor.GravityVector * settings.AirGravityScalar;
				hvel *= 1 / (1f + settings.AirDrag * dt);
			}
			else
			{
				vvel = Vector3.zero;
			}

			// COMBINE COMPONENTS
			// ----------------------------------------
			currentVelocity = hvel + vvel;

			AddSurfaceAcceleration(ref currentVelocity);
		}

		[Serializable]
		public class Settings
		{
			public AnimationCurve JoystickCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

			[Title("Ground", HorizontalLine = false)]
			[FormerlySerializedAs("maxSpeed"),] public float SpeedMax;
			[FormerlySerializedAs("decelerationRate")]
			public float AirResistance;

			public float CoefficientOfFriction;
			public float WheelAcceleration;
			public float AirResistanceDamp = 2.0f;

			public AnimationCurve SpeedToTurnRate;
			[FormerlySerializedAs("slopeConversion"), Tooltip("X: angle of the slope. \nY: speed gained. (can be negative)")]
			public AnimationCurve SlopeConversion;
			public AnimationCurve VerticalConversion;

			[Title("Descent", HorizontalLine = false)]
			[FormerlySerializedAs("SpeedToDescentRate")]
			public AnimationCurve SpeedToDescentTurnRate;
			public AnimationCurve SpeedToDescentOvershoot;
			public AnimationCurve DescentCurve;
			public float          MaxDescentInfluence;

			[Title("Air", HorizontalLine = false)]
			[FormerlySerializedAs("airDrag")] public float AirDrag = 0.1f;
			[FormerlySerializedAs("airGravityScalar")]
			public float AirGravityScalar;
			public float Deceleration = 3.5f;

			[Title("Speed Boost", HorizontalLine = false)]
			public float SpeedBoostContactAccel  = 20f;
			public float SpeedBoostFrictionMulti = 0.8f;
			public float SpeedBoostMaxSpeed;
		}
	}
}
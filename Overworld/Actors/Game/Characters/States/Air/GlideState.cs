using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class GlideState : AirState
	{
		private readonly Settings _settings;

		public GlideState(Settings settings)
		{
			_settings = settings;
		}

		[ShowInInspector] public float GlideTilting { get; private set; }

		public override void OnActivate()
		{
			GlideTilting = 0;
			actor.inertia.Reset(_settings.Inertia, actor.velocity);
		}

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			Vector3 inputDirection = inputs.move;

			if (inputDirection.sqrMagnitude > 0)
			{
				Vector2 right   = Vector2.Perpendicular(inputDirection.xz());
				float   dotSide = Vector2.Dot(right, new Vector2(actor.facing.x, actor.facing.z));
				float   side    = Mathf.Sign(dotSide);

				// float dotForward    = Vector3.Dot(directionInputs.moveInputDirection, Actor.FacingDirection);
				// float steeringForce = 1 - dotForward.Min(0); // Steering is stronger the more we push to the side and inactive when we push backwards.

				Debug.DrawLine(Motor.TransientPosition, Motor.TransientPosition + new Vector3(right.x, 0, right.y) * 1.5f, Color.blue);

				float inputMagnitude = inputDirection.magnitude;

				Quaternion rotation = Quaternion.Euler(0, _settings.AngleRange * side * inputMagnitude * dt, 0);
				facing = rotation * actor.facing;

				GlideTilting -= side * inputMagnitude;
			}

			GlideTilting *= 1 / (1 + _settings.TiltingDeceleration * dt);
			GlideTilting =  GlideTilting.Clamp(_settings.TiltingMinimum, _settings.TiltingMaximum);
		}

		protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
		{
			base.UpdateHorizontal(ref hvel, dt);

			hvel = actor.inertia.inertiaVector + actor.facing.normalized * _settings.ForwardSpeed; // Move forward at all time.
			hvel = Vector3.ClampMagnitude(hvel, Mathf.Max(actor.inertia.force, _settings.ForwardSpeed));

			ReorientToGround(ref hvel);
		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			if (justActivated)
				vel.y = -_settings.FallSpeed; // Constant falling speed.

			vel.y = MathUtil.EasedLerp(vel.y, -_settings.FallSpeed, _settings.FallSpeedLerp, dt);
		}

		[Serializable]
		public class Settings
		{
			[FormerlySerializedAs("glideFallSpeed"), Title("Glide")]
			public float FallSpeed = 1;
			[Range01] public float FallSpeedLerp = 0.8f;

			[FormerlySerializedAs("glideAngleRange"), RangeAngle]
			public float AngleRange = 120;
			[FormerlySerializedAs("glideForwardSpeed")]
			public float ForwardSpeed = 2;

			[FormerlySerializedAs("glideTiltingMinimum")]
			public float TiltingMinimum;
			[FormerlySerializedAs("glideTiltingMaximum")]
			public float TiltingMaximum;
			[FormerlySerializedAs("glideTiltingDeceleration")]
			public float TiltingDeceleration = 0.35f;

			public InertiaForce.Settings Inertia;
		}
	}
}
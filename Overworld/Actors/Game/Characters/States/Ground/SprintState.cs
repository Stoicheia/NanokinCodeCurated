using System;
using Anjin.Util;
using UnityEngine;
using Util;

namespace Anjin.Actors
{
	[Serializable]
	public class SprintState : GroundState
	{
		[NonSerialized] public float boost;

		private Settings _settings;
		private float    _elapsed;
		private float    _elapsedBoostTimer;

		public SprintState(Settings settings)
		{
			_settings = settings;
		}

		public bool  IsDone      => _elapsed > _settings.SpeedOverTime[_settings.SpeedOverTime.length - 1].time;
		public float TargetSpeed => _settings.SpeedOverTime.Evaluate(_elapsed);

		public void Reset()
		{
			_elapsed           = 0;
			_elapsedBoostTimer = 0;
		}

		public override void OnActivate()
		{
			base.OnActivate();
			_settings.SpeedOverTime.postWrapMode = WrapMode.Clamp;
		}

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			// Find out where to we should be facing next
			Vector3 targetDirection;

			if (actor.HasVelocity) targetDirection   = actor.velocity.normalized;
			else if (inputs.hasMove) targetDirection = inputs.move;
			else targetDirection                     = facing;

			// Rotate towards that direction
			facing = Quaternion.RotateTowards(
				         actor.Motor.TransientRotation,
				         Quaternion.LookRotation(targetDirection),
				         _settings.FaceDirectionChangeSpeed * dt)
			         * Vector3.forward;
		}

		public override void UpdateVelocity(ref Vector3 currentVel, float dt)
		{
			// Reorient velocity on slope
			float targetSpeed = _settings.SpeedOverTime.Evaluate(_elapsed);
			targetSpeed += boost.Maximum(_settings.MaxBoostSpeed);

			Vector3 groundNormal   = actor.GetGroundNormal(currentVel);
			Vector3 targetVelocity = actor.Motor.GetDirectionTangentToSurface(inputs.move, groundNormal) * targetSpeed;

			// Smooth normal movement Velocity
			MathUtil.LerpWithSharpness(ref currentVel, targetVelocity, _settings.SpeedSharpness.Evaluate(inputs.moveMagnitude), dt);
		}

		public override void OnUpdate(float dt)
		{
			_elapsed += dt;

			if (IsDone)
			{
				_elapsedBoostTimer += dt;
				if (actor.velocity.magnitude < 0.2f || _elapsedBoostTimer > _settings.BoostResetTimer)
				{
					boost              = 0;
					_elapsedBoostTimer = 0;
				}
			}
			else
			{
				if (actor.airMetrics.airborn)
					boost -= _settings.BoostDeceleration * 0.5f * (1 - Mathf.Clamp01(actor.inertia.force / 5f)) * dt;
				else
					boost -= _settings.BoostDeceleration * dt;

				boost = boost.Clamp(0, _settings.MaxBoost);
			}
		}

		[Serializable]
		public class Settings
		{
			public AnimationCurve SpeedOverTime;
			public AnimationCurve SpeedSharpness;
			public float          FaceDirectionChangeSpeed;
			public float          MaxBoostSpeed     = 8;
			public float          MaxBoost          = 12;
			public float          BoostResetTimer   = 2.5f;
			public float          BoostDeceleration = 0.01f;
		}
	}
}
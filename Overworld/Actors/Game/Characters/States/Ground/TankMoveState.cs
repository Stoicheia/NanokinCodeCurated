using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Extensions;

namespace Anjin.Actors
{
	/// <summary>
	/// A movement state used for heavy characters.
	/// In this state, the character turns by steering its angle.
	/// The movement has an acceleration curve.
	/// </summary>
	public class TankMoveState : GroundState
	{
		[Serializable]
		public class Settings
		{
			[InfoBox("X-Axis: Elapsed seconds of accelerated.\nY-Axis: Speed.\n\nThe sampling value increases according to the passage of time.")]
			[SerializeField] public AnimationCurve SpeedOverTime = AnimationCurve.EaseInOut(0, 0, 1, 1);
			[InfoBox("A scaling value applied to the passage of time of deceleration. (allows decelerating slower/faster than acceleration)")]
			[SerializeField] public float DecelerationScale = 1;
			[InfoBox("How quickly should the actor's velocity change according to the current speed. Usually should be kept low.")]
			[SerializeField] public float VelocitySharpness;
			[SuffixLabel("deg")]
			[SerializeField] public float FaceDirectionChangeSpeed = 180; // Degrees per second
			[SuffixLabel("deg")]
			[SerializeField] public float MoveDirectionChangeSpeed = 180; // Degrees per second
			[InfoBox("Allows instant turning on the first frame where we have movement inputs.")]
			[SerializeField] public bool InstantTurningOnInitialInput = true;

			private AnimationCurve _speedOverTimeInv;

			public float GetTimeForSpeed(float speed)
			{
				if (_speedOverTimeInv == null)
				{
					_speedOverTimeInv = SpeedOverTime.GetInverse();
				}

				return _speedOverTimeInv.Evaluate(speed);
			}
		}

		public Settings settings;

		private bool    _wasIdle;
		private Vector3 _moveDirection;

		public TankMoveState(Settings settings)
		{
			this.settings = settings;
		}

		/// <summary>
		/// The sampling value fluctuating according with passage of time.
		/// </summary>
		[Title("Debug")]
		[ShowInInspector, HideInEditorMode]
		public float accumulatedTime;

		/// <summary>
		/// Value between 0-1 indicating how much we've progressed from zero to max speed.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public float MaxSpeedProgress => accumulatedTime / settings.SpeedOverTime[settings.SpeedOverTime.length - 1].time;

		/// <summary>
		/// The current speed sampled from the curve.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public float TargetSpeed => inputs.moveSpeed ?? settings.SpeedOverTime.Evaluate(accumulatedTime);

		public override void OnDeactivate()
		{
			base.OnDeactivate();
			_wasIdle = false;
		}

		public override void OnActivate()
		{
			base.OnActivate();

			_moveDirection  = actor.facing;
			accumulatedTime = settings.GetTimeForSpeed(actor.velocity.magnitude);
		}

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			Vector3 targetDirection;

			if (inputs.look.HasValue && inputs.look.Value.magnitude > Mathf.Epsilon)
				targetDirection = inputs.look.Value;
			else if (actor.HasVelocity) targetDirection   = actor.velocity.normalized;
			else if (inputs.hasMove) targetDirection = inputs.move;
			else targetDirection                     = facing;

			facing = Quaternion.RotateTowards(
				         actor.Motor.TransientRotation,
				         Quaternion.LookRotation(targetDirection),
				         !inputs.instantLook ? settings.FaceDirectionChangeSpeed * dt : 100000)
			         * Vector3.forward;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			if (inputs.hasMove)
			{
				accumulatedTime += deltaTime;
				accumulatedTime =  accumulatedTime.Maximum(settings.SpeedOverTime[settings.SpeedOverTime.length - 1].time);

				if (_wasIdle)
				{
					if (settings.InstantTurningOnInitialInput) _moveDirection = inputs.move;
					_wasIdle = false;
				}
			}
			else
			{
				_wasIdle = true;
			}

			// Update directionality of movement.
			if (inputs.hasMove)
			{
				// Rotate movement towards the held direction.
				_moveDirection = Quaternion.RotateTowards(
					Quaternion.LookRotation(_moveDirection),
					Quaternion.LookRotation(inputs.move),
					settings.MoveDirectionChangeSpeed
				) * Vector3.forward;
			}


			// Update speed of velocity.
			float speed = currentVelocity.magnitude;
			MathUtil.LerpWithSharpness(ref speed, TargetSpeed, settings.VelocitySharpness, deltaTime);

			// Apply our new velocity and adjust to the surface.
			currentVelocity = _moveDirection * speed;
			currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, actor.GetGroundNormal(currentVelocity)) * currentVelocity.magnitude;
		}

		public override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);

			if (inputs.move.magnitude < Mathf.Epsilon)
			{
				accumulatedTime -= Time.deltaTime * settings.DecelerationScale;
				accumulatedTime =  accumulatedTime.Minimum(0);
			}
		}
	}
}
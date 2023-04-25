using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtilities;
using Util;

namespace Anjin.Actors
{
	public class PogoState : StateKCC
	{
		private readonly Settings _settings;

		private float       _jumpCount;
		private ManualTimer _jumpTimer;
		private float       _savedVelocity;
		private Vector3     _nextFacing;

		public float GroundProgress => _jumpTimer.Progress;

		public PogoState(Settings settings)
		{
			_settings  = settings;
			_jumpTimer = new ManualTimer();
		}

		public override void OnActivate()
		{
			_jumpCount = 0;

			actor.inertia.settings = _settings.Inertia;
		}

		protected override float TurnSpeed => 1;

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			facing = IsGround ? actor.JoystickOrFacing : actor.facing;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			if (IsGround)
			{
				_jumpTimer.Update(deltaTime);

				if (airMetrics.justLanded || justActivated)
				{
					// LAND or ACTIVATE FROM GROUND
					// ----------------------------------------
					_jumpCount = _jumpCount.Clamp(0, _settings.MaxBounces);

					_jumpTimer.Duration = _settings.BounceDuration.Evaluate(BounceProgress);
					_jumpTimer.Restart();

					_savedVelocity  = currentVelocity.ChangeY(0).magnitude;
					currentVelocity = Vector3.zero;
				}

				if (_jumpTimer.IsDone)
				{
					// JUMP
					// ----------------------------------------
					if (inputs.jumpHeld)
						_jumpCount += 1;
					else
						_jumpCount -= 1;

					if (!inputs.hasMove)
						_savedVelocity *= 1 - _settings.BounceDeceleration.Evaluate(BounceProgress);

					float height = _settings.BounceHeight.Evaluate(BounceProgress);

					actor.AddForce(Vector3.up * MathUtil.CalculateJumpForce(height * actor.GetJumpHeightModifier(), actor.Gravity));

					float hforce = _settings.BounceForce.Evaluate(BounceProgress) * _settings.JoystickAmplitudeCurve.Evaluate(inputs.moveMagnitude) + _savedVelocity;
					actor.inertia.Reset(_settings.Inertia, actor.JoystickOrFacing, hforce);
				}
			}
			else if (IsAir)
			{
				_jumpTimer.Finish();

				SplitAxisVector axes = new SplitAxisVector(currentVelocity);

				axes.horizontal = actor.inertia.vector;
				ApplyAirPhysics(ref axes.vertical, actor.GravityVector * _settings.HangTime.GetGravityScale(airMetrics));

				currentVelocity = Vector3.ClampMagnitude(axes, _settings.MaxAirMovement);
			}
		}

		private float BounceProgress => _jumpCount / _settings.MaxBounces;

		[Serializable]
		public class Settings
		{
			public AnimationCurve JoystickAmplitudeCurve = AnimationCurve.Linear(0, 0, 1, 1);
			public AnimationCurve JoystickAngleCurve     = AnimationCurve.Linear(0, 0, 1, 1);

			[Title("Bouncing")]
			public int MaxBounces = 4;
			public AnimationCurve BounceDuration     = AnimationCurve.Constant(0, 1, 0.22f);
			public AnimationCurve BounceHeight       = AnimationCurve.Linear(0, 12, 1, 22);
			public AnimationCurve BounceForce        = AnimationCurve.Linear(0, 2, 1, 9);
			public AnimationCurve BounceDeceleration = AnimationCurve.Linear(0, 0, 1, 1);

			[Title("Air Movement")]
			public JumpHanger HangTime;
			public InertiaForce.Settings Inertia ;
			public float                 MaxAirMovement;
		}
	}
}
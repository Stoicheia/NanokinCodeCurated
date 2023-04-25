using System;
using Anjin.Nanokin;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[Serializable]
	public class WalkState : GroundState
	{
		private float    _stepTimer;
		private Settings _settings;
		private Vector3  _dir;

		private Vector3 _lastFrameJoystick;
		private bool    _hadInputLastFrame;
		private bool    _justDashed;


		public WalkState(Settings settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// Last time we dashed.
		/// </summary>
		public float DashTime { get; private set; } = float.MinValue;

		/// <summary>
		/// Current stepping sound.
		/// Must be set externally
		/// </summary>
		public AudioDef? StepSound { get; set; }

		[ShowInInspector]
		protected override Vector3 TurnDirection
		{
			get
			{
				if (inputs.look.HasValue) return inputs.look.Value;
				if (inputs.hasMove) return inputs.move;

				float velocityMagnitude = actor.velocity.magnitude;
				if (velocityMagnitude > Mathf.Epsilon) return actor.velocity / velocityMagnitude;

				else return actor.facing;
			}
		}

		protected override float TurnSpeed => _settings.TurnSharpness.Evaluate(actor.velocity.magnitude);

		public override void OnActivate()
		{
			base.OnActivate();
			_settings.Speed.Evaluate(0.5f);
			_dir = actor.facing;

			actor.velocity.x *= inputs.moveMagnitude;
			actor.velocity.z *= inputs.moveMagnitude;
		}

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			if (_justDashed)
			{
				facing      = inputs.move;
				_justDashed = false;
				return;
			}

			base.UpdateFacing(ref facing, dt);
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			if (inputs.hasMove)
			{
				if (!_hadInputLastFrame)
					OnActivate();

				_dir = inputs.move;
			}

			if (GameInputs.ActiveDevice == InputDevices.Gamepad)
			{
				// "Dashing" (detect joystick being slammed)
				// ----------------------------------------

				float d = inputs.move.magnitude - _lastFrameJoystick.magnitude;
				if (d > _settings.DashDetectionThreshold)
				{
					if (currentVelocity.magnitude < _settings.MaxSpeed)
					{
						// Immediately jump to max run speed

						// NOTE:
						// This logic can allow for a difficult speedrun tech: instant momentum redirection.
						// You could build up a lot of slide momentum, hop out and dash back to flip
						// that momentum by 180deg.
						currentVelocity = inputs.move * _settings.MaxSpeed;
					}

					currentVelocity += inputs.move.normalized * _settings.DashVelocity;

					DashTime    = Time.time;
					_justDashed = true;
				}
			}

			_hadInputLastFrame = inputs.hasMove;
			_lastFrameJoystick = inputs.move;

			// Reorient velocity on slope
			Vector3 groundnorm = actor.GetGroundNormal(currentVelocity);
			Vector3 movedir    = actor.Motor.GetDirectionTangentToSurface(_dir, groundnorm);

			// Smooth normal movement Velocity
			float speed = currentVelocity.magnitude;

			float targetSpeed = inputs.moveSpeed ?? Mathf.Max(speed * 0.9f * inputs.moveMagnitude, actor.ApplySpeedBoost(_settings.Speed.Evaluate(inputs.moveMagnitude)));
			targetSpeed *= _settings.SpeedTurnMult.Evaluate(actor.NormalizedJoystickFacingDot);

			if(actor.SpeedBoost)
				Debug.Log(targetSpeed);

			float sharpness = _settings.SpeedSharpness.Evaluate(inputs.moveMagnitude);
			if (inputs.hasMove)
				sharpness *= _settings.SpeedSharpnessMultMoving;
			else
			{
				sharpness *= _settings.SpeedSharpnessMultStopping;
				if (actor.hasSurface) sharpness *= actor.surface.Friction;
				if (actor.hasTerrain) sharpness *= actor.terrain.Friction;
			}

			MathUtil.LerpWithSharpness(ref speed, targetSpeed, sharpness, deltaTime);
			currentVelocity = movedir * speed;

			AddSurfaceAcceleration(ref currentVelocity);
		}

		public override void AfterCharacterUpdate(float dt)
		{
			base.AfterCharacterUpdate(dt);

			float velocityMagnitude = actor.velocity.magnitude;
			if (velocityMagnitude > 0.1f)
			{
				_stepTimer += Time.deltaTime * (velocityMagnitude / _settings.MaxSpeed);
				if (_stepTimer > _settings.StepSoundTimer)
				{
					GameSFX.Play(StepSound ?? _settings.DefaultStepSound, actor);
					_stepTimer = 0;
				}
			}
			else
			{
				_stepTimer = 0;
			}
		}

		[Serializable]
		public class Settings
		{
			public AnimationCurve Speed                      = AnimationCurve.EaseInOut(0, 0, 1, 3.5f);
			public AnimationCurve SpeedTurnMult              = AnimationCurve.EaseInOut(0, 0.8f, 1, 1);
			public AnimationCurve SpeedSharpness             = AnimationCurve.Linear(0, 12, 1, 8);
			public float          SpeedSharpnessMultMoving   = 1.25f;
			public float          SpeedSharpnessMultStopping = 1;
			public AnimationCurve TurnSharpness              = AnimationCurve.Linear(0, 12, 1, 8);

			[Tooltip("Higher values = the joystick must be slammed faster to recognize as dashing.")]
			[Range01]
			public float DashDetectionThreshold = 0.65f;
			public float DashVelocity = 0;

			[Space]
			public AudioDef DefaultStepSound;

			public float StepSoundTimer;

			public float MaxSpeed
			{
				get
				{
					_maxSpeedkeys = _maxSpeedkeys ?? Speed.keys; // `keys` has a gc alloc for some reason and this is called every frame
					return _maxSpeedkeys[Speed.length - 1].value;
				}
			}

			private Keyframe[] _maxSpeedkeys;
		}
	}
}
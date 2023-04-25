using Anjin.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class WallSlideState : StateKCC
	{
		private Vector3 _startPosition;
		private bool _wasFalling;

		private bool _raising;
		private bool _shorthop;

		private Vector3? _initialTurnDirection;

		private float _stepTimer;

		protected readonly Settings settings;
		public AudioDef? StepSound { get; set; }

		public override float Gravity
		{
			get
			{
				float gravBase = base.Gravity * settings.GravityMod;
				float kls = settings.AssumedInwardForce * settings.BaseFriction;

				if (actor.velocity.y < 0)
				{
					return gravBase - Mathf.Min(gravBase, kls);
				}

				return gravBase + kls; // physics lah

			}
		}

		protected override Vector3 TurnDirection
		{
			get
			{
				if (_initialTurnDirection.HasValue) return _initialTurnDirection.Value;
				return base.TurnDirection;
			}
		}

		public WallSlideState(Settings settings)
		{
			this.settings = settings;
		}

		public override void OnActivate()
		{
			_startPosition = actor.position;

			_raising = actor.velocity.y > 0;
			_shorthop = false;

			_initialTurnDirection = base.TurnDirection;

		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			base.UpdateVertical(ref vel, dt);

			// In case we start rising again for some reason, wind volumes etc.
			if (_wasFalling && vel.y > 0)
				_startPosition = actor.position;

			_wasFalling = vel.y < 0;

			if (!inputs.jumpHeld && _raising && vel.y > 0)
			{
				_raising = false;
			}

			if (vel.y < -settings.MaxSpeed)
			{
				vel.y = -settings.MaxSpeed;
			}
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			UpdateAir(ref currentVelocity, deltaTime, settings.SpeedDamping);
		}


		public override void AfterCharacterUpdate(float dt)
		{
			base.AfterCharacterUpdate(dt);

			float velocityMagnitude = actor.velocity.magnitude;
			if (velocityMagnitude > 0.01f)
			{
				_stepTimer += Time.deltaTime * (velocityMagnitude / settings.MaxSpeed);
				if (_stepTimer > settings.StepSoundTimer)
				{
					GameSFX.Play(StepSound ?? settings.DefaultStepSound, actor);
					_stepTimer = 0;
				}
			}
			else
			{
				_stepTimer = 0;
			}
		}


		[System.Serializable]
		public class Settings
		{
			public float GravityMod = 1f;
			public float AssumedInwardForce = 1f;
			public float BaseFriction = 1f;
			public float InitialSpeedMultiplier = 0.2f;
			public float MaxSpeed = 100f;
			public float SpeedDamping = 0.2f;

			public float ClingSeconds = 0.1f;

			public float HorizontalDamping = 1f;
			public InertiaForce.Settings InertialControl;

			public float StepSoundTimer = 0.1f;
			public AudioDef DefaultStepSound;
			public float ParticleIntervalSeconds = 0.1f;
		}
	}
}

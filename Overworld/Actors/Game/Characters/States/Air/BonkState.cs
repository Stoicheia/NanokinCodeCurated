using Anjin.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class BonkState : StateKCC
	{
		private Vector3 _startPosition;

		private bool _raising;

		private Vector3? _initialTurnDirection;

		protected readonly Settings settings;

		private float _timer;
		private bool _launched;
		public int _bumps;

		public bool PlaySound { get; private set; }

		public bool BonkOver => _timer > settings.StunTime;

		public override float Gravity => base.Gravity * settings.GravityMod;

		protected override Vector3 TurnDirection
		{
			get
			{
				if (_initialTurnDirection != null) return _initialTurnDirection.Value;
				else return inputs.move;
			}
		}

		public BonkState(Settings settings)
		{
			this.settings = settings;
		}

		public override void OnActivate()
		{
			_startPosition = actor.position;
			actor.inertia.Reset(settings.InertialControl, actor.velocity);

			_raising  = actor.velocity.y > 0;
			_timer = 0;
			_launched = false;
			_bumps = 0;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			UpdateAir(ref currentVelocity, deltaTime, settings.SpeedDamping);
			if (Motor.GroundingStatus.IsStableOnGround)
			{
				currentVelocity = Vector3.Lerp(Vector3.zero, currentVelocity, 1 - settings.GroundDecel * deltaTime);
			}

			_timer += deltaTime;
		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			base.UpdateVertical(ref vel, dt);

			if (Motor.GroundingStatus.IsStableOnGround && _launched && settings.NumberOfBumps > _bumps)
			{
				actor.Jump(ref vel, vel.magnitude * Mathf.Pow(settings.UniversalElasticity, _bumps+1));
				_bumps++;
			}
			else
			{
				_launched = true;
			}
		}

		public BonkState WithForce(ref Vector3 currentVelocity, Vector3 dir, bool fromAttack = false)
		{
			PlaySound = !fromAttack;

			_initialTurnDirection = dir;

			//actor.ClearVelocity
			//actor.velocity.Horizontal()

			currentVelocity.x = 0;
			currentVelocity.z = 0;

			actor.Jump(ref currentVelocity, settings.UpForce,
				settings.BackForce + Mathf.Clamp01(Motor.Velocity.magnitude) * settings.UniversalElasticity * settings.BackForce * 5, -TurnDirection);

			return this;
		}


		public override void AfterCharacterUpdate(float dt)
		{

		}


		[System.Serializable]
		public class Settings
		{
			public float GravityMod = 1f;
			public float UpForce = 10;
			public float BackForce = 10;
			public float SpeedDamping = 0.2f;
			public float StunTime = 1f;
			public float UniversalElasticity = 0.3f;
			public float GroundDecel = 0.5f;
			public int NumberOfBumps = 2;

			public InertiaForce.Settings InertialControl;
		}
	}
}

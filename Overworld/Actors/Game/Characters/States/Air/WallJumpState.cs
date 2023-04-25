using System;
using Anjin.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class WallJumpState : AirState
	{
		private Vector3 _startPosition;
		private bool    _wasFalling;

		private bool _raising;

		private Vector3 _initialDirection;

		private float _internalTimer;

		protected readonly Settings settings;
		public override    float    SpeedDamping => settings.HorizontalDamping;
		public override    float    Gravity      => base.Gravity * settings.GravityScale * settings.ApexHang.GetGravityScale(airMetrics);

		public bool CanAct => _internalTimer >= settings.CanActAfterSeconds;

		protected override Vector3 TurnDirection
		{
			get
			{
				//if (actor.elapsedStateTime > settings.DirectionLockSeconds)
				//{
				//	return inputs.move;
				//}

				Debug.Log("Wall normal: " + _initialDirection);
				return _initialDirection;
			}
		}

		public WallJumpState(Settings settings)
		{
			this.settings = settings;
		}

		public override void OnActivate()
		{
			base.OnActivate();
			_startPosition = actor.position;
			actor.inertia.Reset(settings.InertialControl, actor.velocity);

			_raising  = actor.velocity.y > 0;
			_internalTimer = 0;
		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			base.UpdateVertical(ref vel, dt);

			_internalTimer += dt;
			// In case we start rising again for some reason, wind volumes etc.
			if (_wasFalling && vel.y > 0)
				_startPosition = actor.position;

			_wasFalling = vel.y < 0;

			if (!inputs.jumpHeld && _raising && vel.y > 0)
			{
				_raising  = false;
			}
		}

		public WallJumpState WithDirection(Vector3 dir)
		{
			//actor.Jump(ref currentVelocity, height, settings.InitialOutwardSpeed, -TurnDirection);
			_initialDirection = dir;
			return this;
		}

		public WallJumpState WithHeight(ref Vector3 currentVelocity, float height)
		{
			//actor.Jump(ref currentVelocity, height, settings.InitialOutwardSpeed, -TurnDirection);
			return this;
		}

		public WallJumpState WithDefaultHeight(ref Vector3 currentVelocity)
		{
			//actor.Jump(ref currentVelocity, settings.Height, settings.InitialOutwardSpeed, -TurnDirection);
			return this;
		}

		[Serializable]
		public class Settings
		{
			public float InitialOutwardSpeed = 2f;
			public float DirectionLockSeconds = 0.5f;

			[FormerlySerializedAs("height")]
			public float Height = 5f;
			[FormerlySerializedAs("gravityScale")]
			public float GravityScale = 1;
			public float HorizontalDamping = 4f;
			[Range01]
			public float ShortHopDrag = 0f;
			public float ShortHopDuration = 0f;

			[FormerlySerializedAs("inertiaControl")]
			public InertiaForce.Settings InertialControl;
			[FormerlySerializedAs("apexHang")]
			public JumpHanger ApexHang;
			public float TurnSpeed;

			public float CanActAfterSeconds = 0.3f;
		}
	}
}

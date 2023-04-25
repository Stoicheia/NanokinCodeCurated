using Anjin.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class DoubleJumpState : AirState
	{
		private Vector3 _startPosition;
		private bool _wasFalling;

		private bool _raising;
		private bool _shorthop;

		protected readonly Settings settings;
		public override float SpeedDamping => settings.HorizontalDamping;
		public override float Gravity => base.Gravity * settings.GravityScale * settings.ApexHang.GetGravityScale(airMetrics);

		protected override Vector3 TurnDirection
		{
			get
			{
				if (settings.UseInputLookDirection && inputs.look.HasValue && inputs.look.Value.magnitude > Mathf.Epsilon)
				{
					return inputs.look.Value;
				}

				if (Vector3.Distance(_startPosition, actor.position) < Mathf.Epsilon)
					return actor.inertia.direction;

				return _startPosition.Towards(actor.position).Horizontal();
			}
		}

		public DoubleJumpState(Settings settings)
		{
			this.settings = settings;
		}

		public override void OnActivate()
		{
			base.OnActivate();
			_startPosition = actor.position;
			actor.inertia.Reset(settings.InertialControl, actor.velocity);

			_raising = actor.velocity.y > 0;
			_shorthop = false;
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
				_shorthop = actor.elapsedStateTime < settings.ShortHopDuration;
				_raising = false;
			}

			if (_shorthop)
			{
				if (vel.y > 0)
					vel.y *= 1 - settings.ShortHopDrag;
				else
					_shorthop = false;
			}
		}

		public DoubleJumpState WithHeight(ref Vector3 currentVelocity, float height)
		{
			actor.Jump(ref currentVelocity, height);
			return this;
		}

		public DoubleJumpState WithDefaultHeight(ref Vector3 currentVelocity)
		{
			actor.Jump(ref currentVelocity, settings.Height);
			return this;
		}

		[System.Serializable]
		public class Settings
		{
			public bool UseInputLookDirection = false;

			[FormerlySerializedAs("height")]
			public float Height = 4f;
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
		}
	}
}

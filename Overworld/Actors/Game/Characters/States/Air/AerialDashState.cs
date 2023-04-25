using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Actors
{
	[Serializable]
	public class AerialDashState : AirState
	{
		public readonly Settings settings;

		public AerialDashState(Settings settings)
		{
			this.settings = settings;
		}

		public override float Gravity => actor.Gravity * settings.DashGravityScale;

		public override float SpeedDamping => settings.HorizontalDamping;

		private float? _tempJumpForceOnActivate;

		public override void OnActivate()
		{
			base.OnActivate();

			Vector3 start        = actor.facing;
			Vector3 target       = actor.JoystickOrFacing;
			Vector3 curvedTarget = Vector3.Slerp(start, target, settings.directionCurve.Evaluate(actor.NormalizedJoystickFacingDot));

			actor.AddForce(Vector3.up * (actor.CalculateJumpForce(settings.DashJumpHeight) + _tempJumpForceOnActivate.GetValueOrDefault(0)), true);
			actor.inertia.Reset(settings.Inertia, curvedTarget, settings.DashForwardForce);

			_tempJumpForceOnActivate = null;
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();
			_tempJumpForceOnActivate = null;
		}

		public override void UpdateFacing(ref Vector3 facing, float dt)
		{
			facing = actor.inertia.direction;
		}

		public AerialDashState WithAddedJumpForce(float force)
		{
			_tempJumpForceOnActivate = force;
			return this;
		}

		[Serializable]
		public class Settings
		{
			[FormerlySerializedAs("inertiaSettings")]
			public InertiaForce.Settings Inertia;

			[FormerlySerializedAs("dashForwardForce"),Space]
			public float DashForwardForce = 15f;
			[FormerlySerializedAs("dashJumpHeight")]
			public float DashJumpHeight = 0.4f;
			[FormerlySerializedAs("dashGravityScale")]
			public float DashGravityScale = 0.12f;

			public float HorizontalDamping = 4.5f;

			public AnimationCurve directionCurve;
		}
	}
}
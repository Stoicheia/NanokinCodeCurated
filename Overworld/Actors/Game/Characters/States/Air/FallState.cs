using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Actors
{
	public class FallState : AirState
	{
		private readonly Settings _settings;

		public override float SpeedDamping => _settings.HorizontalDamping;

		protected override Vector3 TurnDirection => inputs.move.magnitude > Mathf.Epsilon
			? inputs.move
			: actor.facing;

		public FallState(Settings settings)
		{
			_settings = settings;
		}

		public override float Gravity => base.Gravity * _settings.GravityScalar;

		public override void OnActivate()
		{
			base.OnActivate();
			actor.inertia.Reset(_settings.Inertia, actor.velocity);
		}

		[Serializable]
		public class Settings
		{
			public InertiaForce.Settings Inertia;
			[FormerlySerializedAs("minMoveSpeed")]
			public float MinMoveSpeed;
			[FormerlySerializedAs("maxMoveSpeed")]
			public float MaxMoveSpeed;
			[FormerlySerializedAs("gravityScalar")]
			public float GravityScalar = 1;
			public float HorizontalDamping = 1.35f;
		}
	}
}
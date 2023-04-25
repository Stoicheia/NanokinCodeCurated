using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[Serializable]
	public class GroundTakeoffState : StateKCC
	{
		public readonly Settings settings;

		public override bool IsGround => true;

		[ShowInInspector] private bool  _hasReleasedTakeoff;
		[ShowInInspector] private float _elapsedTakeoffTime;
		[ShowInInspector] private float _carriedVelocity;
		private                   bool  _hasCarriedFromPreviousState;

		public GroundTakeoffState(Settings settings)
		{
			this.settings = settings;
		}

		[ShowInInspector] public float ElapsedTakeoffTime => _elapsedTakeoffTime;

		[ShowInInspector] public float NormalizedElapsedTakeoff => (_elapsedTakeoffTime - settings.MinDuration) / (settings.MaxDuration - settings.MinDuration);

		[ShowInInspector] public bool ShouldBeginJump => _elapsedTakeoffTime >= settings.MinDuration &&
		                                                 (_hasReleasedTakeoff || _elapsedTakeoffTime >= settings.MaxDuration);

		protected override Vector3 TurnDirection => actor.facing; // Can't turn in this state.

		public override void OnActivate()
		{
			_elapsedTakeoffTime          = 0;
			_hasReleasedTakeoff          = false;
			_hasCarriedFromPreviousState = false;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			if (!_hasCarriedFromPreviousState)
			{
				_carriedVelocity = currentVelocity.magnitude;
			}

			float   walkSpeed      = _carriedVelocity * settings.MovementSpeedMultiplier;
			Vector3 targetVelocity = inputs.move * walkSpeed;

			currentVelocity = currentVelocity.EasedLerp(targetVelocity, settings.MovementSpeedDeceleration);

		}

		public override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);

			if (!active)
				return;

			if (!inputs.jumpHeld)
			{
				_hasReleasedTakeoff = true;
			}

			_elapsedTakeoffTime += Time.deltaTime;
		}


		[Serializable]
		public class Settings
		{
			[Title("Height")]
			[FormerlySerializedAs("minHeight")] public float MinHeight = 1f;
			[FormerlySerializedAs("minDuration")]                      public float          MinDuration = 0.07f;
			[FormerlySerializedAs("maxDuration")]                      public float          MaxDuration = 0.11f;
			[FormerlySerializedAs("movementSpeedMultiplier"), Range01] public float          MovementSpeedMultiplier;
			[FormerlySerializedAs("movementSpeedDeceleration")]        public float          MovementSpeedDeceleration    = 0.5f;
			[FormerlySerializedAs("jumpForceCurve")]                   public AnimationCurve JumpForceCurve               = AnimationCurve.Constant(0, 0, 1);
			[FormerlySerializedAs("maxSpeedForTakeoffActivation")]     public float          MaxSpeedForTakeoffActivation = 0.85f;
		}
	}
}
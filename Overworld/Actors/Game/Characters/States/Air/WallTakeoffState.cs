using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[Serializable]
	public class WallTakeoffState : StateKCC
	{
		public readonly Settings settings;

		[ShowInInspector] private bool  _hasReleasedTakeoff;
		[ShowInInspector] private float _elapsedTakeoffTime;
		[ShowInInspector] private float _carriedVelocity;
		private                   bool  _hasCarriedFromPreviousState;

		public Vector3 FromWallNormal;

		private Vector3 _direction;
		private bool _swordInput;
		private bool _swordInitiallyHeld;
		public bool SwordUse => _swordInput && !_swordInitiallyHeld;

		public WallTakeoffState(Settings settings)
		{
			this.settings = settings;
		}

		public void UpdateDirection()
		{
			_direction = -actor.facing;
		}

		[ShowInInspector] public float ElapsedTakeoffTime => _elapsedTakeoffTime;

		[ShowInInspector] public float NormalizedElapsedTakeoff => (_elapsedTakeoffTime - settings.MinDuration) / (settings.MaxDuration - settings.MinDuration);

		[ShowInInspector] public bool ShouldBeginJump => _elapsedTakeoffTime >= settings.MinDuration &&
		                                                 (_hasReleasedTakeoff || _elapsedTakeoffTime >= settings.MaxDuration);

		//protected override Vector3 TurnDirection => _direction; // Can't turn in this state.

		protected override Vector3 TurnDirection { get
			{
				Debug.Log("Wall normal: " + FromWallNormal);
				return (FromWallNormal != Vector3.zero ? FromWallNormal : _direction);
				//return FromWallNormal;
			}
		}

		public override void OnActivate()
		{
			_elapsedTakeoffTime          = 0;
			_hasReleasedTakeoff          = false;
			_hasCarriedFromPreviousState = false;
			_swordInput = false;
			_swordInitiallyHeld = inputs.swordHeld;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			if (!_hasCarriedFromPreviousState)
			{
				_carriedVelocity = currentVelocity.magnitude;
			}

			float   walkSpeed      = _carriedVelocity * settings.MovementSpeedMultiplier;
			//Vector3 targetVelocity = inputs.move * walkSpeed;
			Vector3 targetVelocity = FromWallNormal * walkSpeed;

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

			if (inputs.swordHeld)
			{
				_swordInput = true;
			}

			_elapsedTakeoffTime += Time.deltaTime;

		}


		[Serializable]
		public class Settings
		{
			public float          MinDuration = 0.07f;
			public float          MaxDuration = 0.11f;
			public float          UpThrustMultiplier = 0.2f;
			public float          UpHeightMultiplier = 3f;
			public float          BackThrustMultiplier = 1.2f;
			public float          BackHeightMultplier = 1.2f;
			public float          ForwardThrustMultiplier = 0.7f;
			public float          ForwardHeightMultiplier = 0.75f;
			public float          SideThrustMultiplier = 0.75f;
			public float          SideOutMultipler = 0.75f;
			public float          SideHeightMultiplier = 0.75f;

			public float SideLeniency = 0.5f;

			[Range01] public float          MovementSpeedMultiplier;
			public float          MovementSpeedDeceleration    = 0.5f;
		}
	}
}
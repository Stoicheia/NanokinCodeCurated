using System;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtilities;
using Util;

namespace Anjin.Actors {
	public class WaterJetState : AirState {

		public readonly Settings settings;

		public WaterJetState(Settings settings)
		{
			this.settings = settings;
		}

		public override float SpeedDamping => settings.SpeedDamping;
		public override float Gravity      => 1;

		[ShowInInspector]
		public bool EnteredOnHead;

		public bool IsPlayerMoving;

		[ShowInInspector]
		public bool IsOnHead {
			get {
				if (jet == null) return false;

				return (Motor.transform.position + Motor.CharacterTransformToCapsuleTop).y > jet.HeadYMinPos.y;
			}
		}

		private WaterJet jet => actor.waterJet;

		public override void OnActivate()
		{
			base.OnActivate();
			actor.Motor.SetGroundSolvingActivation(false);

			EnteredOnHead = IsOnHead;
		}

		public override void OnDeactivate()
		{
			actor.Motor.SetGroundSolvingActivation(true);
			IsPlayerMoving = false;
		}

		private bool _horizontalSmoothing;

		public override void UpdateVelocity(ref Vector3 currentVelocity, float dt)
		{
			IsPlayerMoving = false;

			Vector3 addedVelocity = Vector3.zero;
			if (actor.waterJet)
				addedVelocity = actor.waterJet.Velocity / dt;

			SplitAxisVector currentAxes = new SplitAxisVector(currentVelocity - addedVelocity);

			Vector3 newHorizontal = currentAxes.horizontal;
			Vector3 newVertical   = currentAxes.vertical;

			_horizontalSmoothing = true;

			UpdateHorizontal(ref newHorizontal, dt);
			UpdateVertical(ref newVertical, dt);

			// Lerp the horizontal axis.
			if(_horizontalSmoothing)
				newHorizontal = MathUtil.LerpWithSharpness(currentAxes.horizontal, newHorizontal, SpeedDamping, dt);

			newHorizontal = Vector3.ClampMagnitude(newHorizontal, actor.AirMaxSpeed);

			currentVelocity = newHorizontal + newVertical + addedVelocity;
			// currentVelocity = Vector3.ClampMagnitude(currentVelocity, actor.AirMaxSpeed);
		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			if (justActivated) {
				vel.y = Mathf.Max(vel.y, settings.MaxFallSpeedOnActivation);
			}

			if (EnteredOnHead && IsOnHead) {
				float dist = Motor.transform.position.y - jet.HeadCenter.y;
				float bob  = Mathf.Sin(Time.time * settings.BobSpeed) * settings.BobForce;

				vel.y = Mathf.Lerp(vel.y, Mathf.Lerp(bob, -dist, Mathf.Clamp01(Mathf.Abs(dist) / 0.5f)), 0.4f);
			} else {
				if (vel.y < settings.MaxRiseSpeed)
					vel.y += settings.RiseAccel;
			}
		}

		protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
		{
			Vector3 jetCenter       = (jet.transform.position.ChangeY(0) - actor.transform.position.ChangeY(0));
			float   distToJetCenter = jetCenter.magnitude;
			Vector3 toJetCenterNorm = jetCenter.normalized;

			if (justActivated) {
				_horizontalSmoothing = false;

				hvel = Vector3.ClampMagnitude(hvel, EnteredOnHead ? settings.MoveSpeedClampOnEnteringHead : settings.MoveSpeedClampOnEnteringStream);

			} else if(IsOnHead) {
				if(inputs.move.magnitude < Mathf.Epsilon)
					hvel =  Vector3.Lerp(hvel, Vector3.zero, 0.65f);
				else {
					hvel           += inputs.move * settings.MoveSpeedOnHead;
					IsPlayerMoving =  hvel.magnitude > 0.5f;
				}

			} else {
				hvel = Vector3.Lerp(hvel, toJetCenterNorm * settings.VacuumForce * settings.VacuumHorizontalScaling.Evaluate(Mathf.Clamp01(distToJetCenter / jet.Radius)), 0.25f);

			}


		}


		[Serializable]
		public class Settings {

			public float MoveSpeedClampOnEnteringHead;
			public float MoveSpeedClampOnEnteringStream;
			public float MoveSpeedOnHead;
			public float HeadPullToStableY;

			public float JumpForceWhenDivingOffHead = 5f;

			public float BobSpeed = 10;
			public float BobForce = 1;

			public float SpeedDamping;

			public float MaxFallSpeedOnActivation = 2;

			public float RiseAccel = 1;
			public float MaxRiseSpeed = 5;

			public float          VacuumForce = 20;
			public AnimationCurve VacuumHorizontalScaling;
		}
	}
}
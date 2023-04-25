using System;
using Anjin.Nanokin;
using Anjin.Nanokin.Park;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[RequireComponent(typeof(Stunnable))]
	public class RaptorActor : ActorKCC, IEncounterActor
	{
		// @formatter:off
		[Title("Design")]
		[SerializeField] private RaptorActorSettings Settings;
		[FormerlySerializedAs("_movePuffs"),Title("FX")]
		[SerializeField] private ParticleSystem MovePuffs;
		[FormerlySerializedAs("_dashPoof"),SerializeField]                   private ParticleSystem DashPoof;
		[FormerlySerializedAs("_landPoof"),SerializeField]                   private ParticleSystem LandPoof;
		[FormerlySerializedAs("_spriteRenderer"),SerializeField]             private SpriteRenderer SpriteRenderer;
		[FormerlySerializedAs("_afterImageCooldownDuration"),SerializeField] private float          AfterImageCooldownDuration = 0.12f;

		[NonSerialized, ShowInPlay] public Stunnable Stunnable;

		[FormerlySerializedAs("_walkAnimSpeedCurve"),Title("Animation")]
		[InfoBox("X-axis: percent between 0-1 indicating how close we are to max speed.\nY-axis: animation play speed.")]
		[SerializeField] private AnimationCurve WalkAnimSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
		[FormerlySerializedAs("_minSpeedForWalkAnimation"),SerializeField] private float MINSpeedForWalkAnimation;
		// @formatter:on

		[ShowInPlay] private TankMoveState _moveState;
		[ShowInPlay] private JumpState     _dashState;
		[ShowInPlay] private FallState     _fallState;

		private bool      _hasDashedThisFrame;
		private bool      _hasLandedThisFrame;
		private float     _afterImageCooldown;


		public override StateKCC GetDefaultState() => _moveState;

		public override float MaxJumpHeight => 0;

		protected override void Awake()
		{
			base.Awake();

			Stunnable = gameObject.GetComponent<Stunnable>();
		}

		protected override void RegisterStates()
		{
			RegisterState(_moveState = new TankMoveState(Settings.MoveState));
			RegisterState(_dashState = new JumpState(Settings.JumpSettings));
			RegisterState(_fallState = new FallState(Settings.FallSettings));
		}

		protected override void UpdateFX()
		{
			base.UpdateFX();

			MovePuffs.SetPlaying(_moveState && HasVelocity);
			DashPoof.SetPlaying(_hasDashedThisFrame);
			LandPoof.SetPlaying(_hasLandedThisFrame);

			_hasDashedThisFrame = false;
			_hasLandedThisFrame = false;

			if (velocity.magnitude > 0.1f)
			{
				// We're going fast enough for after-images.
				_afterImageCooldown -= Time.deltaTime;

				if (_afterImageCooldown < 0)
				{
					_afterImageCooldown = AfterImageCooldownDuration;

					if (SpriteRenderer)
					{
						GameEffects.Live.SpawnAfterImage(SpriteRenderer.transform.position + SpriteRenderer.transform.rotation * Vector3.forward * 0.1f, SpriteRenderer.sprite);
					}
				}
			}
		}

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			if (IsGroundState)
			{
				if (!Motor.GroundingStatus.IsStableOnGround)
					return _fallState;

				if (inputs.jumpPressed)
				{
					_hasDashedThisFrame = true;

					currentVelocity += JoystickOrFacing * Settings.DashForce;
					_dashState.WithDefaultHeight(ref currentVelocity);
					return _dashState;
				}

				return null;
			}
			else if (IsAirState)
			{
				if (Motor.GroundingStatus.IsStableOnGround)
				{
					_hasLandedThisFrame = true;
					return _moveState;
				}
			}

			return null;
		}

		protected override void Update()
		{
			if (!GameController.OverworldEnemiesActive && hasBrain && !activeBrain.IgnoreOverworldEnemiesActive)
				return;

			base.Update();

			if (Stunnable.Stunned)
				// Override with nothing. (cannot receive inputs while stunned)
				inputs = CharacterInputs.DefaultInputs;
		}


		public override void UpdateRenderState(ref RenderState state)
		{
			if (stateChanged)
			{
				state = new RenderState(AnimID.Stand);
			}

			if (_moveState)
			{
				if (velocity.magnitude > MINSpeedForWalkAnimation)
				{
					state.animID    = AnimID.Walk;
					state.animSpeed = WalkAnimSpeedCurve.Evaluate(_moveState.MaxSpeedProgress);
				}
				else
				{
					state.animID = AnimID.Stand;
				}
			}
			else if (_dashState)
			{
				state.animID      = AnimID.Jump;
				state.animPercent = 0;
			}
		}

		public bool IsAggroed {
			get {
				if (activeBrain is RaptorBrain lbrain) {
					switch (lbrain.State) {

						/*case RaptorBrain.States.Idle:    break;
						case RaptorBrain.States.Return:  break;
						case RaptorBrain.States.Meander: break;*/

						case RaptorBrain.States.Alert:
						case RaptorBrain.States.Chase:
						case RaptorBrain.States.SafetyJump:
						case RaptorBrain.States.PounceWindup:
						case RaptorBrain.States.Pounce:
						//case RaptorBrain.States.Wait:
							return true;

						default: return false;
					}
				}

				return false;
			}
		}

		[Serializable]
		private class MoveState : GroundState
		{
			[FormerlySerializedAs("_speed"), SerializeField]
			private float Speed;

			[FormerlySerializedAs("_velocitySharpness"), SerializeField]
			private float VelocitySharpness;

			public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
			{
				Vector3 groundNormal   = actor.GetGroundNormal(currentVelocity);
				Vector3 targetVelocity = actor.Motor.GetDirectionTangentToSurface(inputs.move, groundNormal) * Speed;
				currentVelocity = MathUtil.LerpWithSharpness(currentVelocity, targetVelocity, VelocitySharpness, deltaTime);
			}
		}
	}
}
using Anjin.Actors.States;
using Anjin.Nanokin;
using Anjin.Nanokin.Park;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;
using DG.Tweening;
using Combat.Data.VFXs;
using Combat.Toolkit;
using UnityUtilities;
using Anjin.Util;

namespace Anjin.Actors
{
	public class NanokeeperActor : ActorKCC, IEncounterActor
	{
		// @formatter:off
		[Title("Design")]
		[SerializeField] private NanokeeperActorSettings Settings;

		[ShowInPlay] private TankMoveState _moveState;
		[ShowInPlay] private FallState	   _fallState;

		public bool IsAggroed
		{
			get
			{
				if (activeBrain is NanokeeperBrain nbrain)
				{
					switch (nbrain.State)
					{

						/*case RaptorBrain.States.Idle:    break;
						case RaptorBrain.States.Return:  break;
						case RaptorBrain.States.Meander: break;*/

						case NanokeeperBrain.States.Chase:
							//case RaptorBrain.States.Wait:
							return true;

						default: return false;
					}
				}

				return false;
			}
		}

		public override float MaxJumpHeight => 0;

		[System.NonSerialized, ShowInPlay] public Stunnable Stunnable;

		[FormerlySerializedAs("_walkAnimSpeedCurve"), Title("Animation")]
		[InfoBox("X-axis: percent between 0-1 indicating how close we are to max speed.\nY-axis: animation play speed.")]
		[SerializeField] private AnimationCurve WalkAnimSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
		[FormerlySerializedAs("_minSpeedForWalkAnimation"), SerializeField] private float MINSpeedForWalkAnimation;
		[FormerlySerializedAs("_spriteRenderers"), SerializeField] private List<SpriteRenderer> SpriteRenderers;

		public override StateKCC GetDefaultState() => _moveState;

		private BlinkVFX chaseVFX;

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			if (IsGroundState)
			{
				if (!Motor.GroundingStatus.IsStableOnGround)
				{
					return _fallState;
				}

				//if ((activeBrain is NanokeeperBrain nbrain) && (nbrain.State == NanokeeperBrain.States.Chase))
				//{
				//	currentVelocity = JoystickOrFacing * 2;

				//	return _moveState;
				//}
				//else
				//{
				//	return _idleState;
				//}

				return null;
			}
			else if (IsAirState)
			{
				if (Motor.GroundingStatus.IsStableOnGround)
				{
					return _moveState;
				}
			}

			return null;
		}

		protected override void RegisterStates()
		{
			RegisterState(_moveState = new TankMoveState(Settings.MoveState));
			RegisterState(_fallState = new FallState(Settings.FallState));
		}

		protected override void Awake()
		{
			base.Awake();

			Stunnable = gameObject.GetComponent<Stunnable>();

			chaseVFX = new BlinkVFX(5f, 0.5f, ColorsXNA.Red, ColorsXNA.White);
			chaseVFX.paused = true;
		}

		protected override void Update()
		{
			if (!GameController.OverworldEnemiesActive && hasBrain && !activeBrain.IgnoreOverworldEnemiesActive)
				return;

			base.Update();

			if (Stunnable.Stunned)
				// Override with nothing. (cannot receive inputs while stunned)
				inputs = CharacterInputs.DefaultInputs;

			if (activeBrain is NanokeeperBrain nbrain)
			{
				if (nbrain.State == NanokeeperBrain.States.Chase)
				{
					if (!vfx.Contains(chaseVFX))
					{
						chaseVFX.elapsed = 0;
						chaseVFX.paused = false;

						vfx.Add(chaseVFX);
					}
				}
				else
				{
					if (vfx.Contains(chaseVFX))
					{
						chaseVFX.paused = true;

						vfx.Remove(chaseVFX);
					}
				}
			}
			else
			{
				if (vfx.Contains(chaseVFX))
				{
					chaseVFX.paused = true;

					vfx.Remove(chaseVFX);
				}
			}

			//if (vfx != null)
			//{
			//	VFXState vfxstate = vfx.state;

			//	Color tint = vfxstate.tint.Alpha(vfxstate.opacity);
			//	Color fill = vfxstate.fill;
			//	float emission = vfxstate.emissionPower;

			//	foreach (var renderer in SpriteRenderers)
			//	{
			//		renderer.color = tint;
			//		renderer.ColorFill(fill);
			//		renderer.EmissionPower(emission);
			//	}
			//}
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			if (stateChanged)
			{
				state = new RenderState(AnimID.Stand);
			}

			if (velocity.magnitude > MINSpeedForWalkAnimation)
			{
				state.animID = AnimID.Walk;
				state.animSpeed = WalkAnimSpeedCurve.Evaluate(_moveState.MaxSpeedProgress);
			}
			else
			{
				state.animID = AnimID.Stand;
			}
		}
	}
}

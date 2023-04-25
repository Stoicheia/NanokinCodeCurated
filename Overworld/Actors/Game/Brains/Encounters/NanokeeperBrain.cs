using Anjin.Nanokin.Park;
using Anjin.Util;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class NanokeeperBrain : ActorBrain<NanokeeperActor>, ICharacterActorBrain
	{
		public enum States
		{
			Idle,
			Chase,
			Return
		}

		[SerializeField] private float _detectRadius = 5;
		[SerializeField] private float _speed = 2f;

		[Title("Timing")]
		[SerializeField, Range01] private float _idleLookProbability = 0.7f;
		[SerializeField] private FloatRange _delayBeforeIdleUpdateAfterFacing = new FloatRange(1.5f, 4f);

		[ShowInPlay] public States State;
		[ShowInPlay] private States _prevState;
		[ShowInPlay] private bool _enteredState;

		[ShowInPlay] private Vector3 _goal;
		[ShowInPlay] private Vector3 _facingDir;
		[ShowInPlay] private Vector3 _originalPosition;

		[ShowInPlay] private float _idleTimer;

		[ShowInPlay] private EncounterMonster _encounter;
		[ShowInPlay] private EncounterCollider _collider;
		[ShowInPlay] private NanokeeperActor _actor;

		public override int Priority => 0;

		private void Awake()
		{
			_encounter = gameObject.GetOrAddComponent<EncounterMonster>();
			_encounter.onSpawn += () =>
			{
				State = States.Idle;

				if (_actor.TryGetComponent(out Stunnable stunnable))
					stunnable.Stunned = false;

				_originalPosition = transform.position;
			};

			_collider = gameObject.GetOrAddComponent<EncounterCollider>();

			State = States.Idle;
			_prevState = State;
			_enteredState = true;
		}

		public override void OnTick(NanokeeperActor actor, float dt)
		{
			if (!ActorController.playerActive)
			{
				State = States.Idle;
				return;
			}
			Actor player = ActorController.playerActor;

			if (State != _prevState)
				_enteredState = true;
			_prevState = State;

			bool playerInRange = Vector3.Distance(_encounter.home, player.position) <= _encounter.homeRadius
			 && Vector3.Distance(player.position, actor.position) <= _detectRadius;

			Vector3 plr_vec = player.position - actor.position;
			Vector3 dir_to_player = plr_vec.normalized;
			float dist_to_player = plr_vec.magnitude;

			// Update the current state.
			switch (State)
			{
				case States.Idle:
					{
						if (playerInRange)
						{
							// Player entered our safe haven?! Ur dead kid
							State = States.Chase;
							_facingDir = actor.position.Towards(player.position);
							_goal = player.position;
							_goal.y = transform.position.y;

							//actor.Jump(0.75f);
							break;
						}

						_idleTimer -= dt;
						if (_idleTimer <= 0)
						{
							// Time for another idle movement.
							if (RNG.Chance(_idleLookProbability))
							{
								// A chance to just looking into a different direction.
								_facingDir = RNG.OnCircle;
								_idleTimer = _delayBeforeIdleUpdateAfterFacing.RandomInclusive;
							}
						}

						break;
					}

				case States.Chase:
					{
						if (playerInRange)
						{
							_goal = player.position;
							_goal.y = transform.position.y;
							_facingDir = actor.position.Towards(_goal);
						}
						else
						{
							_goal = _originalPosition;
							_facingDir = actor.position.Towards(_goal);

							State = States.Return;
						}

						break;
					}

				case States.Return:
					{
						if (playerInRange)
						{
							State = States.Chase;
							_facingDir = actor.position.Towards(player.position);
							_goal = player.position;
							_goal.y = transform.position.y;
						}
						else
						{
							_goal = _originalPosition;
							_facingDir = actor.position.Towards(_goal);

							float groundDistanceSq = Mathf.Pow(actor.position.x - _goal.x, 2) + Mathf.Pow(actor.position.z - _goal.z, 2);

							if (groundDistanceSq < 0.2f)
							{
								// Goal reached.
								State = States.Idle;
								_idleTimer = _delayBeforeIdleUpdateAfterFacing.RandomInclusive;
							}
						}

						break;
					}

				//case States.SafetyJump:
				//	{
				//		if (_enteredState)
				//		{
				//			_safetyJumpPerformed = false;
				//		}
				//		else if (_safetyJumpPerformed)
				//		{
				//			if (actor.IsMotorStable)
				//			{
				//				State = States.Wait;
				//				_waitTimer = 0.4f;
				//			}
				//		}

				//		break;
				//	}

				//case States.PounceWindup:
				//	{
				//		_waitTimer -= dt;

				//		if (_waitTimer <= 0)
				//		{
				//			State = States.Pounce;
				//			_pounceStartPosition = actor.position;
				//			_pounceDuration = 0;
				//		}


				//		break;
				//	}

				//case States.Pounce:
				//	{
				//		if (_enteredState)
				//		{
				//			_basePounceDirection = _pounceDirection;
				//		}

				//		if (Vector2.Angle(_pounceDirection.xz(), _basePounceDirection.xz()) <= PounceReaimLimitDegrees)
				//			_pounceDirection = Vector3.Lerp(_pounceDirection, dir_to_player, PounceReaimLerp);

				//		_pounceDuration += dt;

				//		if (!_pounceJumpPerformed && (_pounceDuration >= PounceJumpTime || Vector3.Distance(actor.position, player.position) <= 7f))
				//		{
				//			_pounceJumpPerformed = true;
				//			_actor.Jump(ref _actor.velocity, 0.75f, true);
				//			break;
				//		}

				//		if (_pounceDuration > MaxPounceDuration || (!(_actor.currentState is JumpState) && Vector3.Distance(actor.position, _pounceStartPosition) >= PounceLength))
				//		{
				//			_waitTimerState = States.Chase;
				//			State = States.Wait;
				//			_waitTimer = AfterPounceWait;
				//			_pounceCooldownTimer = PounceCooldown;
				//			_afterPounceCircleDirection = RNG.Sign;
				//			_afterPounceSafetyJump = true;
				//			_pounceJumpPerformed = false;
				//		}

				//		break;
				//	}
			}

			_collider.NeutralAdvantage = ((State == States.Chase) ? EncounterAdvantages.Enemy : EncounterAdvantages.Neutral);

			_enteredState = false;
		}

		public override void OnBeginControl(NanokeeperActor actor)
		{
			_actor = actor;
		}

		public override void OnEndControl(NanokeeperActor actor) { }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs = CharacterInputs.DefaultInputs;

			/*if (_actor.Stunnable && _actor.Stunnable.Stunned) {

				return;
			}*/

			Actor player = ActorController.playerActor;
			if (!player)
				return;

			if (State == States.Chase || State == States.Return)
			{
				// Movement states. We shall move towards the goal.
				inputs.move = actor.position.Towards(_goal);
				inputs.moveSpeed = _speed;
				inputs.moveMagnitude = inputs.move.magnitude;
				inputs.hasMove = inputs.moveMagnitude > Mathf.Epsilon;
			}
			else
			{
				inputs.look = _facingDir;
				inputs.NoMovement();
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{
			//inputs.jumpPressed = false;
		}
	}
}

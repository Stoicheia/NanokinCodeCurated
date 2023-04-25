using System;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Combat.Toolkit;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	/// <summary>
	/// AI for a raptor.
	/// - Meanders around the home in a radius r1.
	/// - Chase the player when they enter a radius r2 around the home.
	/// - Returns to a point in r1 when the player leaves r2.
	///
	/// r1 should be small. (safetyRadius serialized field)
	/// r2 should be larger. (HomeRadius property)
	/// </summary>
	public class RaptorBrain : ActorBrain<RaptorActor>, ICharacterActorBrain
	{
		public enum States
		{
			Idle,
			Meander,
			Alert,
			Chase,
			SafetyJump,
			PounceWindup,
			Pounce,
			Return,
			Wait
		}

		public float PounceBeginRadius       = 4;
		public float PounceJumpTime          = 0.4f;
		public float MaxPounceDuration       = 1.5f;
		public float PounceSpeed             = 15f;
		public float AfterPounceWait         = 0.75f;
		public float PounceReaimLerp         = 0.15f;
		public float PounceReaimLimitDegrees = 20f;
		public float PounceCooldown          = 2f;
		public float CirclingDistance        = 6f;

		public float PounceWindupDuration = 1.5f;
		public float PounceLength         = 5;

		public float SafetyJumpThreshold = 3f;
		public float SafetyJumpSpeed     = 8f;

		[SerializeField] private float _detectRadius   = 5;
		[SerializeField] private float _chaseRadius    = 10;
		[SerializeField] private float _distanceToDash = 3.5f;

		[Title("Timing")]
		[SerializeField] private float _delayBeforeReturnAfterChase = 1.5f;
		[SerializeField, Range01] private float      _idleLookProbability              = 0.7f;
		[SerializeField]          private FloatRange _delayBeforeIdleUpdateAfterMoving = new FloatRange(2, 4);
		[SerializeField]          private FloatRange _delayBeforeIdleUpdateAfterFacing = new FloatRange(1.5f, 4f);
		[SerializeField]          private FloatRange _maxMeanderTime = new FloatRange(0.75f, 2.5f);
		[SerializeField]          private FloatRange _dashCooldownDuration;

		[ShowInPlay] public  States State;
		[ShowInPlay] private States _prevState;
		[ShowInPlay] private bool   _enteredState;

		[ShowInPlay] private Vector3 _goal;
		[ShowInPlay] private Vector3 _facingDir;
		[ShowInPlay] private bool    _canDash;

		[ShowInPlay] private Vector3 _pounceStartPosition;
		[ShowInPlay] private Vector3 _pounceDirection;
		[ShowInPlay] private Vector3 _basePounceDirection;
		[ShowInPlay] private float   _pounceDuration;
		[ShowInPlay] private bool    _pounceJumpPerformed;
		[ShowInPlay] private float   _afterPounceCircleDirection;
		[ShowInPlay] private bool    _afterPounceSafetyJump;
		[ShowInPlay] private bool    _safetyJumpPerformed;


		[ShowInPlay] private float _idleTimer;
		[ShowInPlay] private float _dashCooldown;
		[ShowInPlay] private float _effectTimer;
		[ShowInPlay] private float _pounceCooldownTimer;
		[ShowInPlay] private float _meanderTimer;

		// TODO: any of this reusable?
		[ShowInPlay] private float            _waitTimer;
		[ShowInPlay] private States           _waitTimerState;
		[ShowInPlay] private EncounterMonster _encounter;
		[ShowInPlay] private RaptorActor      _actor;

		public override int Priority => 0;

		private void Awake()
		{
			_encounter = gameObject.GetOrAddComponent<EncounterMonster>();
			_encounter.onSpawn += () =>
			{
				State = States.Idle;

				if (_actor.TryGetComponent(out Stunnable stunnable))
					stunnable.Stunned = false;
			};

			State        = States.Idle;
			_prevState    = State;
			_enteredState = true;
		}

		public override void OnTick(RaptorActor actor, float dt)
		{
			if (!ActorController.playerActive) {
				State = States.Idle;
				return;
			}
			Actor player = ActorController.playerActor;

			if (State != _prevState)
				_enteredState = true;
			_prevState = State;

			bool playerInRange = Vector3.Distance(_encounter.home, player.position) <= _encounter.homeRadius
			 && Vector3.Distance(player.position, actor.position) <= _detectRadius;

			/*if (playerInRange)
			{
				if (!Nanokin.GameController.Live.EnemiesNearby.ContainsKey(gameObject.name))
				{
					Nanokin.GameController.Live.EnemiesNearby.Add(gameObject.name, gameObject);
				}
			}
			else
			{
				if (Nanokin.GameController.Live.EnemiesNearby.ContainsKey(gameObject.name))
				{
					Nanokin.GameController.Live.EnemiesNearby.Remove(gameObject.name);
				}
			}*/

			Vector3 plr_vec        = player.position - actor.position;
			Vector3 dir_to_player  = plr_vec.normalized;
			float   dist_to_player = plr_vec.magnitude;

			_dashCooldown -= dt;
			if (_dashCooldown <= 0)
				_canDash = true;

			// Update the current state.
			switch (State)
			{
				case States.Idle:
				{
					if (playerInRange)
					{
						// Player entered our safe haven?! Ur dead kid
						State     = States.Alert;
						_facingDir = actor.position.Towards(player.position);

						actor.Jump(0.75f);
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
						else
						{
							// Otherwise we simply meander to a different spot.
							State     = States.Meander;
							_goal      = RNG.InRadius(_encounter.home, _encounter.homeRadius);
							_idleTimer = _delayBeforeIdleUpdateAfterMoving.RandomInclusive;
						}
					}

					break;
				}

				case States.Meander:
				{
					_facingDir = actor.position.Towards(_goal);
					_meanderTimer -= dt;
					if (playerInRange)
					{
						State     = States.Alert;
						_facingDir = actor.position.Towards(player.position);

						actor.Jump(0.75f);
						break;
					}

					float groundDistanceSq = Mathf.Pow(actor.position.x - _goal.x, 2) + Mathf.Pow(actor.position.z - _goal.z, 2);

					if (groundDistanceSq < 0.2f || _meanderTimer <= 0)
					{
						// Goal reached.
						State     = States.Idle;
						_meanderTimer = _maxMeanderTime.RandomInclusive;
						_idleTimer = _delayBeforeIdleUpdateAfterMoving.RandomInclusive;
					}

					break;
				}

				case States.Alert:
				{
					if (actor.IsMotorStable)
						State = States.Chase;

					break;
				}

				case States.Chase:
				{
					if (_pounceCooldownTimer > 0)
						_pounceCooldownTimer -= dt;

					if (_enteredState && _afterPounceSafetyJump && dist_to_player <= SafetyJumpThreshold)
					{
						// Do a safety hop
						_afterPounceSafetyJump = false;
						State                 = States.SafetyJump;
						_goal                  = actor.position - dir_to_player;
						_waitTimerState        = States.Chase;
					}

					if (!ActorController.isSpawned || Vector3.Distance(player.position, _encounter.home) >= _chaseRadius)
					{
						// We've exceeded our chase radius.
						State          = States.Wait;
						_waitTimerState = States.Return;
						_waitTimer      = _delayBeforeReturnAfterChase;

						_goal = RNG.InRadius(_encounter.home, _encounter.homeRadius); // Goal for the return state afterwards
					}
					else
					{
						if (_pounceCooldownTimer <= 0)
						{
							// If we're ready to pounce, wait till we're close enough.
							_goal = player.position;

							float dot_to_player = Vector2.Dot(transform.forward.xz(), dir_to_player.xz());

							if (dot_to_player > 0.8f && dist_to_player <= PounceBeginRadius)
							{
								State           = States.PounceWindup;
								_waitTimer       = PounceWindupDuration;
								_pounceDirection = dir_to_player;
								_actor.vfx.Add(new HopVFX(3, PounceWindupDuration / 3, 0.5f));
							}
						}
						else
						{
							// If we're still waiting on the cooldown, circle the player
							Vector3 parallel_to_player = Vector3.Cross(dir_to_player, Vector3.up).normalized;
							_goal = player.position + (-dir_to_player * CirclingDistance) + parallel_to_player * 4f * _afterPounceCircleDirection;
						}
					}

					break;
				}

				case States.SafetyJump:
				{
					if (_enteredState)
					{
						_safetyJumpPerformed = false;
					}
					else if (_safetyJumpPerformed)
					{
						if (actor.IsMotorStable)
						{
							State     = States.Wait;
							_waitTimer = 0.4f;
						}
					}

					break;
				}

				case States.PounceWindup:
				{
					_waitTimer -= dt;

					if (_waitTimer <= 0)
					{
						State               = States.Pounce;
						_pounceStartPosition = actor.position;
						_pounceDuration      = 0;
					}


					break;
				}

				case States.Pounce:
				{
					if (_enteredState)
					{
						_basePounceDirection = _pounceDirection;
					}

					if (Vector2.Angle(_pounceDirection.xz(), _basePounceDirection.xz()) <= PounceReaimLimitDegrees)
						_pounceDirection = Vector3.Lerp(_pounceDirection, dir_to_player, PounceReaimLerp);

					_pounceDuration += dt;

					if (!_pounceJumpPerformed && (_pounceDuration >= PounceJumpTime || Vector3.Distance(actor.position, player.position) <= 7f))
					{
						_pounceJumpPerformed = true;
						_actor.Jump(ref _actor.velocity, 0.75f, true);
						break;
					}

					if (_pounceDuration > MaxPounceDuration || (!(_actor.currentState is JumpState) && Vector3.Distance(actor.position, _pounceStartPosition) >= PounceLength))
					{
						_waitTimerState             = States.Chase;
						State                      = States.Wait;
						_waitTimer                  = AfterPounceWait;
						_pounceCooldownTimer        = PounceCooldown;
						_afterPounceCircleDirection = RNG.Sign;
						_afterPounceSafetyJump      = true;
						_pounceJumpPerformed        = false;
					}

					break;
				}

				case States.Wait:
				{
					_waitTimer -= dt;

					if (_waitTimer <= 0)
						State = _waitTimerState;

					break;
				}

				case States.Return:
				{
					if (Vector3.Distance(player.position, _encounter.home) <= _chaseRadius)
					{
						// Player stepped into our chase radius again!
						State = States.Chase;
					}
					else if (Vector3.Distance(actor.position, _goal) < 0.2f)
					{
						// We've reached our return goal. We finally go back to being idle.
						State     = States.Idle;
						_idleTimer = _delayBeforeIdleUpdateAfterMoving.RandomInclusive;
						_facingDir = actor.facing;
					}

					break;
				}
			}

			_enteredState = false;

			// This throws an exception that doesnt print for some reason, big performance hit
			// Draw.Label2D(actor.currentPosition + (actor.HeadHeight + 0.75f) * Vector3.up, _state.ToString(), 20f, Color.red);
			// Draw.WireSphere(_goal, 0.25f, Color.red);
		}

		public override void OnBeginControl(RaptorActor actor)
		{
			_actor = actor;
		}

		public override void OnEndControl(RaptorActor actor) { }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs = CharacterInputs.DefaultInputs;

			/*if (_actor.Stunnable && _actor.Stunnable.Stunned) {

				return;
			}*/

			Actor player = ActorController.playerActor;
			if (!player)
				return;

			if (State == States.Chase
			    || State == States.Return
			    || State == States.Meander)
			{
				// Movement states. We shall move towards the goal.
				inputs.move = actor.position.Towards(_goal);
			}

			if (State != States.Chase && State != States.Return) // When chasing, we will let the cc handle it.
			{
				inputs.look = _facingDir;
			}

			if (State == States.PounceWindup)
			{
				inputs.look        = _pounceDirection.normalized;
				inputs.instantLook = true;

				float backup_start_percentage = 0.65f;
				float time_norm               = Mathf.Clamp01(_waitTimer / PounceWindupDuration);
				float backup_time_norm        = time_norm + (1 - backup_start_percentage);

				if (time_norm < backup_start_percentage)
				{
					// Debug.Log(time_norm + ", " + backup_time_norm);
					inputs.move = -_pounceDirection * Mathf.Sin(backup_time_norm * 0.5f) * Mathf.PI * 0.125f;
					//inputs.jumpPressed = true;
				}
				else
				{
					inputs.NoMovement();
					inputs.moveSpeed = 0;
				}
			}

			if (State == States.Pounce)
			{
				inputs.look        = _pounceDirection.normalized;
				inputs.instantLook = true;
				inputs.move        = _pounceDirection.normalized;
				inputs.moveSpeed   = Mathf.Lerp(character.velocity.magnitude, PounceSpeed, 0.2f);
			}

			if (State == States.Chase)
			{
				/*if (_canDash && player != null && Vector3.Distance(actor.currentPosition, player.currentPosition) <= _distanceToDash)
				{
					// Close in the remaining distance with a dash!
					inputs.jumpPressed = true;
					_canDash           = false;
					_dashCooldown      = _dashCooldownDuration.RandomInclusive;
				}*/
			}

			if (State == States.SafetyJump)
			{
				inputs.look        = actor.position.Towards(player.position);
				inputs.LookDirLerp = 1;

				if (!_safetyJumpPerformed)
				{
					_safetyJumpPerformed = true;
					inputs.move          = actor.position.Towards(_goal);
					inputs.moveSpeed     = SafetyJumpThreshold;
					inputs.jumpPressed   = true;
				}
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs.jumpPressed = false;
		}
	}
}
using System;
using Anjin.Nanokin.Park;
using Anjin.Util;
using UnityEngine;
using UnityUtilities;
using Util;

namespace Anjin.Actors
{
	/// <summary>
	/// AI for a surprise fish.
	/// - Waits for player to enter a radius.
	/// - Leaps at the player to surprise them.
	/// - Flops on the ground after
	/// </summary>
	public class SurpriseFishBrain : ActorBrain<SurpriseFishActor>, ICharacterActorBrain, IRecyclable
	{
		[SerializeField] private float       _detectRadius = 7.5f;
		[SerializeField] private float _resettedAlertDuration = 1f;
		[SerializeField] private ManualTimer _alertDuration;
		[SerializeField] private ManualTimer _holdDuration;
		[SerializeField] private ManualTimer _layDuration;
		[SerializeField] private float _overshootDive;

		private CharacterInputs   _inputs;
		private States            _state = States.Asleep;
		private EncounterMonster  _monster;
		private EncounterCollider _encounterCollider;

		private Vector3 initialPosition;

		private Vector3 diveDirection;

		public override int Priority => 0;

		private void Start()
		{
			initialPosition = transform.position;

			_monster           = GetComponent<EncounterMonster>();
			_encounterCollider = GetComponent<EncounterCollider>();
		}

		public void Recycle()
		{
			_state             = States.Asleep;
			transform.position = initialPosition;
			_alertDuration.Reset();
			_holdDuration.Reset();
			_layDuration.Reset();
		}

		public override void OnBeginControl(SurpriseFishActor actor) { }

		public override void OnTick(SurpriseFishActor actor, float dt)
		{
			_alertDuration.Update(dt);
			_holdDuration.Update(dt);
			_layDuration.Update(dt);

			if (!ActorController.playerActive)
				return;

			Actor player = ActorController.playerActor;
			// ----------------------------------------
			// State transitions...

			//_inputs.OnProcessed();
			//_inputs.processed = false;

			switch (_state)
			{
				case States.Asleep:
					if (Vector3.Distance(actor.position, player.position) < _detectRadius)
					{
						_inputs.swordPressed = true;
						_alertDuration.Restart();
						_state = States.Alert;
					}

					break;

				case States.Alert:
					if (_alertDuration.IsDone)
					{
						_alertDuration.Reset();
						_holdDuration.Restart();

						_inputs.jumpPressed = true;

						_state = States.Emerge;
					}
					else
					{
						//TODO: ???
					}

					break;

				case States.Emerge:
					_inputs.jumpHeld = true;
					if (_holdDuration.IsDone)
					{
						_holdDuration.Reset();

						var towardsPlayer = (player.center - actor.position).normalized;
						var overshoot = 1 + _overshootDive;
						diveDirection = new Vector3(towardsPlayer.x * overshoot, towardsPlayer.y, towardsPlayer.z * overshoot).normalized;
						diveDirection.y = Mathf.Clamp(diveDirection.y, -1, -0.1f);  //TODO: make this a setting later
						diveDirection = diveDirection.normalized;

						_inputs.divePressed = true;

						_state = States.Leap;
					}
					else
					{
						//TODO: rotate fish to face the player as it's aiming
					}

					break;

				case States.Leap:

						if (actor.hasWater)
						{
							_state = States.Asleep;

							_alertDuration.Duration = _resettedAlertDuration;

							initialPosition.x = transform.position.x;
							initialPosition.z = transform.position.z;

							_alertDuration.Reset();
							_holdDuration.Reset();
							_layDuration.Reset();

							_inputs.jumpPressed = false;
							_inputs.jumpHeld = false;
							_inputs.divePressed = false;

							transform.position = initialPosition;
						}
						if(actor.IsGroundState)
						{
							_layDuration.Restart();
							_state = States.Laying;
						}

					else
					{
						//TODO: ???
					}
					break;
				case States.Flopping:

						if (actor.hasWater)
						{
							_state = States.Asleep;

							_alertDuration.Duration = _resettedAlertDuration;

							initialPosition.x = transform.position.x;
							initialPosition.z = transform.position.z;

							_alertDuration.Reset();
							_holdDuration.Reset();
							_layDuration.Reset();

							_inputs.jumpPressed = false;
							_inputs.jumpHeld = false;
							_inputs.divePressed = false;

							transform.position = initialPosition;
						}
						if(actor.IsGroundState)
						{
							_layDuration.Restart();
							_state = States.Laying;
						}

					break;

				case States.Laying:
					if (_layDuration.IsDone)
					{
						_inputs.jumpPressed = true;
						_state = States.Flopping;
					}

					break;
			}

			_encounterCollider.NeutralAdvantage = _state == States.Leap ? EncounterAdvantages.Enemy : EncounterAdvantages.Player;
		}

		public override void OnEndControl(SurpriseFishActor actor) { }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs = _inputs;

			Actor player = ActorController.playerActor;
			if (!player)
				return;

			switch (_state)
			{
				case States.Alert:
					//actor.LeapGoal      = (player.position + Vector3.up).DropToGround();
					//inputs.jumpPressed = true;
					//_state = States.Emerge;

					break;
				case States.Emerge:
					if (actor.AtPeak)
					{
						Vector3 playerPosition = player.position;
						playerPosition.y = actor.center.y;

						inputs.look = (playerPosition - actor.center).normalized;
						inputs.move = diveDirection;
					}

					break;
				case States.Leap:
					if (!actor.IsGroundState)
					{
						inputs.move = diveDirection;
						inputs.moveSpeed = 1;	//TODO: make this a setting later
					}

					break;
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }

		private void OnDrawGizmos()
		{
			Draw2.DrawSphere(transform.position, _detectRadius, Color.yellow.Alpha(0.75f));
		}

		public enum States
		{
			Asleep,  // no player ---> i slep
			Alert,   // Boutta leap at this purple hair chick who just walked near this nuclear bomb
			Emerge,	 // Ready, aim...
			Leap,    // Mayday!!!
			Laying,  // Played like a fiddle. Anyway solid ground ain't so bad after all, fucking judgemental asshole fishes need to learn open-mindedness eh?
			Flopping // Flopping all over the place like a DUMBASS, last line of defense is to intimidate with breakdancing, turns out we were the idiot not the kid
		}
	}
}
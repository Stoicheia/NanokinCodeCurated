using Anjin.Nanokin.Park;
using Anjin.Util;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class CannoneerBrain : ActorBrain<CannoneerActor>, ICharacterActorBrain, IRecyclable
	{
		public enum States
		{
			Idle,
			Alert,
			WaitForNextShot,
			Recharge
		}

		[ShowInPlay] public States State;
		[ShowInPlay] private States _prevState;
		[ShowInPlay] private bool _enteredState;

		[SerializeField] private float _detectRadius = 10;

		[SerializeField] private int maxShots;

		[ShowInPlay] private Vector3 _originalDir;
		[ShowInPlay] private Vector3 _facingDir;

		[ShowInPlay] private int shotsFired;

		public override int Priority => 0;

		[ShowInPlay] private EncounterMonster _encounter;
		[ShowInPlay] private CannoneerActor _actor;
		private KinematicCharacterMotor _motor;

		[SerializeField] private ManualTimer _shotIntervalDuration;
		[SerializeField] private ManualTimer _shotDelayDuration;
		[SerializeField] private ManualTimer _rechargeDuration;
		[SerializeField] private ManualTimer _backToIdleDuration;

		private bool _rechargeStarted;
		private bool _rechargeEnded;
		private bool _playIdleAnimation;

		private Quaternion _originalRotation;

		[HideInInspector] public bool FireCannonball;
		[HideInInspector] public bool ShowSmoke;
		[HideInInspector] public bool HideSmoke;
		[HideInInspector] public bool PlayFireAnimInRecharge;
		[HideInInspector] public bool BackToIdle;

		private void Awake()
		{
			_encounter = gameObject.GetOrAddComponent<EncounterMonster>();
			_encounter.onSpawn += () =>
			{
				State = States.Idle;

				if (_actor.Stunnable)
					_actor.Stunnable.Stunned = false;
			};

			_motor = GetComponent<KinematicCharacterMotor>();

			State = States.Idle;
			_prevState = State;
			_enteredState = true;

			_rechargeStarted = false;
			_rechargeEnded = false;
			_playIdleAnimation = false;
			PlayFireAnimInRecharge = true;

			shotsFired = 0;

			_shotIntervalDuration.Reset();
			_shotDelayDuration.Reset();
			_rechargeDuration.Reset();
			_backToIdleDuration.Reset();

			_originalDir = transform.rotation * Vector3.forward;
			_originalRotation = transform.rotation;
		}

		public override void OnTick(CannoneerActor actor, float dt)
		{
			if (_actor.Stunnable && _actor.Stunnable.Stunned)
			{
				return;
			}

			_shotIntervalDuration.Update(dt);
			_shotDelayDuration.Update(dt);
			_rechargeDuration.Update(dt);
			_backToIdleDuration.Update(dt);

			if (!ActorController.playerActive)
			{
				State = States.Idle;
				return;
			}

			Actor player = ActorController.playerActor;

			if (State != _prevState)
				_enteredState = true;
			_prevState = State;

			//bool playerInRange = Vector3.Distance(_encounter.home, player.position) <= _encounter.homeRadius
			// && Vector3.Distance(player.position, actor.position) <= _detectRadius;

			//bool playerInRange = Vector3.Distance(player.position, actor.position) <= _detectRadius;
			bool playerInRange = _encounter.bounds.IsPointInBounds(player.position);

			Vector3 plr_vec = player.position - actor.position;
			plr_vec.y = 0;

			//Vector3 dir_to_player = plr_vec.normalized;
			//float dist_to_player = plr_vec.magnitude;

			if (!playerInRange && (State != States.Idle))
			{
				State = States.Idle;
				shotsFired = 0;

				FireCannonball = false;
				ShowSmoke = false;
				HideSmoke = true;
				BackToIdle = false;
				PlayFireAnimInRecharge = true;

				_rechargeEnded = false;
				_rechargeStarted = false;
				_playIdleAnimation = false;
			}

			Quaternion lookAt;

			switch (State)
			{
				case States.Idle:
					if (playerInRange)
					{
						State = States.Alert;
						_shotDelayDuration.Restart();
					}
					else
					{
						_facingDir = _originalDir;
						//_actor.View.transform.localEulerAngles = Vector3.zero;
						_motor.SetRotation(_originalRotation);

						FireCannonball = false;
						ShowSmoke = false;
						HideSmoke = true;
						BackToIdle = false;
						PlayFireAnimInRecharge = true;

						_rechargeEnded = false;
						_rechargeStarted = false;
						_playIdleAnimation = false;
					}

					break;
				case States.Alert:
					PlayFireAnimInRecharge = true;

					if (!_shotDelayDuration.IsPlaying && !_shotDelayDuration.IsDone)
					{
						_shotDelayDuration.Restart();
					}

					_facingDir = actor.position.Towards(player.position);

					lookAt = Quaternion.LookRotation(plr_vec, Vector3.up);
					_motor.SetRotation(lookAt);

					//_actor.View.transform.rotation = Quaternion.LookRotation(plr_vec, Vector3.up);

					//_actor.LoadCannonball();

					break;
				case States.WaitForNextShot:
					PlayFireAnimInRecharge = true;

					if (!_shotIntervalDuration.IsPlaying && !_shotIntervalDuration.IsDone)
					{
						_shotIntervalDuration.Restart();
					}

					_facingDir = actor.position.Towards(player.position);

					lookAt = Quaternion.LookRotation(plr_vec, Vector3.up);
					_motor.SetRotation(lookAt);

					//_actor.View.transform.rotation = Quaternion.LookRotation(plr_vec, Vector3.up);

					if (shotsFired >= maxShots)
					{
						State = States.Recharge;

						_rechargeDuration.Restart();
						_backToIdleDuration.Restart();

						_rechargeStarted = true;
					}
					else
					{
						if (_shotIntervalDuration.IsDone)
						{
							State = States.Alert;

							_shotDelayDuration.Restart();
						}
					}

					break;
				case States.Recharge:
					if (!_rechargeDuration.IsPlaying && !_rechargeDuration.IsDone)
					{
						_shotIntervalDuration.Restart();
					}

					_facingDir = actor.position.Towards(player.position);

					lookAt = Quaternion.LookRotation(plr_vec, Vector3.up);
					_motor.SetRotation(lookAt);

					if (_backToIdleDuration.IsDone)
					{
						PlayFireAnimInRecharge = false;
						_playIdleAnimation = true;
					}

					//_actor.View.transform.rotation = Quaternion.LookRotation(plr_vec, Vector3.up);

					if (_rechargeDuration.IsDone)
					{
						shotsFired = 0;

						_rechargeEnded = true;

						if (playerInRange)
						{
							_shotDelayDuration.Restart();

							State = States.Alert;
						}
						else
						{
							_shotDelayDuration.Reset();

							State = States.Idle;
						}

						_shotIntervalDuration.Reset();
					}

					break;
			}
		}

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (_actor.Stunnable && _actor.Stunnable.Stunned)
			{
				return;
			}

			inputs = CharacterInputs.DefaultInputs;

			Actor player = ActorController.playerActor;
			if (!player)
				return;

			if (_rechargeStarted)
			{
				_rechargeStarted = false;

				//inputs.glidePressed = true;
				ShowSmoke = true;
			}

			if (_rechargeEnded)
			{
				_rechargeEnded = false;

				//inputs.divePressed = true;
				HideSmoke = true;
			}

			if (_playIdleAnimation)
			{
				_playIdleAnimation = false;

				BackToIdle = true;
			}

			//inputs.look = _facingDir;
			//inputs.instantLook = true;

			switch (State)
			{
				case States.Idle:
					break;
				case States.Alert:
					if (_shotDelayDuration.IsDone)
					{
						if (shotsFired < maxShots)
						{
							++shotsFired;

							//inputs.swordPressed = true;
							FireCannonball = true;

							State = States.WaitForNextShot;

							_shotIntervalDuration.Restart();
						}
					}

					break;
				case States.WaitForNextShot:
					break;
				case States.Recharge:
					break;
			}
		}

		public void Recycle()
		{
			_shotIntervalDuration.Reset();
			_shotDelayDuration.Reset();
			_rechargeDuration.Reset();
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs.swordPressed = false;
			inputs.glidePressed = false;
			inputs.divePressed = false;
		}

		public override void OnBeginControl(CannoneerActor actor)
		{
			_actor = actor;
		}

		public override void OnEndControl(CannoneerActor actor) { }
	}
}

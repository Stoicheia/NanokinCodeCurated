using Anjin.Actors;
using Anjin.Util;
using KinematicCharacterController;
using Overworld.Terrains;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.Serialization;
using UnityUtilities;

namespace Anjin.Nanokin.Park
{
	/// <summary>
	/// Allows starting a fight by collision!
	/// Supports various specific advantage/disadvantage features
	///
	/// - Head bonk (like mario jumping on a Goomba) to start in advantage
	/// - Backstab to start in disadvantage
	/// </summary>
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(EncounterMonster))]
	[AddComponentMenu("Anjin: Events/Encounter on Collision")]
	public class EncounterCollider : SerializedMonoBehaviour
	{
		private Collider _col;

		// Features
		// ----------------------------------------
		public bool EnableNeutralTouch;
		public bool EnableHeadBonkAdvantage;
		public bool EnableBackstabDisadvantage;

		// Config
		// ----------------------------------------
		[Space]
		[ShowIf("EnableNeutralTouch")]
		public bool DisableNeutralTouchWhileStunnned = true;

		[FormerlySerializedAs("Advantage")]
		public EncounterAdvantages NeutralAdvantage = EncounterAdvantages.Neutral;

		[Tooltip("-1 = player is heads on. 0 = player is from the side. 1 = player is running away and is aligned with the monster's direction.")]
		[ShowIf("EnableBackstabDisadvantage")]
		[Range(-1, 1)]
		public float BackstabThreshold = -0.2f;

		private EncounterMonster _encounter;
		private Stunnable        _stunnable;
		private bool             _hasStunnable;

		private void Awake()
		{
			_encounter    = GetComponent<EncounterMonster>();
			_stunnable    = GetComponent<Stunnable>();
			_col = GetComponent<Collider>();
			_hasStunnable = _stunnable;
		}

		private void OnCollisionEnter(Collision collision)
		{
			HandleCollision(collision);
		}

		public void HandleCollision(Collision collision)
		{
			if (!collision.gameObject.TryGetComponent(out EncounterPlayer encounterPlayer) || encounterPlayer.Immune || GameController.Live.StateGame != GameController.GameState.Overworld)
				return;

			bool hasPlayerActor  = collision.gameObject.TryGetComponent(out ActorKCC playerActor);
			bool hasMonsterActor = gameObject.TryGetComponent(out ActorKCC monsterActor);
			bool hasMonsterMotor = gameObject.TryGetComponent(out KinematicCharacterMotor monsterMotor);

			// HEAD BONK
			// ----------------------------------------
			if (EnableHeadBonkAdvantage && hasPlayerActor && hasMonsterMotor)
			{
				Vector3 headPoint = transform.position
				                    + monsterMotor.CharacterTransformToCapsuleCenter.Towards(monsterMotor.CharacterTransformToCapsuleTop, 0.75f);

				Vector3 hitPoint = collision.contacts[0].point;
				if (hitPoint.y > headPoint.y)
				{
					if (_hasStunnable)
					{
						_stunnable.Stunned = true;
					}

					playerActor.OnBounce(new BounceInfo(0, Vector3.up, 1.25f, 0.85f, 1));
					_encounter.Trigger(EncounterAdvantages.Player);
					return;
				}
			}


			// BACK STAB
			// ----------------------------------------
			if (EnableBackstabDisadvantage && hasPlayerActor && hasMonsterActor && (!_hasStunnable || !_stunnable.Stunned))
			{
				Vector3 difference = playerActor.position - monsterActor.position;
				float monsterAdvantage = Vector3.Dot(difference.normalized, playerActor.facing.ChangeY(0) + monsterActor.facing.ChangeY(0));
				if (monsterAdvantage > 1 + BackstabThreshold)
				{
					_encounter.Trigger(EncounterAdvantages.Enemy);
					return;
				}
			}


			// NEUTRAL TOUCH
			// ----------------------------------------
			bool disableNeutralTouch = DisableNeutralTouchWhileStunnned && _hasStunnable && _stunnable.Stunned;
			if (!disableNeutralTouch)
				_encounter.Trigger(NeutralAdvantage);
		}

		public void HandleCollision(Collider collision)
		{
			if (!collision.gameObject.TryGetComponent(out EncounterPlayer encounterPlayer) || encounterPlayer.Immune || GameController.Live.StateGame != GameController.GameState.Overworld)
				return;

			bool hasPlayerActor  = collision.gameObject.TryGetComponent(out ActorKCC playerActor);
			bool hasMonsterActor = gameObject.TryGetComponent(out ActorKCC monsterActor);
			bool hasMonsterMotor = gameObject.TryGetComponent(out KinematicCharacterMotor monsterMotor);

			// HEAD BONK
			// ----------------------------------------
			if (EnableHeadBonkAdvantage && hasPlayerActor && hasMonsterMotor)
			{
				Vector3 headPoint = transform.position
				                    + monsterMotor.CharacterTransformToCapsuleCenter.Towards(monsterMotor.CharacterTransformToCapsuleTop, 0.75f);

				Vector3 hitPoint = _col.ClosestPoint(collision.transform.position);
				if (hitPoint.y > headPoint.y)
				{
					if (_hasStunnable)
					{
						_stunnable.Stunned = true;
					}

					playerActor.OnBounce(new BounceInfo(0, Vector3.up, 1.25f, 0.85f, 1));
					_encounter.Trigger(EncounterAdvantages.Player);
					return;
				}
			}


			// BACK STAB
			// ----------------------------------------
			if (EnableBackstabDisadvantage && hasPlayerActor && hasMonsterActor && (!_hasStunnable || !_stunnable.Stunned))
			{
				Vector3 difference = playerActor.position - monsterActor.position;
				float monsterAdvantage = Vector3.Dot(difference.normalized, playerActor.facing.ChangeY(0) + monsterActor.facing.ChangeY(0));
				if (monsterAdvantage > 1 + BackstabThreshold)
				{
					_encounter.Trigger(EncounterAdvantages.Enemy);
					return;
				}
			}


			// NEUTRAL TOUCH
			// ----------------------------------------
			bool disableNeutralTouch = DisableNeutralTouchWhileStunnned && _hasStunnable && _stunnable.Stunned;
			if (!disableNeutralTouch)
				_encounter.Trigger(NeutralAdvantage);
		}
	}
}
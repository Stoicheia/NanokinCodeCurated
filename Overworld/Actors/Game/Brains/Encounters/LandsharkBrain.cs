using System;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	/// <summary>
	/// AI for a landshark.
	/// - Patrol around the home in a radius r1. (never stops idle)
	/// - Chase the player when they enter a radius r2 around the home.
	/// - When the player is close enough, go underground for a brief movement then re-emerges under the player. (teleport)
	/// </summary>
	public class LandsharkBrain : ActorBrain<LandsharkActor>, ICharacterActorBrain
	{
		[FormerlySerializedAs("_teleportTimer"), SerializeField]
		private ManualTimer TeleportTimer;
		[FormerlySerializedAs("_emergeTimer"), SerializeField]
		private ManualTimer EmergeTimer;
		[FormerlySerializedAs("_returnTimer"), SerializeField]
		private ManualTimer ReturnTimer;
		[FormerlySerializedAs("_minimumPatrolPerimeterCoverage"), SerializeField, Range01]
		private float MinimumPatrolPerimeterCoverage = 0.5f; // A ratio of the perimeter to patrol, e.g. 0.5 = radius, 1 = diameter
		[FormerlySerializedAs("_detectRadius"), SerializeField]
		private float DetectRadius = 15;
		[FormerlySerializedAs("_chaseRadius"), SerializeField]
		private float ChaseRadius = 25;
		[FormerlySerializedAs("_teleportDistance"), SerializeField]
		private float TeleportDistance = 3.5f; // Distance before the shark goes underground and re-emerges under the player.
		[FormerlySerializedAs("_velocityPredictionInfluence"), SerializeField]
		private float VelocityPredictionInfluence = 0.75f;

		[FormerlySerializedAs("_gizmoColorDetectRadius"), Title("Gizmos")]
		[SerializeField] private Color GizmoColorDetectRadius = Color.yellow.Alpha(.2f);
		[FormerlySerializedAs("_gizmoColorChaseRadius"), SerializeField]
		private Color GizmoColorChaseRadius = Color.red.Alpha(.2f);
		[FormerlySerializedAs("_gizmoColorTeleportRadius"), SerializeField]
		private Color GizmoColorTeleportRadius = Color.green.Alpha(.2f);

		[Title("Debug")]
		[ShowInPlay, NonSerialized] public States State;
		[ShowInPlay] private Vector3 _goal;

		private EncounterMonster _encounter;

		public override int Priority => 0;

		public Vector3 Position => transform.position;

		private void Awake()
		{
			_encounter = gameObject.GetOrAddComponent<EncounterMonster>();
			_encounter.onSpawn += () =>
			{
				State = States.Patrol;

				if (TryGetComponent(out Stunnable stunnable))
					stunnable.Stunned = false;
			};
		}

		private void Start()
		{
			State = States.Patrol;
			_goal  = GetNextPatrolGoal();
		}

		public override void OnTick(LandsharkActor actor, float dt)
		{
			TeleportTimer.Update(dt);
			EmergeTimer.Update(dt);
			ReturnTimer.Update(dt);
		}

		private void ChaseOrPatrol(float playerDistance)
		{
			if (!ActorController.isSpawned) {
				State = States.Patrol;
				return;
			}

			bool shouldPatrol = playerDistance >= ChaseRadius;
			bool shouldChase  = playerDistance <= DetectRadius;

			if (shouldPatrol && State == States.Patrol) return; // Already patrolling.
			if (shouldChase && State == States.Chase) return;   // Already chasing.

			if (shouldPatrol)
			{
				// Player too far, stop chasing.
				State = States.Patrol;
				_goal  = RNG.InRadius(_encounter.home, _encounter.homeRadius);

				/*if (Nanokin.GameController.Live.EnemiesNearby.ContainsKey(gameObject.name))
				{
					Nanokin.GameController.Live.EnemiesNearby.Remove(gameObject.name);
				}*/
			}
			else if (shouldChase)
			{
				State = States.Chase;

				/*if (!Nanokin.GameController.Live.EnemiesNearby.ContainsKey(gameObject.name))
				{
					Nanokin.GameController.Live.EnemiesNearby.Add(gameObject.name, gameObject);
				}*/
			}
		}

		private Vector3 GetNextPatrolGoal()
		{
			Vector3 ret;
			bool    grounded;

			do
			{
				ret = RNG.InRadius(_encounter.home, _encounter.homeRadius);
				ret = ret.DropToGround(out grounded);
			} while (!grounded && Vector3.Distance(ret, Position) < _encounter.homeRadius * MinimumPatrolPerimeterCoverage);

			return ret;
		}

		public override void OnBeginControl(LandsharkActor actor) { }

		public override void OnEndControl(LandsharkActor actor) { }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			Actor player = ActorController.playerActor;
			if (!player)
				return;

			float playerDistance = Vector3.Distance(transform.position, player.position);

			// ----------------------------------------
			// State transitions...

			switch (State)
			{
				case States.Chase when playerDistance <= TeleportDistance:
					State = States.Teleporting;
					break;

				// Perform the teleportation after a delay
				case States.Teleporting when TeleportTimer.JustEnded:
					TeleportTimer.Reset();

					inputs.look        = actor.position.Towards(player.position);
					inputs.jumpPressed = true; // Emerge the shark actor.

					Vector3 playerVelocity = Vector3.zero;
					if (player is ActorKCC kccPlayer)
						playerVelocity += kccPlayer.velocity * Time.fixedDeltaTime;

					actor.Motor.SetPosition((player.position + playerVelocity.Change3(y: 0) * VelocityPredictionInfluence).DropToGround()); // Warp underneath the player.
					actor.Reorient(Position.Towards(player.position).Change3(y: 0).normalized);

					State = States.Emerge;
					break;

				// Burrow back underground after a delay
				case States.Emerge when EmergeTimer.JustEnded:
					// Spent enough time in emerge. Time to return home.
					EmergeTimer.Reset();
					State = States.Return;
					break;

				case States.Return when ReturnTimer.IsDone && Vector3.Distance(_encounter.home, player.position) > Mathf.Max(DetectRadius, ChaseRadius) + 1:

					ReturnTimer.Reset();

					// Re-emerge somewhere in the home radius.
					actor.Motor.SetPosition(RNG.InRadius(_encounter.home, _encounter.homeRadius));
					ChaseOrPatrol(playerDistance);
					break;

				// Bidirection transition between patrol & chase.
				case States.Patrol:
				case States.Chase:
					ChaseOrPatrol(playerDistance);
					break;
			}

			// ----------------------------------------
			// State Functionality...

			switch (State)
			{
				case States.Patrol:
					if (Vector3.Distance(actor.position, _goal) < 0.2f)
						// Goal reached, pick a new one:
						_goal = GetNextPatrolGoal();

					// Move towards the goal.
					inputs.move = actor.position.Towards(_goal);
					break;

				case States.Chase:
					inputs.move = actor.position.Towards(player.position);
					break;

				case States.Teleporting:
					TeleportTimer.PlayOrContinue();
					inputs.diveHeld = true;
					break;

				case States.Emerge:
					EmergeTimer.PlayOrContinue();
					break;

				case States.Return:
					ReturnTimer.PlayOrContinue();
					inputs.diveHeld = true; // We hide underground during the return phase. (works like 'teleporting')
					break;
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs = CharacterInputs.DefaultInputs;
		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();
			switch (State)
			{
				case States.Patrol:
					Draw.WireSphere(actor.position, DetectRadius, GizmoColorDetectRadius);
					break;

				case States.Chase:
					Draw.WireSphere(actor.position, ChaseRadius, GizmoColorChaseRadius);
					Draw.WireSphere(actor.position, TeleportDistance, GizmoColorTeleportRadius);
					break;
			}

			Draw.Arrow(transform.position, _goal, Vector3.up, 0.25f, Color.cyan);
		}

		public enum States
		{
			Patrol,      // Actively patrolling the home area.
			Chase,       // Chase the player.
			Teleporting, // Awaiting instant warp to Emerge under the player.
			Emerge,      // Has emerged under the player.
			Return       // Returning home after emerging.
		}
	}
}
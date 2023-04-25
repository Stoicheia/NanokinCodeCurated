using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Actors.States;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Assets.Scripts.Utils;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dreamteck.Splines;
using Overworld.Interactables;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	public interface ICoasterController
	{
		void OnTrackEnd(CoasterTrack.EndBehaviors behavior, CoasterTrack track, CoasterCarActor actor);

		void OnCarVoided(CoasterCarActor actor);
		void OnCoasterTrigger(string     trigger, SplineUser user);

		void OnActorTryEnter(Actor actor);
		void OnActorExit(Actor     actor);

		bool CoasterActive { get; }
	}

	public partial class CoasterCarActor : Actor, IStateMachineUser, IRenderStateModifier, IInteractable, ITriggerable
	{
		public enum States {
			Moving,
			Switching,
			Falling,
		}

		public enum PhysModes {
			Follower,
			Manual
		}

		public enum LeanStates
		{
			Off,
			Left,
			Right
		}

		[Required]
		public CoasterConfig Config;

		public CoasterTrack Track;
		public Actor        Rider;
		public Transform    RiderTiltRoot;
		public float        DefaultSpeed;

		public LayerMask   OverlapMask;
		public BoxCollider OverlapCollider;

		public RollerCoaster Coaster;

		// States
		//-------------------------------------------------------
		public State GetDefaultState() => Idle;

		[ShowInInspector] public IdleState       Idle;
		[ShowInInspector] public RideState       Ride;
		[ShowInInspector] public JumpPointState  JumpPoint;
		[ShowInInspector] public VoidedState     Voided;
		[ShowInInspector] public SwordSwingState SwordSwing;

		[NonSerialized, ShowInPlay] public MinecartInputs  Inputs;
		[NonSerialized, ShowInPlay] public CoasterUIInputs UIInputs;

		[SerializeField] private ParticlePrefab FX_SwordSweepPrefab;
		[SerializeField] private ParticlePrefab FX_SwordHitPrefab;

		[NonSerialized, ShowInPlay] public float            Speed;
		//[NonSerialized, ShowInPlay] public MinecartMinigame Minigame;

		[NonSerialized, ShowInPlay]
		public ICoasterController Controller;

		//[NonSerialized, ShowInPlay] public States     State;

		[NonSerialized, ShowInPlay] public StateMachine State;
		[NonSerialized, ShowInPlay] public PhysModes    PhysMode;

		[NonSerialized, ShowInPlay] public LeanStates   LeanState;
		[NonSerialized, ShowInPlay] public bool			LeanTransitioning;
		[NonSerialized, ShowInPlay] public float		LeanTransitioninStart;
		[NonSerialized, ShowInPlay] public ValTimer		LeanTransitionTimer;

		[NonSerialized, ShowInPlay] public SplineFollower Follower;

		[NonSerialized, ShowInPlay] private float _distance;
		[NonSerialized, ShowInPlay] private float _trackLength;

		[NonSerialized, ShowInPlay]
		private FixedArray<TrackJumpPoint> _jumpPointsInside;
		private FixedArray<CoasterObstacle> _obstacles;

		[ShowInPlay] private Vector3 _velocity;
		[ShowInPlay] private float   _leanNorm;

		private static Collider[] _tmpOverlaps = new Collider[8];

		protected override void Awake()
		{
			base.Awake();

			if (!Follower)
				Follower = GetComponent<SplineFollower>();

			_distance = 0;

			_jumpPointsInside = new FixedArray<TrackJumpPoint>(20);
			_obstacles        = new FixedArray<CoasterObstacle>(4);


			State      = new StateMachine(this);

			Idle      = State.Register(new IdleState());
			Ride      = State.Register(new RideState());
			JumpPoint = State.Register(new JumpPointState());
			Voided    = State.Register(new VoidedState());

			State.Register(SwordSwing);
			State.Boot();

			//State     = States.Moving;

			LeanState           = LeanStates.Off;
			LeanTransitioning   = false;
			LeanTransitionTimer = new ValTimer();

			Follower.onEndReached       += percent => OnEndReachedBasedOnDirection(Follower.direction);
			Follower.onBeginningReached += percent => OnEndReachedBasedOnDirection(Follower.direction);
		}

		void OnEndReachedBasedOnDirection(Spline.Direction direction)
		{
			if(Controller != null && Controller.CoasterActive && !Follower.spline.isClosed)
			{
				Controller.OnTrackEnd(Track.EndBehavior, Track, this);

				switch (Track.EndBehavior)
				{
					case CoasterTrack.EndBehaviors.None:
						break;

					case CoasterTrack.EndBehaviors.Voided:
						Follower.SetPercent(direction == Spline.Direction.Forward ? 1 : 0);
						Void();
						break;

					case CoasterTrack.EndBehaviors.JumpTo:
						break;
				}

			}
		}

		protected override void Start()
		{
			base.Start();
			if (Rider != null)
			{
				Rider.renderer.modifier = this;
			}
		}

		[Button]
		public void SetTrack(CoasterTrack track, float distance)
		{
			Track     = track;
			_distance = distance;

			Follower.wrapMode = track.Spline.isClosed ? SplineFollower.Wrap.Loop : SplineFollower.Wrap.Default;
			Follower.spline   = track.Spline;
			Follower.RebuildImmediate();
			Follower.SetDistance(_distance);

			_trackLength = Track.Spline.CalculateLength();

			if (Track != null) {
				Speed = Track.BaseSpeed;
			} else {
				Speed = DefaultSpeed;
			}
		}

		protected override void Update()
		{
			base.Update();

			Inputs = MinecartInputs.Default;
			if (activeBrain is ICharacterInputProvider<MinecartInputs> provider) {
				provider.PollInputs(ref Inputs);
			}

			_jumpPointsInside.Reset();
			UIInputs = CoasterUIInputs.Default;

			// Update States
			State.Update(Time.deltaTime);

			if (GameController.IsWorldPaused || Coaster == null || Controller == null || !Controller.CoasterActive) return;

			// Update jump points inside
			_distance = Follower.CalculateLength(0, Follower.GetPercent());

			//Track.Spline.

			if (Coaster.JumpPointsPerTrack.TryGetValue(Track, out List<TrackJumpPoint> jumpPoints))
			{
				for (var i = 0; i < jumpPoints.Count; i++)
				{
					TrackJumpPoint point = jumpPoints[i];
					if (_distance >= point.Track1_Dist1 && _distance < point.Track1_Dist2)
						_jumpPointsInside.Add(point);
				}
			}


			// Update overlaps
			_obstacles.Reset();
			if (OverlapMask != 0)
			{
				int overlapCount = Physics.OverlapBoxNonAlloc(OverlapCollider.transform.position, OverlapCollider.size, _tmpOverlaps, OverlapCollider.transform.rotation, OverlapMask, QueryTriggerInteraction.Collide);
				for (int i = 0; i < overlapCount; i++) {
					Collider collider = _tmpOverlaps[i];

					if (_obstacles.Full || !collider.TryGetComponent(out CoasterObstacle obstacle) || !obstacle.Active) continue;

					if (_leanNorm > 0.1f && obstacle.Side == CoasterObstacle.Sides.Left) continue;
					if (_leanNorm < -0.1f && obstacle.Side == CoasterObstacle.Sides.Right) continue;

					_obstacles.Add(obstacle);
				}
			}

			//_tiltNorm = Mathf.Sin(Time.time);


			// Update Physics
			UpdatePhysics(Time.deltaTime);

			if (Rider) {
				RiderTiltRoot.localRotation = Quaternion.Euler(0,0, transform.rotation.eulerAngles.z);
			}
		}


		public void UpdatePhysics(float dt)
		{
			switch (PhysMode)
			{
				case PhysModes.Follower:
					// Let the follower run
					float minSpeed   = Config.MinSpeed;
					float maxSpeed   = Config.MaxSpeed;
					float friction   = Config.Friction;
					float gravity    = Config.Gravity;
					float slopeRange = Config.SlopeRange;

					float dotPercent = 0;

					var spline = Follower.spline;
					if (spline != null)
					{
						var sample = spline.Evaluate(Follower.GetPercent());

						float dot = Vector3.Dot(sample.forward, Vector3.down);
						dotPercent = Mathf.Lerp(-slopeRange / 90f, slopeRange / 90f, (dot + 1f) / 2f);
					}

					Speed -= Time.deltaTime * friction /* * (1f - brakeForce)*/;

					float speedAdd     = 0f;
					float speedPercent = Mathf.InverseLerp(minSpeed, maxSpeed, Speed);

					// Apply gravity forces
					if (dotPercent > 0f)
						speedAdd = gravity * dotPercent * Config.SpeedAccelleration.Evaluate(speedPercent) * Time.deltaTime;
					else
						speedAdd = gravity * dotPercent * Config.SpeedDecelleration.Evaluate(1f - speedPercent) * Time.deltaTime;

					// Acceleration -> speed
					Speed += speedAdd /* * (1f -brakeForce)*/;
					Speed =  Mathf.Clamp(Speed, minSpeed, maxSpeed);

					// Addition forces
					/*if (addForce > 0f) {
						float lastAdd = addForce;
						addForce =  Mathf.MoveTowards(addForce, 0f, Time.deltaTime * 30f);
						speed    += lastAdd - addForce;
					}*/

					// Apply speed
					/*follower.followSpeed =  speed;
					follower.followSpeed *= (1f - brakeForce);*/

						/*if (Track != null)
					{
						//var min = Track.BaseSpeed.ValueOrDefault()
						Speed = Track.BaseSpeed;
					}
					else
					{
						Speed = Config.MinSpeed;
					}*/

					Follower.Move(Speed * Time.deltaTime);

					/*if (LeanTransitioning) {
						if (!LeanTransitionTimer.Tick(0)) {
							switch (LeanState)
							{
								case LeanStates.Left:
									_leanNorm = Mathf.Lerp(LeanTransitioninStart, -1, LeanTransitionTimer.norm_0to1);
									break;
							}
						} else {
							LeanTransitioning = false;
						}

					} else {

					}*/


					// Leaning
					_leanNorm = _leanNorm.Clamp(-1, 1);

					Follower.motion.offset = new Vector2(
						_leanNorm			* Config.MaxTiltOffset.x,
						_leanNorm.Abs()	* Config.MaxTiltOffset.y
					);

					var rot = Follower.motion.rotationOffset;
					Follower.motion.rotationOffset = new Vector3(rot.x, rot.y, -_leanNorm * Config.MaxTiltAngle);

					break;

				case PhysModes.Manual:
					// Do nothing... for now
					break;
			}
		}

		public void ChangeLeanState(LeanStates next, float time)
		{
			if (!LeanTransitioning) {
				LeanTransitioning     = true;
				LeanTransitioninStart = _leanNorm;
				LeanTransitionTimer.Set(time);
			} else {

			}

			LeanState = next;
		}

		public void ChangePhysMode(PhysModes next)
		{
			if (PhysMode == next) return;

			PhysMode = next;

			switch (next)
			{
				case PhysModes.Follower:
					Follower.enabled = true;
					Follower.follow  = true;
					Follower.RebuildImmediate();
					Follower.SetDistance(_distance);
					break;
				case PhysModes.Manual:
					Follower.enabled = false;
					Follower.follow  = false;
					break;
			}
		}


		public State GetNextState(float dt)
		{
			if (Voided)
				return null;

			if ((Ride || SwordSwing) && _obstacles.Count > 0)
			{
				/*_velocity = transform.forward * Speed;
				Speed     = -3;*/
				Void();
				Controller.OnCarVoided(this);

				return Voided;
			}

			if (SwordSwing)
			{
				if (State.elapsedTime > Config.Swing.Duration)
					return Ride;
			}

			if (JumpPoint && JumpPoint.IsDone)
				return Ride;

			if (Ride)
			{

				if (Inputs.swordPressed)
				{
					if(Inputs.leanLeft) {
						SwordSwing.Direction = SwordSwingState.Directions.Left;
						FX_SwordSweepPrefab.Instantiate(SwordSwing.LeftFXRoot, SwordSwing.LeftFXRoot.position, SwordSwing.LeftFXRoot.rotation);
					} else if(Inputs.leanRight) {
						SwordSwing.Direction = SwordSwingState.Directions.Right;
						FX_SwordSweepPrefab.Instantiate(SwordSwing.RightFXRoot, SwordSwing.RightFXRoot.position, SwordSwing.RightFXRoot.rotation);
					} else {
						SwordSwing.Direction = SwordSwingState.Directions.Forwards;
						FX_SwordSweepPrefab.Instantiate(SwordSwing.ForwardsFXRoot, SwordSwing.ForwardsFXRoot.position, SwordSwing.ForwardsFXRoot.rotation);
					}

					return SwordSwing;
				}

				if (Inputs.jumpPressed)
				{
					bool left  = Inputs.leanLeft;
					bool right = Inputs.leanRight;

					TrackJumpPoint jp    = _jumpPointsInside[0];

					if (Ride.LeftJP != null || Ride.RightJP != null) {
						jp = null;
						if (left  && Ride.LeftJP) jp  = Ride.LeftJP;
						if (right && Ride.RightJP) jp = Ride.RightJP;
					}

					if (jp) {
						return JumpPoint.WithJumpPoint(jp, Mathf.Clamp01((_distance - jp.Track1_Dist1) / Mathf.Abs(jp.Track1_Dist1 - jp.Track1_Dist2)));
					}
				}
			}

			return null;
		}

		// TODO (C.L. 02-8-2023): Multiple riders here

		public void AddRider(Actor actor)
		{
			actor.renderer.modifier = this;
			Rider = actor;
		}

		public void RemoveRider(Actor actor)
		{
			actor.renderer.modifier = null;
			Rider = null;
		}

		public void OnBeforeDeactivate(State prev, State next)
		{
			if (next is CoasterState cstr) ChangePhysMode(cstr.PhysMode);
		}

		public void OnChangeState(State prev, State next)		{ }
		public void OnBeforeActivate(State prev, State next)	{ }

		public void ModifyRenderState(ActorRenderer renderer, ActorBase actor, ref RenderState state)
		{
			state      = new RenderState(AnimID.Sit, 0.25f);
			//state.roll = RiderTiltRoot.localRotation.eulerAngles.z;
		}

		public async UniTask DoTrackSwap(TrackJumpPoint jp, float dist)
		{
			Vector3 target = jp.Track2.Spline.EvaluatePosition(jp.Track2.Spline.Travel(0, dist));
			await transform.DOJump(target, 3, 1, 0.45f).ToUniTask();
		}

		public void Void()
		{
			if (Voided) return;

			_velocity = transform.forward * Speed;
			Speed     = -3;
			State.Change(Voided);
		}

		public void ToRide()
		{
			if (Ride) return;

			_velocity = Vector3.zero;
			Speed         = 0;
			State.Change(Ride);
		}

		public void OnInteract(Actor actor)
		{
			Controller.OnActorTryEnter(actor);
		}

		public bool IsBlockingInteraction(Actor actor) => Controller == null;

		public void OnTrigger(Trigger source, Actor actor, TriggerID triggerID = TriggerID.None)
		{
			if (triggerID == TriggerID.Enter)
			{
				Controller.OnActorTryEnter(actor);
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Threading;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Util;
using API.Spritesheet.Indexing.Runtime;
using Cysharp.Threading.Tasks;
using Drawing;
using ImGuiNET;
using Pathfinding;
using UnityEditor;
using UnityEngine;
using Util;
using Util.Components.Timers;
using Util.ConfigAsset;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	/// <summary>
	/// Anything that implements this should be able to tell party members what to do.
	/// </summary>
	public interface IPartyLeader {
		PartyMode Mode { get; }
		bool PollInstructions(PartyMemberBrain brain, out PartyMemberInstructions instructions);
	}

	[Flags]
	public enum PanicFlags {
		None = 1 << 0,
		FromNoValidPosition = 1 << 1,
		FromPathTooLong = 1 << 2,

		All = FromNoValidPosition | FromPathTooLong,
	}


	/// <summary>
	/// Everything needed to tell a party member character how to act when following.
	/// </summary>
	public struct PartyMemberInstructions
	{
		public Vector3? GoalPosition;
		public Vector3? GoalPositionLocked;
		public Vector3? PrevGoalPosition;
		public Vector3  FacingDirection;

		/// <summary>
		/// If set, this is the position the party member will try to use when panic teleporting instead of the goal.
		/// </summary>
		public Vector3? PanicGoal;

		public bool  IsMoving;
		public bool  GoalPosInMotion;
		public bool  NoSnap;
		public bool  IsRunning;
		public float LeaderSpeed;

		//public bool Swimming;

		public PanicFlags PanicFlags;

		public bool wait;

		/// <summary>
		/// To be used if the party member is in motion, so they know which direction to walk if they're already close enough to the target point.
		/// </summary>
		public Vector3 DirectionAtGoalPosition;

		public static PartyMemberInstructions Default = new PartyMemberInstructions
		{
			GoalPosition       = null,
			GoalPositionLocked = null,
			PrevGoalPosition   = null,
			PanicGoal          = null,
			PanicFlags         = PanicFlags.All,
			IsMoving           = false,
			//Swimming		   = false,
		};
	}

	public class PartyMemberBrain : ActorBrain<NPCActor>, ICharacterActorBrain, IAnimOverrider /*IAnimOverrider*/
	{
		public enum States
		{
			Idle,
			ToGoal,
			FollowingPath,
			Panic,
			ModeTransition,
		}

		public enum CalculationState
		{
			Ready,
			Cooldown,
			Calculating,
			NoValidPath
		}

		public class Settings {
			public RangeOrFloat SwimJumpInDelay;
			public RangeOrFloat SwimJumpOutDelay;
			public float        SwimEntryWaitTime = 0.1f;
		}

		public SettingsAsset<Settings> SettingsAsset;
		public Settings                settings => SettingsAsset;

		// SETTINGS
		//========================================================
		public float RecalculationCooldown    = 0.5f;
		public float MaxRecalculationWaitTime = 2;

		public float PanicCooldown     = 3f;
		public float PanicPathDistance = 100f;
		public float PanicGoalDistance = 100f;

		public int MaxFailedCalculations = 3;
		//========================================================

		public static bool registered;

		// TODO(C.L.): Move this somewhere else?
		public static bool Setting_EnableAI       = true;
		public static bool Setting_CanPanic       = true;
		public static bool Setting_AutoPathRecalc = true;

		// Note(C.L. 7-14-22): these will affect all instances of this brain, and get reset in LateUpdate
		[NonSerialized, ShowInPlay] public static bool manualRecalc;
		[NonSerialized, ShowInPlay] public static bool hardPanic;


		[NonSerialized, ShowInPlay] public  IPartyLeader Leader;
		[ShowInPlay]                private bool         _hasLeader;

		// Note(C.L. 7-14-22): This is mainly for the debug menu. If we need a leader control stack for some reason we'll add one.
		[NonSerialized, ShowInPlay] public  IPartyLeader SecondaryLeader;
		[ShowInPlay]                private bool         _hasSecondaryLeader;

		[NonSerialized, ShowInPlay] private PartyMemberInstructions Instructions;

		[NonSerialized, ShowInPlay] public PartyMode Mode;
		[NonSerialized, ShowInPlay] public States    State;

		//[ShowInPlay] public Queue<PartyMode> _modeTransitionQueue;
		[ShowInPlay] public PartyMode        _transitionNextMode;
		//public ValTimer  _transitionTimer;

		// Path Calculation
		[NonSerialized, ShowInPlay] public  CalculationState CalcState;
		[NonSerialized, ShowInPlay] public  MPPath           path;
		[NonSerialized, ShowInPlay] public  MPPath           nextPath;
		[NonSerialized, ShowInPlay] public  bool             WantsRecalculation;
		[ShowInPlay]                private ValTimer         _recalculateCooldownTmr;


		[NonSerialized, ShowInPlay] private int                   FailedCalculations;
		private                             ABPath                _calculatingPath;
		[NonSerialized, ShowInPlay] private (NNInfo hit, bool ok) LastValidNavmeshPos;
		[NonSerialized, ShowInPlay] private (NNInfo hit, bool ok) ValidNavmeshPos;

		[NonSerialized, ShowInPlay] public bool StraightShot;
		[NonSerialized, ShowInPlay] public bool PrevStraightShot;

		[ShowInPlay] private Vector3 _mostRecentGoal;
		[ShowInPlay] private Vector3 _prevPos;

		[ShowInPlay] private PathFollowState _pathState;
		//[ShowInPlay] private int             _pathSegment;
		[ShowInPlay] private float           _pathSpeed;

		[ShowInPlay] private Vector3 _prevGoal;

		[ShowInPlay] private ValTimer _panicCooldownTmr;
		[ShowInPlay] private ValTimer _calculationTmr;
		[ShowInPlay] private ValTimer _calculationCooldownTmr;

		[ShowInPlay] private ValTimer _waitTmr;

		private bool _isCalcDone => CalcState == CalculationState.Ready || CalcState == CalculationState.Cooldown;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void SubsystemReg()
		{
			registered   = false;
			manualRecalc = false;
		}

		private void Awake()
		{
			Leader          = null;
			SecondaryLeader = null;

			_hasLeader          = Leader          != null;
			_hasSecondaryLeader = SecondaryLeader != null;
		}

		public void Start()
		{
			CalcState = CalculationState.Ready;
			State     = States.Idle;

			Mode = PartyMode.Ground;

			//_modeTransitionQueue = new Queue<PartyMode>();

			if (!registered)
			{
				registered           =  true;
				DebugSystem.onLayout += ONLayout;
				DebugSystem.RegisterMenu("PartyAI");
			}

			//_pathRecalcCTS = new CancellationTokenSource();
		}

		public void SetLeader(IPartyLeader leader)
		{
			Leader     = leader;
			_hasLeader = leader != null;
		}

		public void SetSecondaryLeader(IPartyLeader leader)
		{
			SecondaryLeader     = leader;
			_hasSecondaryLeader = leader != null;
		}

		public void InitiateModeTransition(PartyMode mode, Vector3 newGoal)
		{
			State               = States.ModeTransition;
			_transitionNextMode = mode;
			actor.JumpTo(newGoal);

			if(mode == PartyMode.Swimming) {
				actor.DelayTimer.Set(settings.SwimJumpInDelay.Evaluate());
				_waitTmr.Set(settings.SwimEntryWaitTime);
			} else if (mode == PartyMode.Ground) {
				actor.DelayTimer.Set(settings.SwimJumpOutDelay.Evaluate());
			}
		}

		public override void OnTick(NPCActor actor, float dt)
		{
			if (GameController.IsWorldPaused || !_hasLeader && !_hasSecondaryLeader) return;

			IPartyLeader currentLeader = Leader;
			if (_hasSecondaryLeader)
				currentLeader = SecondaryLeader;

			// Poll instructions
			if(!currentLeader.PollInstructions(this, out Instructions))
				Instructions = PartyMemberInstructions.Default;

			Vector3 charPos = actor.transform.position;

			if (ValidNavmeshPos.ok)
				LastValidNavmeshPos = ValidNavmeshPos;

			if(CalcState == CalculationState.Cooldown)
				_calculationCooldownTmr.Tick();

			_recalculateCooldownTmr.Tick();

			Vector3? maybeGoal = Instructions.GoalPositionLocked;
			bool     hasGoal   = maybeGoal.HasValue;
			Vector3  goal      = transform.position;

			if (!hasGoal) {
				path         = null;
				//_pathSegment = 0;
			} else {
				goal = maybeGoal.Value;
			}

			if (State != States.ModeTransition) {

				bool waiting = !_waitTmr.Tick();

				// Handle mode switching
				if (Mode != Leader.Mode && hasGoal) {
					if(!waiting)
						InitiateModeTransition(Leader.Mode, goal);
				} else {

					GraphLayer layer        = GraphLayer.Main;
					float      searchRadius = 0.2f;

					if (Mode == PartyMode.Swimming) {
						layer        = GraphLayer.Water;
						searchRadius = 4f;
						actor.State  = NPCState.Swimming;
					}

					if(!waiting) {
						ValidNavmeshPos = MotionPlanning.GetPosOnNavmesh(charPos, layer, searchRadius: searchRadius);

						PrevStraightShot = StraightShot;
						StraightShot = !MotionPlanning.RaycastOnGraph(ValidNavmeshPos.hit.position, goal, out var hit, layer, searchRadius: searchRadius)
									   && hit.node     != null
									   && hit.distance < 10
									   // This is to prevent detecting a straight shot through the ground.
									   && (hit.distance < 0.75f || !Physics.Linecast(ValidNavmeshPos.hit.position + Vector3.up * 0.1f,
																					 goal                         + Vector3.up * 0.1f,
																					 Layers.Walkable.mask, QueryTriggerInteraction.Ignore));

						UpdateCalculation(goal, layer, searchRadius);
					}

				}
			} else {

				// Handle mode transition
				if (actor.State != NPCState.Jump) {

					if (_transitionNextMode != Leader.Mode && hasGoal) {
						InitiateModeTransition(Leader.Mode, goal);
					} else {
						State = States.Idle;
						Mode  = _transitionNextMode;
					}
				}
			}

		}

		private void UpdateCalculation(Vector3 goal, GraphLayer layer, float searchRadius)
		{

			switch (CalcState)
			{
				case CalculationState.Ready:


					// Should we recalculate the path?
					// - NOT if the character isn't on the navmesh.
					// - If we don't have a straight shot to the goal, and we're further away then 10 meters.

					// - NOT If we're following a path that still reaches the goal.

					// ReSharper disable once ReplaceWithSingleAssignment.False
					bool recalculate     = false;
					bool can_recalculate = true;

					bool goalChanged = Vector3.Distance(_prevGoal, goal) > 0.01;

					if (ValidNavmeshPos.ok && !StraightShot && goalChanged)
						recalculate = true;

					if (!goalChanged && path != null)
					{
						recalculate = false;

						var last_point          = path.Nodes[path.Nodes.Count - 1];
						var straightShotFromEnd = !MotionPlanning.RaycastOnGraph(goal, last_point.point, out var hit2, layer, searchRadius);

						if (!straightShotFromEnd || (hit2.node != null && hit2.distance > 7))
						{
							recalculate = true;
						}
					}

					if (!ValidNavmeshPos.ok) {
						recalculate     = false;
					}

					// If we're following a path, and doing a path action, we don't want to start a recalc
					if (State == States.FollowingPath && (actor.State != NPCState.Ground && actor.State != NPCState.Swimming)) {
						recalculate     = false;
						can_recalculate = false;
					}

					if ((can_recalculate && Setting_AutoPathRecalc && (recalculate || WantsRecalculation) && _recalculateCooldownTmr.done) || manualRecalc)
					{
						//Debug.Log($"Begin Calculation to {goal}");
						CalcState = CalculationState.Calculating;

						_calculationTmr.Set(MaxRecalculationWaitTime);
						_calculationCooldownTmr.Set(0.25f);

						StartRecalc(transform.position, goal);

						WantsRecalculation = false;
						_prevGoal       = goal;
					}

					break;

				case CalculationState.Cooldown:
					if (_calculationCooldownTmr.done) {
						//Debug.Log($"Calc Cooldown Done");
						CalcState = CalculationState.Ready;
					}
					break;

				case CalculationState.Calculating:
					if (_calculationTmr.Tick()) {
						EndPathing();
						CalcState = CalculationState.Ready;
					}
					break;

				case CalculationState.NoValidPath:
					// Other parts of the code should handle this.
					break;
			}
		}

		private void LateUpdate()
		{
			manualRecalc = false;
			hardPanic    = false;
		}

		public async UniTask StartRecalc(Vector3 start, Vector3 goal)
		{
			CalcState = CalculationState.Calculating;

			CalcSettings settings = CalcSettings.Default;
			settings.layer = GraphLayer.Main | GraphLayer.Water;

			(MPPath calculatedPath, bool ok) = await MotionPlanning.CalcPath(start, goal, settings);

			if (!ok)
			{
				path             = null;
				_calculatingPath = null;
				FailedCalculations++;

				// If no valid path is found, the system will try again for a set number of iterations, then trip over into NoValidPath
				if (FailedCalculations > MaxFailedCalculations) {
					CalcState = CalculationState.NoValidPath;
				} else {
					_recalculateCooldownTmr.Set(RecalculationCooldown);
					CalcState = CalculationState.Ready;
				}

				return;
			}

			if (path == null) {
				path                    = calculatedPath;
				_pathState.index        = 0;
				_pathState.just_started = true;
			} else {

				nextPath = calculatedPath;
			}

			FailedCalculations = 0;
			//Debug.Log($"Calculation Done");

			//_calculationCooldownTmr.Set();
			CalcState          = CalculationState.Cooldown;
		}

		// Attempt to follow the leader's instructions.
		//-------------------------------------------------------------------------------------------

		// TODO:
		// Lua Callbacks,
		// Getting stuck off the navmesh,
		// Panic,
		// Fix Straight Shot in the air/in the ground,
		// Funnel Simplification.
		// Snapping to ground.

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (!Setting_EnableAI /*|| GameController.Live.IsCutsceneControlled*/) return;

			if (!_waitTmr.done) {
				inputs.NoMovement();
				return;
			}

			Vector3  charPos = actor.transform.position;
			Vector3? goal    = Instructions.GoalPositionLocked;
			_mostRecentGoal = goal.GetValueOrDefault(charPos);

			if (hardPanic)
			{
				State = States.Panic;
				path  = null;
			}

			// TRANSITION: If we have no goal, switch to idle.
			if (State != States.Idle && !goal.HasValue)
			{
				State = States.Idle;
			}

			switch (State)
			{
				case States.Idle:

					//We should start moving if:
					//	We aren't at the goal position.
					//  The goal position is valid.
					//	We have a straight shot, or we have a valid path.

					inputs.NoMovement();

					if (!goal.HasValue)
						break;

					if (StraightShot) {
						State = States.ToGoal;
					} else if ((path != null && path.Nodes.Count > 0) || (nextPath != null && nextPath.Nodes.Count > 0)) {
						State = States.FollowingPath;
						_pathState            = new PathFollowState(0, 0);
						_pathState.target_end = true;
					} else {

						// If we have no valid path from our position, all we can do is panic (provided the instructions let us)
						if (CalcState == CalculationState.NoValidPath && Instructions.PanicFlags.HasFlag(PanicFlags.FromNoValidPosition) && Setting_CanPanic) {
							Panic();
						} else if(_isCalcDone) {
							WantsRecalculation = true;
						}
					}

					break;

				case States.ToGoal:

					if (!StraightShot)
					{
						State = States.Idle;
						break;
					}

					Vector3 validGoal = goal.Value;

					float speed = Instructions.IsMoving ? Instructions.LeaderSpeed : actor.CSettings.WalkSpeed;

					const float speedup = 1.5f;
					const float maxDist = 4;

					float dist = Vector3.Distance(charPos, validGoal);

					bool willPass = MotionPlanning.WillPassTarget(charPos, validGoal, speed, Time.deltaTime);

					inputs.hasMove = true;

					if (dist < 0.1f || willPass)
					{
						inputs.NoMovement();
						if (Instructions.GoalPosInMotion && !Instructions.NoSnap)
							character.Teleport(_mostRecentGoal);
					}
					/*else if(willPass) {
						inputs.moveSpeed = Mathf.Min(actor.WalkSpeed, dist / Time.deltaTime);
					}*/
					else
					{
						inputs.moveSpeed = speed + speedup * Mathf.Clamp01(dist / maxDist) + 0.3f;
						inputs.move      = (validGoal - charPos).normalized;
					}

					break;

				case States.FollowingPath:

					if (path == null)
					{
						if (nextPath != null) {
							path = nextPath;
						} else {
							State = States.Idle;
							break;
						}
					}

					if (nextPath != null && CalcState != CalculationState.Calculating && (actor.State == NPCState.Ground || actor.State == NPCState.Swimming)) {
						_pathState.index        = 0;
						_pathState.just_started = true;

						path                    = nextPath;
						nextPath                = null;

						// TODO: This can fail because the actor may have been despawned while this recalc was taking place.
						/*if (path.Nodes.Count > 0) {
							actor.Teleport(path.Nodes[0].point);
						}*/
					}

					if (StraightShot)
					{
						EndPathing();
						State = States.ToGoal;
						break;
					}

					if (path.Length() > PanicPathDistance && Instructions.PanicFlags.HasFlag(PanicFlags.FromPathTooLong) && Setting_CanPanic) {
						Panic();
						inputs.NoMovement();
						break;
					}

					// ONLY if our paths don't have lots of verts.
					//if(distToTarget > 2)
					_pathSpeed = Mathf.Lerp(_pathSpeed, actor.CSettings.WalkSpeed * 1.75f, 0.25f);
					/*else
						PathSpeed = Mathf.Lerp(PathSpeed, actor.WalkSpeed * 0.75f, 0.25f);*/

					var output = MotionPlanning.FollowPath(path, ref _pathState, charPos, _pathSpeed);

					if(output.reached_index.Item1) {
						//Debug.Log($"{actor}: reached index {output.reached_index.Item2}");
					}

					if (output.action != MPAction.Move && output.actionTarget.HasValue && output.actionStart.HasValue)
					{
						//actor.Teleport(output.actionTarget.Value.point);
						StartAction(output.action, output.actionStart.Value, output.actionTarget.Value, output.actionHeight);
					}
					else
					{
						inputs.hasMove   = true;
						inputs.move      = output.direction;
						inputs.moveSpeed = output.speed.GetValueOrDefault(_pathSpeed);
					}

					if (output.reached_target)
					{
						EndPathing();
					}
					break;

				case States.Panic:

					if (_panicCooldownTmr.Tick()) {
						State = States.Idle;
					}

					break;
			}

			//
			// NOTE: Old
			//

			/*PrevStraightShot = StraightShot;
			StraightShot = !MotionPlanning.RaycastOnGraph(onNavmeshState.hit.position, goal, out var hit)
							&& hit.node != null
							&& hit.distance < 10;*/

			//bool panic = false;
			/*if (!StraightShot && PrevStraightShot) {
				Debug.DrawLine(hit.origin, hit.point, ColorsXNA.Azure, 10f);
			}

			if (StraightShot) {
				Debug.DrawLine(hit.origin, hit.point, ColorsXNA.Goldenrod, 1f);
			}*/


			if (ValidNavmeshPos.ok && StraightShot)
			{
				/*inputs.hasMove = true;
				float speed = Instructions.IsMoving ? Instructions.LeaderSpeed : actor.WalkSpeed;

				var speedup = 1.5f;
				var maxDist = 4;
				var dist = Vector3.Distance(charPos, goal);

				bool willPass = MotionPlanning.WillPassTarget(charPos, GoalPos, PrevPos, speed);

				if (dist < 0.1f || willPass) {
					inputs.NoMovement();
					if (Instructions.GoalPosInMotion && !Instructions.NoSnap)
						character.Teleport(GoalPos);
				}
				else if(willPass) {
					inputs.moveSpeed = Mathf.Min(actor.WalkSpeed, dist / Time.deltaTime);
				}
				else  {
					inputs.moveSpeed = speed + speedup * Mathf.Clamp01(dist / maxDist) + 0.3f;
					inputs.move      = goal  - charPos;
				}*/
			}
			else
			{
				/*var reaches = false;

				if (PathState.Path != null)
					reaches = PathState.Path.ReachesDestination(0.2f) &&
							  PathState.state == MPState.Running &&
							  PathState.Path.Destination == goal;

				//THIS IS THE ISSUE: PATH CALCULATED AT ODD TIME DOES NOT REACH GOAL!
				if (Vector3.Distance(charPos, goal) >= UpdateDistance && !reaches) {

					if (UpdateTimer <= 0) {
						UpdateTimer = UpdateRate;
						if(NextState.state == MPState.Idle ||
						   NextState.state == MPState.PathError) {
							StartSettings.SetTargetPos(goal);
							NextState.Start(StartSettings);
                            Debug.Log("Repath "+name);
						}
					}

					UpdateTimer -= Time.deltaTime;
				}

				if (NextState.state == MPState.Calculating) {
					MotionPlanning.Pathing_UpdateState(ref NextState, charPos);
					if(NextState.state == MPState.PathError && NextState.error == MPError.CouldNotFind)
						panic = true;
				}

				if (NextState.state == MPState.Running) {
					PathState = NextState;

					//TEMP
					PathState.follower_speed = 1;
					PathState.prev_node = (PathState.Path.BaseNodes[0], true);
					PathState.current_segment = 0;

					NextState.ResetToIdle();
				}

				var result = MotionPlanning.Pathing_UpdateState(ref PathState, charPos);

				if(PathState.state == MPState.Running)
					character.LUA_OnPathUpdate(result, PathState);

				if (result.reached_node)
					character.LUA_OnPathReachNode(result, PathState);

				//If we arrived at a node that
				if (result.action != MPAction.Move && PathState.state == MPState.Running) {
					PathState.state = MPState.FollowerAction;

					Debug.Log("Handler "+name);

					switch (result.action) {
						case MPAction.FallDown:
							/*actor.State = NPCState.Fall;
							break;#1#
						case MPAction.JumpUp:
						case MPAction.JumpAcross:
							actor.State = NPCState.Jump;
							break;

						case MPAction.Teleport:
							//character.Teleport();
							break;
					}

					actor.actionStart = result.node.point;
					var next = PathState.Path.GetPath()[PathState.current_segment];
					actor.actionTarget = next.point;

					actor.actionTimer = 0;
				}

				if (PathState.state == MPState.FollowerAction && actor.State == NPCState.ReachedTarget) {
					PathState.state = MPState.Running;
				}

				inputs.move 	 = PathState.follower_dir;
				inputs.hasMove   = true;
				inputs.moveSpeed = actor.WalkSpeed * PathState.follower_speed;

				PathState.distance_traveled += Vector3.Distance(charPos, charPos + (PathState.follower_dir * PathState.follower_speed * Time.deltaTime));*/
			}

			/*if (panic && Setting_CanPanic) {
				Debug.Log(name + " Panicked");
				character.Teleport(GoalPos);
			}*/

			if (Vector3.Distance(charPos, _mostRecentGoal) < 0.5f)
				inputs.look = Instructions.FacingDirection;
			else
				inputs.look = null;

			_prevPos = charPos;
		}

		public void ResetCalcState()
		{
			CalcState               = CalculationState.Ready;
			_recalculateCooldownTmr.Set(0);
		}

		public void EndPathing()
		{
			State      = States.Idle;
			path       = null;
			nextPath   = null;
			_pathState = PathFollowState.Default;


			// If we reached the end of the path and we don't have a straight shot to the target, we need to try more pathing.
			if (!StraightShot)
				WantsRecalculation = true;
		}

		public void StartAction(MPAction action, MPNode start, MPNode target, Option<float> height)
		{
			//Debug.Log($"{actor}: start action: {action}, {start}, {target}");

			switch (action)
			{
				case MPAction.Move: return;
				case MPAction.FallDown:
				case MPAction.JumpAcross:
				case MPAction.JumpUp:
					//Note(CL): This assumes we never have an action at the end of the path.
					actor.JumpTo(start.point, target.point, height);
					//_pathSegment++;
					/*if (PathSegment < path.Nodes.Count - 2)
						PathSegment++;*/
					/*else
						EndPathing();
						*/

					break;
				case MPAction.Teleport: break;
			}

			/*actor.actionStart  = start.point;
			actor.actionTarget = target.point;
			actor.actionTimer  = 0;
			actor.actionHeight = height;*/
		}

		/// <summary>
		/// Forces the party member to teleport back to the leader
		/// </summary>
		public void Panic()
		{
			State = States.Panic;
			EndPathing();
			ResetCalcState();

			if (Instructions.PanicGoal.HasValue)
				actor.Teleport(Instructions.PanicGoal.Value);
			else if (Instructions.GoalPositionLocked.HasValue)
				actor.Teleport(Instructions.GoalPositionLocked.Value);

			_panicCooldownTmr.Set(PanicCooldown);
		}

		// Animation
		//-------------------------------------------------------------------------------------------

		public override bool OverridesAnim(ActorBase    actor) => (State != States.ModeTransition || !_waitTmr.done) && (Instructions.IsRunning || Instructions.GoalPosInMotion || Mode == PartyMode.Swimming);
		public override void OnAnimEndReached(ActorBase actor) { }

		public override RenderState GetAnimOverride(NPCActor actor)
		{
			RenderState state = actor.renderer.state;
			AnimID      anim = AnimID.Stand;

			switch (Mode) {
				case PartyMode.Ground:
					anim = AnimID.Walk;

					state.offset.y = 0;

					if (Instructions.IsRunning || State == States.FollowingPath)
						anim = AnimID.Run;

					break;

				case PartyMode.Swimming:
					anim = AnimID.SwimIdle;

					if ((State == States.FollowingPath || Instructions.IsRunning || Instructions.GoalPosInMotion) && actor.renderer.Animable.indexing.HasAnimation(AnimID.SwimMove))
						anim = AnimID.SwimMove;

					break;
			}

			state.animID   = anim;

			return state;
		}

		// Unused interface methods
		//-------------------------------------------------------------------------------------------

		public          void ResetInputs(Actor character, ref CharacterInputs inputs) { }
		public override int  Priority => 0;

		void OnDrawGizmos()
		{
			if (path != null)
				MotionPlanning.DrawPathInEditor(path);

			if (StraightShot)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, _mostRecentGoal);
			}
			/* else if(PathState.Path != null)
				MotionPlanning.DrawPathInEditor(path);*/
		}


		void ONLayout(ref DebugSystem.State state)
		{
			if (state.IsMenuOpen("PartyAI"))
			{
				if (ImGui.Begin("PartyAI"))
				{
					ImGui.Checkbox("Enable AI", ref Setting_EnableAI);
					ImGui.Checkbox("Can Panic", ref Setting_CanPanic);
					ImGui.Checkbox("Auto Path Recalc", ref Setting_AutoPathRecalc);
					if (ImGui.Button("Recalculate"))
					{
						manualRecalc = true;
					}

					if (ImGui.Button("Hard Panic"))
					{
						hardPanic = true;
					}
				}

				ImGui.End();
			}
		}
	}
}
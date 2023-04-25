using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.MP;
using Anjin.Regions;
using Anjin.UI;
using Anjin.Util;
using Overworld.Controllers;
using Pathfinding;
using UnityEngine;
using UnityEngine.Profiling;
using Vexe.Runtime.Extensions;
using GraphLayer = Anjin.MP.GraphLayer;
using Random = System.Random;

namespace Anjin.Nanokin.ParkAI
{
	/*
	 *	TODO:
	 * 	- Add support for standing in queues!
	 *  - Add pathfinding system for traversing the graph!
	 *	- Add support for making another decision when one fails for some reason (e.g. we tried to interact with a POI but there were no open occupancy slots/the queue was full)
	 *	- Add basic occupancy for general spatial nodes (Mainly so peeps don't try to cram themselves into a tight corridor/space if it's already too full)
	 */


	/*
	 * Layers of the system:
	 * == Decisions ==:
	 * 		The "AI" part of the system. This is what drives each agent's behaviour for what they do and
	 * 		where they go. This part of the system should only need to update non-frequently.
	 *
	 * == Actions ==:
	 * 		Handles the agent performing actions that are decided by the decision engine. This part is responsible for
	 *		updating the desired velocity of the agent.
	 *
	 * 		Some actions include:
	 * 			- Standing
	 * 			- Walking
	 * 			- Looking towards something
	 * 			- Playing an animation
	 * 			- Changing which node they exist on
	 *		This system should update frequently, but not NEED to do so every frame. We should be able to
	 * 		put this on a tick-based system, where actions are updated every "tick", or X frames.
	 *
	 *
	 * == World ==:
	 * 		Handles updating the actual position of the agent (moving using an actual velocity), and insuring the avatar is playing the right animation.
	 * 		This runs every frame, but doesn't need to be active if the agent isn't seen or is at a low LOD. This is split from Handler layer so
	 * 		things like avoidance can be calculated and applied based on the velocity the action requests.
	 *
	 *
	 * == Peep ==:
	 * 		Handles calculating peep stats. Things like hunger and stuff. Not very computationally intensive.
	 *
	 */

	public class PeepAgent
	{
		public enum AnimActions { None, Sitting 					}
		public enum State 		{ Idle, Acting, 					}
		public enum MoveMode 	{ None, TowardsPoint, FollowPath,	}
		public enum LookDirMode { None, AtPoint, InDirection, TowardsMovement,    }

		// Base
		//--------------------------------------------------------------------
		public ushort     ID;
		public bool       Active;
		public ParkAIPeep Peep;
		public State      state;
		public bool       DecisionFlag;

		// Location
		public Vector3            Position;
		public GraphLocation      Location;
		public RegionObject       RegionObject;
		public RuntimeParkAIGraph Graph;

		public GraphLocation? Destination;

		// Movement
		public MoveMode    MovementMode;
		public LookDirMode LookMode;
		public bool        MoveInterrupted;
		public float       Speed;
		public float       WalkSpeed;

		public Vector3  Goal;
		public Vector3  Velocity;
		public Vector3  TargetVelocity;

		public Vector3 LookPointOrDirection;

		// Pathing
		[NonSerialized]
		public ABPath 	Path;
		public int 		PathIndex;

		public bool RequestingPath;
		public bool PathingFailed;

		[NonSerialized]
		public ABPath requestedPath; //NOTE: DO NOT ATTEMPT TO HAVE ANY ASTAR PATH DRAW IN THE INSPECTOR. THE EDITOR WILL CRASH.

		static StartEndModifier _pathModifier = new StartEndModifier {
			exactStartPoint = StartEndModifier.Exactness.ClosestOnNode,
			exactEndPoint   = StartEndModifier.Exactness.ClosestOnNode
		};

		// Actions
		public ActionQueue  Actions;

		// Graph Pathing
		public  uint PrevNode;
		public  Node NextNodeOnGraphPath;
		private bool _nodeChanged;

		public  bool      FollowingGraphPath;
		private GraphPath _graphPath;

		//	Avatar
		public AnimActions  AvatarAnim = AnimActions.None;

		public EmoteRequest EmoteRequest;
		public bool         ShowingEmote;

		// AI
		//--------------------------------------------------------------------
		public Decision    LastDecision;

		// Graph Brains
		public bool        IsBrainControlled;
		public IAgentBrain Brain; // This is normally null.


		// MISC/TODO
		//--------------------------------------------------------------------

		// Scratch adjacency arrays
		private static (AdjacentNode[] arr, int num) _scratchAdjacent_POI     = (new AdjacentNode[10], 0);
		private static (AdjacentNode[] arr, int num) _scratchAdjacent_Resting = (new AdjacentNode[20], 0);
		private static (AdjacentNode[] arr, int num) _scratchAdjacent_Stalls  = (new AdjacentNode[30], 0);

		//TODO: Some form of hash table may be a lot better for this?

		//The nodes the agent has visited within the current graph. Cleared on entry into a new graph.
		private const int                   VISITED_ARRAY_SIZE = 100;
		private       (uint[] arr, int num) VisitedWithinGraph;

		// TODO: Maybe a struct for where the agent has visited/what they've done instead? Maybe more than just which nodes they've visited? Will this be needed?
		//public struct Memory { ... }

		public PeepAgent(ushort ID, ParkAIPeep peep, GraphLocation location, RuntimeParkAIGraph graph)
		{
			this.ID = ID;

			Peep = peep;

			Location = location;
			Graph    = graph;
			Velocity = Vector3.zero;

			MovementMode = MoveMode.None;

			Actions      = new ActionQueue();
			state        = State.Idle;
			DecisionFlag = false;

			if (Location.node.type == NodeType.Shape)
				Position = Location.node.Shape.GetRandomWorldPointInside();

			Active             = true;
			VisitedWithinGraph = (new uint[VISITED_ARRAY_SIZE], 0);

			PrevNode = 0;

			WalkSpeed      = 1.5f + UnityEngine.Random.value * 1.0f;
			RequestingPath = false;

			OnEnterGraph(location);
		}

		public void PreUpdate()
		{
			Profiler.BeginSample("Pre Update");
			if (!_nodeChanged && Location.node.id != PrevNode) {
				PrevNode    = Location.node.id;
				_nodeChanged = true;
			}

			if (EmoteRequest.active) {
				EmoteRequest.time -= Time.deltaTime;
				if (EmoteRequest.time <= 0)
					EmoteRequest.active = false;
			}

			Profiler.EndSample();
		}

		public void TryDecisionUpdate(Random rand)
		{
			if (!Active || Brain != null) return;

			Profiler.BeginSample("Decision Tick");
			// Only decide if:
			// 	We have reached the end of our actions
			//	We were interrupted by something (not sure what that may be yet)
			if (state == State.Idle || DecisionFlag) {
				DecisionFlag = false;
				DoDecision(rand);
			}
			Profiler.EndSample();
		}

		public enum DecisionType {
			MeanderInsideCurrent,
			WalkToAdjacent,
			InteractWithPOI,
			GoThroughPortal,
			RestAtNode,
			InteractWithStall,
			//TODO: PathToNode,
		}

		public struct Decision {
			public DecisionType type;
			public AdjacentNode adjacent;
		}

		void DoDecision(Random rand)
		{
			Profiler.BeginSample("Make Decision");


			AdjacentNode? NextNodeAdjacent = null;

			Destination = null;

			_scratchAdjacent_POI.num     = 0;
			_scratchAdjacent_Resting.num = 0;

			bool entered_node = _nodeChanged;
			_nodeChanged = false;

			Node current_node = Location.node;

			if (FollowingGraphPath && current_node != null && NextNodeOnGraphPath != null) {
				for (int i = 0; i < current_node.adjacent.Count; i++) {
					var adj = current_node.adjacent[i];
					if (adj.ID == NextNodeOnGraphPath.id) {
						if(adj.node.enabled) {
							NextNodeAdjacent = current_node.adjacent[i];
							break;
						} else {
							_graphPath.reverse = true;
							UpdateNextGraphPathNode();
							break;
						}
					}
				}
			}

			if(entered_node && current_node != null) {
				FindAdjacent(current_node.adjacent, ref _scratchAdjacent_POI,     IsValidPointOfInterest);
				FindAdjacent(current_node.adjacent, ref _scratchAdjacent_Resting, IsValidRestingPoint);
				FindAdjacent(current_node.adjacent, ref _scratchAdjacent_Stalls,  IsValidStall);
			}

			// Utility functions for easier action list building...
			//------------------------------------------------------------------

			void SetDestination(AdjacentNode adj) => Destination = new GraphLocation(adj.node.graph, adj.node);
			void SetDestinationLoc(GraphLocation loc) => Destination = loc;

			bool getRandomInsideNode(Node node, out Vector3 point)
			{
				point = Vector3.zero;
				if (node == null) return false;

				if (node.HasWalkablePoints) {
					point = node.WalkablePoints.RandomElement(rand);
					point = Matrix4x4.TRS(node.Shape.Transform.Position, node.Shape.Transform.Rotation, Vector3.one).MultiplyPoint3x4(point);
					return true;
				}

				if(node.Shape != null) {
					point = node.Shape.GetRandomWorldPointInside();
					return true;
				}

				return false;
			}

			void walkToRandomInsideNode(Node node, Vector3 defaultPoint)
			{
				if (node != null && node.TryGetRandomPointInside(out Vector3 rand_point, rand))
					Actions.WalkTo(rand_point);
				else Actions.WalkTo(defaultPoint);
			}

			if (Peep.Stats.Urgency_Highest != PeepStat.None) {
				Emote emote = Emote.None;

				switch (Peep.Stats.Urgency_Highest) {
					case PeepStat.Hunger:    emote = Emote.Food; break;
					case PeepStat.Thirst:	 emote = Emote.Drink; break;
					case PeepStat.Boredom:	 emote = Emote.Aloof; break;
					case PeepStat.Tiredness: emote = Emote.Tired; break;
					case PeepStat.Bathroom:	 emote = Emote.Angry; break;
				}

				if (emote != Emote.None && rand.NextDouble() > 0.7f) {
					TryRequestEmote(emote, 5f);
				}
			}

			// Debug
			//TryRequestEmote(Emote.Drink, 5f);


			// Decide what we are GOING to do based on our current state...
			//------------------------------------------------------------------

			Decision decision = new Decision {
				type = DecisionType.MeanderInsideCurrent,
			};

			switch (Peep.Behaviour) {
				case PeepBehaviour.Marathon:

					Marathon_DoDecision();

					void Marathon_DoDecision()
					{
						// TODO: Actually select stall based on concrete logic
						if (_scratchAdjacent_Stalls.num > 0 && Peep.Stats.Hunger.Urgency > 0.1f) {
							AdjacentNode stall = _scratchAdjacent_Stalls.RandomElement(rand);

							if(stall.node.AnyUsageSlots() || stall.node.AnyQueueSlots()) {
								decision = new Decision {
									type     = DecisionType.InteractWithStall,
									adjacent = stall,
								};
								return;
							}
						}

						if (_scratchAdjacent_Resting.num > 0 && Peep.Stats.Tiredness.Urgency > 0.1f) {
							decision = new Decision {
								type     = DecisionType.RestAtNode,
								adjacent = _scratchAdjacent_Resting.RandomElement(rand),
							};
							return;
						}

						if(NextNodeAdjacent.HasValue && NextNodeAdjacent.Value.node != null) {
							if (NextNodeAdjacent.Value.node.type == NodeType.Portal) {
								decision = new Decision {
									type     = DecisionType.GoThroughPortal,
									adjacent = NextNodeAdjacent.Value
								};
								return;
							}

							decision = new Decision {
								type     = DecisionType.WalkToAdjacent,
								adjacent = NextNodeAdjacent.Value
							};
						}
					}


					break;
			}

			// Then build actions USING that decision
			//------------------------------------------------------------------
			ClearActions();

			switch (decision.type) {
				case DecisionType.MeanderInsideCurrent: {
					if (current_node != null)
						walkToRandomInsideNode(current_node, Position);

					Actions.Stand(2f + (float) rand.NextDouble() * 6f);
				} break;

				case DecisionType.WalkToAdjacent: {
					SetDestination(decision.adjacent);

					Actions.TravelToAdjacent(decision.adjacent, rand);
					walkToRandomInsideNode(decision.adjacent.node, decision.adjacent.link_2_worldpos.Value);

				} break;

				case DecisionType.GoThroughPortal: {
					AdjacentNode adjacent = decision.adjacent;
					SetDestinationLoc(adjacent.node.portal_destination);

					Actions.TravelToAdjacent(adjacent, rand);
					Actions.TeleportTo(adjacent.node.portal_destination);

					//Choose a new graph path

					// TODO: Choosing a new path every time we go through a portal
					/*var path = Location.graph.PortalPathReg[NextNodeAdjacent.node.portal_destination.node.id].RandomElement(rand);
					GraphPath = ( path, 1 );*/

					// TEMP
					var dest_spatial = adjacent.node.portal_destination.node.spatial;
					if (dest_spatial != null) {
						Node base_node = adjacent.node.portal_destination.node;
						if(adjacent.node.portal_destination.node != null) {
							AdjacentNode next = base_node.adjacent.RandomElement(rand);
							Actions.WalkTo(next.link_2_worldpos.Value);
							Actions.ChangeNode(next.node);

							walkToRandomInsideNode(next.node, next.link_2_worldpos.Value);

							/*if (next.obj is RegionShape2D dest_shape)
								AWalkTo(dest_shape.GetRandomWorldPointInside());*/
						}
					}
				}break;

				case DecisionType.InteractWithPOI: {


					AdjacentNode POI = decision.adjacent;
					if (OccupyRandomOpenSlot(POI.node, rand, out int index)) {

						SetDestination(POI);
						Actions.TravelToAdjacent(POI, rand);

						Vector3 place = POI.node.poi_spaces[index];
						Actions.WalkTo(place);

						if (POI.node.poi_world_focus_point.Item1)
							Actions.FaceTowards(POI.node.poi_world_focus_point.Item2);

						Actions.Stand(4f + rand.NextFloat() * 4f);

						Actions.DeoccupySlot(POI.node, index);
						Actions.TravelBackThroughAdjacent(current_node, POI, rand);

						/*ATravelToAdjacent(NextNodeAdjacent.Value, rand);
						walkToRandomInsideNode(NextNodeAdjacent.Value.node, NextNodeAdjacent.Value.link_2_worldpos.Value);*/
					}
				} break;

				case DecisionType.RestAtNode: {
					AdjacentNode rest = decision.adjacent;
					if (OccupyRandomOpenSlot(rest.node, rand, out int index)) {

						SetDestination(rest);
						var restTime = Mathf.Lerp(7, 18, Peep.Stats.Tiredness.Urgency);
						Peep.Stats.Tiredness.value = 0;
						var rest_point = rest.node.rest_spaces[index];

						Vector3 rest_world_pos  = Vector3.zero;
						Vector3 mount_world_pos = rest.link_1_worldpos.Value;

						Vector3 face_dir = rest_point.GetFacingVector();

						if (rest.scene_obj != null) {
							Matrix4x4 mat = rest.scene_obj.transform.localToWorldMatrix;
							rest_world_pos  = mat.MultiplyPoint3x4(rest_point.offset);
							mount_world_pos = mat.MultiplyPoint3x4(rest_point.mount_offset + rest_point.offset);
							face_dir        = mat.rotation * face_dir;

						} else if(rest.obj is RegionObjectSpatial spatial) {
							Matrix4x4 mat = spatial.Transform.matrix;
							rest_world_pos  = mat.MultiplyPoint3x4(rest_point.offset);
							mount_world_pos = mat.MultiplyPoint3x4(rest_point.mount_offset + rest_point.offset);
							face_dir        = mat.rotation * face_dir;
						}

						if (rest_point.has_mount)
							Actions.WalkTo(mount_world_pos);
						else
							Actions.WalkTo(rest_world_pos);

						Actions.SetWorldPos(rest_world_pos, true);

						Actions.FaceInDirection(face_dir);
						Actions.DoAnimation(AnimActions.Sitting);

						Actions.ChangeNode(rest.node);

						Actions.DoEmote(Emote.Rest, 1f, restTime);
						Actions.Stand(restTime);

						Actions.DoAnimation(AnimActions.None);

						Actions.ChangeNode(current_node);
						Actions.DeoccupySlot(rest.node, index);

						Actions.SetWorldPos(mount_world_pos);

						/*var point = (NextNodeAdjacent.node.Shape != null) ? NextNodeAdjacent.node.Shape.GetRandomWorldPointInside() : NextNodeAdjacent.link_2_worldpos.Value;

						ATravelToAdjacent(NextNodeAdjacent, rand);
						AWalkTo(point);*/
					}
				} break;

				case DecisionType.InteractWithStall: {

					// If there are any empty usage slots and the queue is empty, occupy one of them
					AdjacentNode stall = decision.adjacent;

					bool queue_open = stall.node.GetOpenQueueSlot(out Slot queueSlot);
					bool usage_open = stall.node.GetOpenUsageSlot(out Slot usageSlot);

					// If everything's full, don't bother
					if (!queue_open && !usage_open) {
						break;
					}

					if(usage_open && stall.node.NobodyInQueue()) {
						usageSlot.occupied = true;
						Actions.WalkTo(usageSlot.world_pos);
						Actions.ChangeNode(stall.node);
						Actions.Use(stall.node, usageSlot);
					} else if (queue_open) {
						queueSlot.occupied = true;

						//AWalkTo(queueSlot.world_pos);
						Actions.ChangeNode(stall.node);
						Actions.Queue(stall.node, queueSlot);
						// Queue inserts a use action afterwards
					} else {
						break;
					}

					Peep.Stats.Hunger.value = 0;

					Actions.TravelBackThroughAdjacent(current_node, stall, rand);
					// If usage slots are full, but

				} break;
			}

			TryStartActing();

			LastDecision = decision;

			Profiler.EndSample();
		}

		public void ActionTick(float dt, SimLevel sim, PeepLOD lod)
		{
			if (!Active || state != State.Acting || Brain != null) return;

			Profiler.BeginSample("Handler Tick");
			Actions.UpdateForAgent(this, dt, sim, lod);
			if (Brain == null && Actions.state == ActionQueue.State.Idle) {
				state = State.Idle;
			}

			Profiler.EndSample();
		}

		public void PreMovementUpdate(float dt)
		{
			Profiler.BeginSample("PreMovementUpdate");
			if(RequestingPath) {
				if (requestedPath == null || requestedPath.error) {
					PathingFailed  = true;
					RequestingPath = false;
					StopAnyPathing();
					DebugLogger.LogWarning($"Agent {ID} had a failed pathing request.", Peep.Avatar, LogContext.Pathfinding, LogPriority.Low);
				} else if ( requestedPath.IsDone()) {

					if(requestedPath.vectorPath.Count > 1) {
						RequestingPath = false;
						//MotionPlanning.AStar_ApplySmoothing(requestedPath, SimpleSmoothModifier.SmoothType.CurvedNonuniform);
						MotionPlanning.Astar_ApplyFunnel(requestedPath, true, false, FunnelModifier.FunnelQuality.High);
						_pathModifier.Apply(requestedPath);

						MovementMode  = MoveMode.FollowPath;
						Path          = requestedPath;
						PathIndex     = 1;
						requestedPath = null;
					} else {
						PathingFailed  = true;
						RequestingPath = false;
						StopAnyPathing();
					}
				}
			}

			// Decide velocity for this movement step
			switch (MovementMode) {
				case MoveMode.None:
					Velocity = Vector3.zero;
					break;

				case MoveMode.TowardsPoint:
					Velocity = (Goal - Position).normalized * Speed;

					break;

				case MoveMode.FollowPath:

					if (Path == null) {
						StopAnyPathing();
						MovementMode = MoveMode.TowardsPoint;
						break;
					}

					Vector3 target = Path.vectorPath[PathIndex];

					Goal     = target;
					Velocity = (target - Position).normalized * Speed;

					break;
			}
			Profiler.EndSample();
		}

		public void PostMovementUpdate(float dt, SimLevel sim, PeepLOD lod, Random rand)
		{
			if (!Active) return;

			Profiler.BeginSample("PostMovementUpdate");

			Vector3 avoidanceForce = Vector3.zero;


			Profiler.BeginSample("Avoidance Force");
			if (ParkAIController.Config.UseAvoidance && Peep.UsingAvoidance && ShouldUseAvoidance()) {
				if (ParkAIController.Live.AvoidanceSystem.GetAgent(Peep.AvoidanceID, out var agent)) {
					avoidanceForce = Vector3.ClampMagnitude(agent.Force.x_z(), 60f) * dt;
				}
			}
			Profiler.EndSample();

			if (MovementMode != MoveMode.None && Velocity.magnitude > Mathf.Epsilon) {
				var distToGoal = Vector3.Distance(Position, Goal);

				if(distToGoal < 5)
					avoidanceForce *= Mathf.Clamp01( distToGoal / 2 );

				/*Draw.ingame.Line(Position, Position + avoidanceForce, 	Color.yellow);
				Draw.ingame.Line(Position, Position + Velocity, 		Color.red);*/
			}


			Velocity += avoidanceForce;

			Profiler.BeginSample("Movement/Pathing");
			if (MoveInterrupted || Vector3.Distance(Position, Goal) < 0.0001f || MotionPlanning.WillPassTarget(Position, Goal, Velocity, dt)) {
				if (!MoveInterrupted) {
					if(MovementMode == MoveMode.FollowPath) {
						if (PathIndex >= Path.vectorPath.Count - 1) {
							StopAnyPathing();
							MovementMode = MoveMode.None;
						} else {
							PathIndex++;
						}
					} else if (MovementMode == MoveMode.TowardsPoint) {
						MovementMode = MoveMode.None;
					}
				} else {
					MoveInterrupted = false;
					MovementMode    = MoveMode.None;
				}
			}
			Profiler.EndSample();

			Position += Vector3.ClampMagnitude(Velocity * dt, 10);

			// Animations
			if(ParkAIController.Config.AvatarAnimation) {

				Profiler.BeginSample("Animation");
				ParkAIAnim animation = ParkAIAnim.Stand;

				if (AvatarAnim != AnimActions.None) {
					switch (AvatarAnim) {
						case AnimActions.Sitting:
							animation = ParkAIAnim.Sit;
							break;
					}
				} else {
					/*if (Actions.current.type == ActionType.WalkTo) {
						animation = ParkAIAnim.Walk;
					} else*/ if (MovementMode != MoveMode.None && Velocity.magnitude >= Mathf.Epsilon) {
						animation = ParkAIAnim.Walk;
					}
				}

				Vector3 dir = Peep.LookDirection;

				switch (LookMode) {

					case LookDirMode.AtPoint:
						dir = ( LookPointOrDirection - Position ).normalized;
						break;

					case LookDirMode.InDirection:
						dir = LookPointOrDirection.normalized;
						break;

					case LookDirMode.TowardsMovement:
						dir = ( Goal - Position ).normalized;
						break;

					case LookDirMode.None: break;
				}

				if (dir.magnitude > Mathf.Epsilon /*&& !dir.AnyNAN()*/)
					Peep.LookDirection = dir;

				if(animation != Peep.Animation) {
					Peep.FrameIndex = 0;
					Peep.FrameTimer = 0;
					Peep.Animation  = animation;
				}

				/*if (Peep.AvatarOld) {
					Peep.AvatarOld.LookDirection = AvatarLookDirection;
					Peep.AvatarOld.PlayOnAnimator(anim);
				}*/
				Profiler.EndSample();
			}

			Profiler.EndSample();
		}

		public bool ShouldUseAvoidance()
		{
			if (state == State.Acting) {
				if (Actions.current.type == ActionType.Queue)
					return false;
			}

			return MovementMode != MoveMode.None;
		}

		public void PeepTick(float dt)
		{
			if (!Active) return;

			Profiler.BeginSample("AI Tick");
			Peep.Stats.Update(dt);
			Profiler.EndSample();
		}

		public void ClearActions() => Actions.actions.Clear();

		public void TryStartActing()
		{
			if(Actions.count > 0) {
				state         = State.Acting;
				Actions.state = ActionQueue.State.Start;
			}
		}

		void MoveTowardsPoint(Vector3 goal, float? speed = null)
		{
			if(MovementMode == MoveMode.FollowPath)
				StopAnyPathing();

			MovementMode = MoveMode.TowardsPoint;
			Goal         = goal;
			Speed        = speed.GetValueOrDefault(GetWalkSpeed());
		}

		void StartPathRequest(Vector3 start, Vector3 goal)
		{
			if (RequestingPath) return;

			if (requestedPath != null) {
				requestedPath.Release(this);
				requestedPath = null;
			}

			RequestingPath = true;
			requestedPath  = ABPath.Construct(start, goal);
			requestedPath.nnConstraint = new NNConstraint {
				graphMask = CalcSettings.LayerToMask(GraphLayer.ParkAI)
			};

			requestedPath.Claim(this);
			AstarPath.StartPath(requestedPath);
		}

		void StopAnyPathing()
		{
			if (requestedPath != null) {
				requestedPath.Release(this);
				requestedPath = null;
			}

			if (Path != null) {
				Path.Release(this);
				Path = null;
			}

			RequestingPath = false;
			PathingFailed  = false;

			if(MovementMode == MoveMode.FollowPath)
				MovementMode = MoveMode.None;
		}


		public Vector3 GetTargetWorldPos()
		{
			/*var (info, ok) = MotionPlanning.GetPosOnNavmesh(WorldPos, 1f);
			if(ok) {
				return info.position;
			}*/

			return Position;
		}

		public void CancelLook()                       { LookMode = LookDirMode.None;			 LookPointOrDirection = Vector3.forward; }
		public void LookAtPoint(Vector3     point)     { LookMode = LookDirMode.AtPoint;		 LookPointOrDirection = point;           }
		public void LookInDirection(Vector3 direction) { LookMode = LookDirMode.InDirection;	 LookPointOrDirection = direction;       }
		public void LookTowardsMovement()              { LookMode = LookDirMode.TowardsMovement; LookPointOrDirection = Vector3.forward; }

		public void ChangeNode(Node node)
		{
			Location.node = node;
			OnLocationChanged();
		}

		public void ChangeRegionObject(RegionObjectSpatial obj)
		{
			Location.region_obj = obj;
			OnLocationChanged();
		}

		public void ChangeLocation(Node node, RegionObjectSpatial obj)
		{
			Location.node       = node;
			Location.region_obj = obj;
			OnLocationChanged();
		}

		public void ChangeLocation(GraphLocation newLocation)
		{
			if (!newLocation.IsValid) return;

			if(newLocation.graph != Location.graph || newLocation.node.type == NodeType.Portal)
				OnEnterGraph(newLocation);

			Location = newLocation;
			OnLocationChanged();
		}

		void OnLocationChanged()
		{
			if (!Location.IsValid) return;

			if (Location.node != null && !Location.node.enabled) {
				TeleportToRandomValidNode(ParkAIController.Live._rand);
				return;
			}

			// Update the graph path
			if(FollowingGraphPath && _graphPath.index_valid) {

				// If we don't yet know what the next node on the graph path is...
				if (NextNodeOnGraphPath == null) {
					NextNodeOnGraphPath = _graphPath[(_graphPath.index + _graphPath.direction).Wrap(_graphPath.length)];
				}

				// If we just entered a node and it's the next node on our path, advance the path index.
				if (NextNodeOnGraphPath != null && Location.node.id == NextNodeOnGraphPath.id) {
					AdvanceGraphPath();
				}
			} else {
				TryFindNewGraphPath();
			}
		}

		void OnEnterInvalidLocation(GraphLocation loc)
		{
			if (!Location.IsValid) {
				// todo
			} else if(Location.node != null && !Location.node.enabled) {

			}

		}

		private static List<Node> _scratchNodes = new List<Node>();

		public void TeleportToRandomValidNode(Random rand)
		{
			if (Graph == null) return;

			_scratchNodes.Clear();

			Node destination = null;

			bool validLocation(Node node) => node.enabled && node.type == NodeType.Shape;

			if (FollowingGraphPath) {

				for (int i = 0; i < _graphPath.array.Length; i++) {
					var n = _graphPath.array[i];
					if(validLocation(n)) _scratchNodes.Add(n);
				}

			} else {

				for (int i = 0; i < Graph.Nodes.Count; i++) {
					var n = Graph.Nodes[i];
					if(validLocation(n)) _scratchNodes.Add(n);
				}

			}

			if(_scratchNodes.Count > 0) {
				destination = _scratchNodes.RandomElement(rand);
			}

			if (destination != null) {
				ChangeLocation(new GraphLocation(destination.graph, destination));

				if (destination.spatial != null) {
					Position = destination.spatial.Transform.Position;
					if (Peep.Avatar)
						Peep.Avatar.transform.position = Position;
				}

				if (Location.node != null) {
					if (Location.node.TryGetRandomPointInside(out Vector3 point, rand))
						Position = point;
					/*if (Location.node.HasWalkablePoints) {
						Position = Location.node.WalkablePoints.RandomElement(rand);
					} else {
						Position = Location.node.Shape.GetRandomWorldPointInside();
					}*/
				}

				if (FollowingGraphPath) {
					TryFindNewGraphPath(false);
				}

				StopAnyActions(true);
				StopAnyPathing();

				DecisionFlag = true;
			}
		}

		public void SetGraphPath(GraphPath path)
		{
			if (!path.valid) return;

			_graphPath = path;

			FollowingGraphPath = true;
			if (_graphPath.index >= _graphPath.length - 1)
				_graphPath.reverse = true;

			NextNodeOnGraphPath = _graphPath[(_graphPath.index + _graphPath.direction).Wrap(_graphPath.length)];

			if (NextNodeOnGraphPath != null && Location.node.id == NextNodeOnGraphPath.id) {
				AdvanceGraphPath();
			}
		}

		// Happens either upon entering a new graph, or teleporting within a graph
		public void OnEnterGraph(GraphLocation location)
		{
			//TODO: Only nice for testing, probably don't need to do this.
			for (int i = 0; i < VISITED_ARRAY_SIZE; i++) {
				VisitedWithinGraph.arr[i] = 0;
			}

			VisitedWithinGraph.num    = 1;
			VisitedWithinGraph.arr[0] = location.node.id;

			Location = location;

			OnLocationChanged();
		}

		void AdvanceGraphPath()
		{
			_graphPath.index += _graphPath.direction;
			if (_graphPath.reverse && _graphPath.index <= 0 || _graphPath.index >= _graphPath.length - 1) {
				if(_graphPath.IsLooping()) {
					if (_graphPath.reverse)
						_graphPath.index = _graphPath.length - 1;
					else
						_graphPath.index = 0;
				} else {
					OnReachedEndOfGraphPath();
				}
			}

			UpdateNextGraphPathNode();
		}

		void UpdateNextGraphPathNode()
		{
			NextNodeOnGraphPath = _graphPath[(_graphPath.index + _graphPath.direction).Wrap(_graphPath.length)];
		}

		void OnReachedEndOfGraphPath() => FollowingGraphPath = false;

		void TryFindNewGraphPath(bool stopCurrentIfNotFound = true)
		{
			if (Location.node.paths.Count > 0) {
				(Node[] array, int index) path = Location.node.paths.RandomElement(ParkAIController.Live._rand);
				if (path.array.Length > 0) {
					SetGraphPath(new GraphPath {
						array = path.array,
						index = path.index
					});

					return;
				}
			}

			if (stopCurrentIfNotFound && FollowingGraphPath) FollowingGraphPath = false;
		}

		public enum ActionType
		{
			Stand,
			WalkTo,
			FaceTowardsPos,
			DoAnimation,
			ChangeNode,
			ChangeRegionObj,
			TeleportTo,
			SetWorldPos,
			SetOccupy,
			SetSlotOccupy,
			Emote,
			Queue,
			Usage
		}

		public struct Action
		{
			public ActionType type;

			//General
			public bool          toggle;
			public float         timer;
			public float         timer_duration;
			public AnimActions   anim;
			public GraphLocation location;
			public Node          node;
			public Slot          slot;
			public Vector3       position;
			public int           index;

			public bool doing_path;
			public bool pathing;
			public int  path_index;

			public Move move;
			public Move move_alt;

			// Emote
			public Emote emote;
			public float chance; // 0 - 1
			public float emote_request_time;
			public bool  emote_override_current_request;

			// Queueing
			public bool in_queue;
		}

		public struct Move
		{
			public Vector3 start;
			public Vector3 destination;
			public bool    override_speed;
			public float   speed;
			public float   distance;

			public float GetSpeed(PeepAgent agent) => override_speed ? speed : agent.GetWalkSpeed();
		}

		public bool UpdateAction(ref Action action, ActionQueue queue, PeepAgent agent, float dt, SimLevel sim, PeepLOD lod, bool first)
		{
			switch(action.type) {
				case ActionType.Stand: {
					return CountDown(ref action, dt);
				} break;

				case ActionType.DoAnimation: {
					AvatarAnim = action.anim;
				} break;

				case ActionType.Emote: {
					if (action.chance >= 1 - Mathf.Epsilon || action.chance >= ParkAIController.Live._rand.NextDouble()) {
						if (action.emote_override_current_request || CanShowEmote()) {
							EmoteRequest = new EmoteRequest { emote = action.emote, time = action.emote_request_time, active = true};
						}
					}
				} break;

				case ActionType.ChangeNode: {
					/*Location.node 		= action.location.node;
					Location.region_obj = action.location.region_obj;*/
					ChangeLocation(action.location.node, action.location.region_obj);
					//OnLocationChanged();
					//Go down the list of visited nodes. If the node isn't in the list, add it.
					bool visited = false;
					for (int i = 0; i < VisitedWithinGraph.num; i++) {
						if(action.location.node.id == VisitedWithinGraph.arr[i]) {
							visited = true;
							break;
						}
					}

					if (!visited)
						VisitedWithinGraph.arr[( VisitedWithinGraph.num++ ) % VISITED_ARRAY_SIZE] = action.location.node.id;
				} break;

				case ActionType.ChangeRegionObj: {
					ChangeRegionObject(action.location.region_obj);
					//Location.region_obj = action.location.region_obj;
				} break;

				case ActionType.TeleportTo: {
					/*if(action.location.graph != Location.graph) { }*/

					ChangeLocation(action.location);
					//OnEnterGraph(action.location);
					var node = action.location.node;

					//Location    = action.location;
					if (node.spatial != null) {
						Position = node.spatial.Transform.Position;
						if (Peep.Avatar)
							Peep.Avatar.transform.position = Position;
					}

					// TODO: For some reason, agents do not correctly maintain their graph path when going through a portal.
					if (FollowingGraphPath && node.type == NodeType.Portal /*&& ParkAIController.Live._rand.NextDouble() > 0.5f*/) {
						TryFindNewGraphPath(false);
					}

				} break;

				case ActionType.WalkTo: {
					ref Move move = ref action.move;

					if (first) {
						MoveTowardsPoint(move.destination, move.GetSpeed(agent));
						TryPathing(ref action);
						PathingFailed = false;
					} else {
						if (MovementMode == MoveMode.None)
							return false;

						if (PathingFailed) {
							TryPathing(ref action);

							PathingFailed = false;
						}
					}

					LookTowardsMovement();

					return true;



					void TryPathing(ref Action _action)
					{
						//TODO: Detect LOD change and start pathing if so.
						if(ActorController.playerActor != null && /*lod == PeepLOD.Near &&*/ ParkAIController.Config.UsePathfinding && Location.region_obj != null && Location.region_obj.RequiresPathfinding) {
							StartPathRequest(Position, Goal);
							_action.doing_path = true;
						}
					}
				} break;

				case ActionType.FaceTowardsPos: {
					if (action.toggle)
						LookInDirection(action.position.normalized);
					else
						LookAtPoint(action.position.normalized);
				} break;

				case ActionType.SetWorldPos: {
					Position = action.position;
					if (action.toggle && Peep.Avatar)
						Peep.Avatar.transform.position = Position;

				} break;

				case ActionType.SetOccupy: {
					action.node.OccupancySlots[action.index] = action.toggle;
				} break;

				case ActionType.SetSlotOccupy: {
					action.slot.occupied = action.toggle;
				} break;

				case ActionType.Queue: {

					Slot slot = action.slot;

					bool slot_changed = false;

					bool CanAdvanceInQueue(ref Action _action, bool waitForMovement)
					{
						if (_action.slot?.next == null) return false;
						if (_action.slot.next.occupied || waitForMovement && MovementMode != MoveMode.None)
							return false;

						return true;
					}


					if(slot == null)
						return false;

					if (!action.in_queue) {

						if(first) {
							// Walk to the start of the queue
							//TODO: Factor movement out so we don't need to do this
							MoveTowardsPoint(slot.world_pos);
							LookAtPoint(slot.world_pos);
						}

						// Constantly try to advance our position in the queue so others can try to fill it as well.
						if (CanAdvanceInQueue(ref action, false)) {
							slot.occupied      = false;
							slot.next.occupied = true;
							action.slot        = slot.next;
							MoveTowardsPoint(slot.next.world_pos);
						}

						if (MovementMode == MoveMode.None) {
							action.in_queue = true;
						} else {
							LookAtPoint(slot.world_pos);
						}

					} else {

						LookInDirection(action.slot.direction);

						if (slot.next != null) {
							if(CanAdvanceInQueue(ref action, true)) {
								slot.occupied      = false;
								slot.next.occupied = true;
								MoveTowardsPoint(slot.next.world_pos, GetWalkSpeed() * 0.4f);
								action.slot = slot.next;
							}
						} else {
							if (action.node.GetOpenUsageSlot(out Slot usageSlot)) {
								slot.occupied      = false;
								usageSlot.occupied = true;
								//MoveTowardsPoint(slot.world_pos);

								//Debug.Log($"{ID}: End queue, occupy usage slot {usageSlot.index}");

								queue.actions.Push(new Action {
									type = ActionType.Usage,
									slot = usageSlot,
									node = action.node,
								});

								// walk to the slot
								queue.actions.Push(new Action {
									type = ActionType.WalkTo,
									move = new Move{ destination = usageSlot.world_pos, speed = GetWalkSpeed() }
								});

								return false;
							}
						}
					}

					/*if (first) {
						//action.toggle = false;
						Debug.Log($"{ID}: Start queue, slot {action.slot.index}");
					}*/

					return true;

				} break;

				case ActionType.Usage: {

					LookInDirection(action.slot.direction);

					if (first) {
						if (action.node.brain != null) {
							//Debug.Log($"{ID}: Usage, deferring to brain!");
							action.node.brain.Control(this);
							Brain = action.node.brain;

							// Deoccupy once the brain is done
							// TODO: Should be be done by the brain? Is there a better way?
							queue.actions.Push(new Action {
								type = ActionType.SetSlotOccupy,
								slot = action.slot,
								toggle = false,
							});

							return false;
						} else {
							//Debug.Log($"{ID}: Usage, {action.slot.index}");
							action.timer = 4f;
						}
					}

					if (!CountDown(ref action, dt)) {
						action.slot.occupied = false;
						return false;
					}

					return true;
				} break;
			}

			// Ends action by DEFAULT
			return false;
		}

		public void StopAnyActions(bool deoccupy)
		{
			if (deoccupy) {

				// We need to make sure we properly deoccupy any slots that lie within the action list,
				if(Actions.count > 0) {

					Action action = Actions.current;
					while (true) {

						if (action.type == ActionType.Queue && action.slot != null) {
							action.slot.occupied = false;
						}

						if (action.type == ActionType.SetOccupy) {
							action.node.OccupancySlots[action.index] = false;
						} else if (action.type == ActionType.SetSlotOccupy) {
							action.slot.occupied = false;
						}

						if (Actions.count > 0)
							break;

						action = Actions.actions.Dequeue();
					}
				}
			}


			Actions.state = ActionQueue.State.Idle;
			Actions.actions.Clear();

			state = State.Idle;

			AvatarAnim = AnimActions.None;
		}

		public bool CountDown(ref Action action, float dt)
		{
			action.timer -= dt;
			return action.timer > 0;
		}

		public class ActionQueue {
			public enum State { Idle, Start, Running }
			public List<Action> actions = new List<Action>();
			public Action       current;
			public int          count => actions.Count;

			public State state = State.Idle;

			public void UpdateForAgent(PeepAgent agent, float dt, SimLevel sim, PeepLOD lod)
			{
				if (agent == null) return;

				switch (state) {
					case State.Start:
						if (actions.Count <= 0)
							state = State.Idle;
						else {
							current = actions.Dequeue();
							agent.UpdateAction(ref current, this, agent, dt, sim, lod, true);
							state   = State.Running;
						}
						break;

					case State.Running:
						if (!agent.UpdateAction(ref current, this, agent, dt, sim, lod, false)) {

							if (actions.Count <= 0 || agent.Brain != null) {
								state     = State.Idle;
							} else {
								current = actions.Dequeue();
								while (!agent.UpdateAction(ref current, this, agent, dt, sim, lod, true)) {
									if (agent.Brain != null) {
										if (actions.Count <= 0)
											state = State.Idle;
										break;
									}

									if (actions.Count <= 0) {
										state     = State.Idle;
										break;
									}

									current = actions.Dequeue();
								}
							}
						}
						break;
				}
			}

			public void Start()
			{
				if (count <= 0 || state != State.Idle) return;
				state = State.Start;
			}

			public void Stop()
			{
				state = State.Idle;
				actions.Clear();
				current  = new Action();
			}

			//	Actions
			//-----------------------------------------------------------------------------------------

			public void Stand(float             time) => actions.Add(new Action {type = ActionType.Stand, timer      = time});
			public void DoAnimation(AnimActions anim) => actions.Add(new Action {type = ActionType.DoAnimation, anim = anim});

			public void DoEmote(Emote emote, float chance = 1f, float request_life = 10f, bool override_current_request = true)
				=> actions.Add(new Action { type = ActionType.Emote, emote = emote, chance = chance, emote_request_time = request_life, emote_override_current_request = override_current_request});

			public void ChangeNode(Node node)
			{
				if (node == null)
					DebugLogger.LogError("NULL NODE!", LogContext.Pathfinding, LogPriority.Low);

				actions.Add(new Action {type = ActionType.ChangeNode, location = new GraphLocation(null, node)});
			}

			public void TeleportTo(GraphLocation locationOld)                    => actions.Add(new Action {type = ActionType.TeleportTo, location     = locationOld});
			public void FaceTowards(Vector3      position)                       => actions.Add(new Action {type = ActionType.FaceTowardsPos, position = position});
			public void FaceInDirection(Vector3  position)                       => actions.Add(new Action {type = ActionType.FaceTowardsPos, position = position, toggle = true});
			public void SetWorldPos(Vector3      position, bool no_lerp = false) => actions.Add(new Action {type = ActionType.SetWorldPos, position    = position, toggle = no_lerp});

			public void ChangeRegionObject(RegionObjectSpatial obj)
				=> actions.Add(new Action {type = ActionType.ChangeRegionObj, location = new GraphLocation(null, null, obj)});

			public void WalkTo(Vector3 destination, float? speed = null)
			{
				actions.Add(new Action {type = ActionType.WalkTo, move = new Move {
					destination 	= destination,
					override_speed 	= speed.HasValue,
					speed 			= speed.GetValueOrDefault(0),
				}});
			}

			public void OccupySlot(Node   node, int index) => actions.Add(new Action {type = ActionType.SetOccupy, node = node, toggle = true, index  = index});
			public void DeoccupySlot(Node node, int index) => actions.Add(new Action {type = ActionType.SetOccupy, node = node, toggle = false, index = index});

			public void Queue(Node node, Slot slot) => actions.Add(new Action {
				type = ActionType.Queue,
				node = node,
				slot = slot,
			});

			public void Use(Node node, Slot slot) => actions.Add(new Action {
				type = ActionType.Usage,
				node = node,
				slot = slot,
			});

			//	Composite Actions
			//-----------------------------------------------------------------------------------------
			public void TravelToAdjacent(AdjacentNode adj, Random rand, bool change_node = true)
			{
				var variation = rand.NextFloat();

				Vector3 pos_1 = adj.link_1_worldpos.Value;
				Vector3 pos_2 = adj.link_2_worldpos.Value;

				if(adj.link_1_edge.HasValue)
					pos_1 = Vector3.Lerp(adj.link_1_edge.Value.L, adj.link_1_edge.Value.R, variation);

				if(adj.link_2_edge.HasValue)
					pos_2 = Vector3.Lerp(adj.link_2_edge.Value.L, adj.link_2_edge.Value.R, variation + rand.NextFloat() * 0.2f);

				WalkTo(pos_1);
				if(change_node) ChangeRegionObject(adj.link);

				WalkTo(pos_2);
				if(change_node) ChangeNode(adj.node);
			}

			public void TravelBackThroughAdjacent(Node node, AdjacentNode adj, System.Random rand, bool change_node = true)
			{
				var variation = rand.NextFloat();

				Vector3 pos_1 = adj.link_2_worldpos.Value;
				Vector3 pos_2 = adj.link_1_worldpos.Value;

				if(adj.link_2_edge.HasValue)
					pos_1 = Vector3.Lerp(adj.link_2_edge.Value.L, adj.link_2_edge.Value.R, variation);

				if(adj.link_1_edge.HasValue)
					pos_2 = Vector3.Lerp(adj.link_1_edge.Value.L, adj.link_1_edge.Value.R, variation + rand.NextFloat() * 0.2f);

				WalkTo(pos_1);
				if(change_node) ChangeRegionObject(adj.link);

				WalkTo(pos_2);
				if(change_node) ChangeNode(node);
			}
		}



		float GetWalkSpeed() => WalkSpeed * ParkAIController.Config.Peep_WalkSpeedMod;

		/*public Vector3 GetVelocity()
		{
			Vector3 velocity = Vector3.zero;

			if (CurrentAction.type == ActionType.WalkTo) {
				CurrentAction.move.
			}
		}*/

		static bool OccupyFirstOpenSlot(Node node, out int slot_index)
		{
			slot_index = -1;
			int len = node.OccupancySlots.Length;

			if (len == 0) return false;

			for (int i = 0; i < len; i++) {
				if (node.OccupancySlots[i]) continue;
				slot_index             = i;
				node.OccupancySlots[i] = true;
				return true;
			}

			return false;
		}

		const  int   TEMP_SLOT_INDICES  = 500;
		static int[] _temp_slot_indices = new int[TEMP_SLOT_INDICES];

		static bool OccupyRandomOpenSlot(Node node, Random rand, out int slot_index)
		{
			slot_index = -1;

			if (node.OccupancySlots == null)
				return false;

			int len = node.OccupancySlots.Length;

			if (len == 0) return false;

			int j = 0;
			for (int i = 0; i < len && j < TEMP_SLOT_INDICES; i++) {
				if (node.OccupancySlots[i]) continue;
				_temp_slot_indices[j++] = i;
			}

			if(j != 0) {
				var rand_index = rand.Next(0, j);
				slot_index                      = _temp_slot_indices[rand_index];
				node.OccupancySlots[slot_index] = true;
				return true;
			}

			return false;
		}


		// Finding Adjacent Nodes
		//-----------------------------------------------------------------------------------------
		private static Func<AdjacentNode, bool> IsValidPointOfInterest = adj => adj.node.type == NodeType.PointOfInterest;
		private static Func<AdjacentNode, bool> IsValidRestingPoint = adj => {
			if (adj.node.type != NodeType.Resting) return false;
			for (int i = 0; i < adj.node.OccupancySlots.Length; i++) {
				if (adj.node.OccupancySlots[i]) return false;
			}
			return true;
		};

		private static Func<AdjacentNode, bool> IsValidStall = adj => {
			if (adj.node.type != NodeType.Stall) return false;
			/*for (int i = 0; i < adj.node.OccupancySlots.Length; i++) {
				if (!adj.node.OccupancySlots[i]) return false;
			}*/
			return true;
		};

		void FindAdjacent(List<AdjacentNode> adjacent, ref (AdjacentNode[] arr, int num) array, Func<AdjacentNode, bool> decider)
		{
			int j = 0;
			for (int i = 0; i < adjacent.Count && i < array.arr.Length; i++) {
				var adj = adjacent[i];
				if (adj.node.enabled && decider(adj)) {
					array.arr[j++] = adj;
					break;
				}
			}

			array.num = j;
		}


		bool CanShowEmote() => !EmoteRequest.active && !ShowingEmote;
		void TryRequestEmote(Emote emote, float time)
		{
			if (CanShowEmote()) EmoteRequest = new EmoteRequest {emote = emote, time = time, active = true};
		}

	}

	public enum SimLevel
	{
		InMap, OffMap
	}

	// TODO: Emote priority?
	public struct EmoteRequest {
		public Emote emote;
		public float time;
		public bool  active;

		public override string ToString() => $"(Emote: {emote}, Time: {time}, Active: {active})";
	}


}

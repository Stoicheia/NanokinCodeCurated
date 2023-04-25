using System;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Util;
using DG.Tweening;
using DG.Tweening.Core;
using Drawing;
using MoonSharp.Interpreter;
using Pathfinding;
using UnityEngine;
using Util;
using Util.UniTween.Value;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace Anjin.MP
{
	public enum MPState
	{
		Idle,        //Doing nothing
		Calculating, //Waiting for a path to calculate
		PathError,
		Running,     //Following a path
		FollowerAction, //The follower is doing an action at a node and we will wait for that to be done.
	}

    public enum MPError
    {
        None,
        NotOnNavmesh,
        CouldNotFind,
    }

	public enum PathTarget
	{
		Position,	//Some type of position
		RegionPath, //A region path
	}

	/*public enum TargetIndexMode {
		Calculated,
		Raw,
	}*/

	[LuaEnum]
	public enum PathFollowMode {
		Calculated,
		Raw,
	}

	public enum PathDirection {
		Forwards,
		Backwards,
		Closest,
	}

	public struct PathingSettings
	{
		public float 	Speed;

		public PathTarget 	Target;
		public Vector3 		TargetPosition;
		public string 		TargetRegionPathID;

		public bool 		AsyncCalculate;
		public bool 		Repeat;
		public bool 		StartFromClosestPoint;

		public static PathingSettings Default = new PathingSettings
		{
			Speed                 = 1,
			Target 				  = PathTarget.Position,
			TargetPosition        = Vector3.zero,
			TargetRegionPathID    = "",
			AsyncCalculate        = false,
			Repeat                = false,
			StartFromClosestPoint = true,
		};

		public void SetTargetPos(Vector3 pos)
		{
			Target         = PathTarget.Position;
			TargetPosition = pos;
		}

		public void SetTargetRegionPath(string ID)
		{
			Target             = PathTarget.RegionPath;
			TargetRegionPathID = ID;
		}
	}

	//TODO: Split pathing into getting to the path and following the path.
	[MoonSharpUserData]
	public struct PathingState
	{
		public PathingSettings settings;

		public MPState state;
		public MPError error;
		public MPPath Path;
		public int current_segment;

		public Vector3 follower_dir;
		public float   follower_speed;

		public float distance_traveled;

		//public float speed;
		//public string RegionPathID;

		public (MPNode node, bool exists) prev_node;
		public Vector3 					  starting_pos;

		public static PathingState Default = new PathingState
		{
			state          	= MPState.Idle,
            error           = MPError.None,
			follower_dir   	= Vector3.zero,
			follower_speed 	= 1,

			current_segment   = 0,
			distance_traveled = 0,

			Path           	= null,
			prev_node       = (new MPNode(), false),
			starting_pos 	= new Vector3(),
			settings 		= PathingSettings.Default,
			/**/
		};

		public void ResetToIdle() => state = MPState.Idle;

		public void Start(PathingSettings _settings)
		{
			//if (state != MPState.Idle) return;
			state = MPState.Calculating;
			settings = _settings;
		}
	}

	[MoonSharpUserData]
	public struct PathUpdateResult
	{
		public bool 	reached_vert;
		public int 		vert_index;

		public bool 	reached_node;
		public int 		node_index;

		public MPNode 	node;
		public MPNode?  next_node;

		public bool 	reached_path_end;

		public bool 	calculation_failed;

		public MPAction action;

		/*public override string ToString()
		{
			return $"{did_reach_node}, {reached_node_index}, {reached_node}, {reached_path_end},";
		}*/
	}

	public struct PathFollowState {
		//public MPPath path;
		public int  index;

		// (current raw node, if the segment index we're on also has a raw node on it)
		public (int index, bool on_raw_segment) index_raw;

		public int  start_index;
		public int  target_index;
		public bool target_end;

		public PathDirection direction;

		public bool loop;
		public int  loop_count;

		public bool on_path;
		public bool region_path;
		public bool just_started;
		public bool translated_to_calc;
		//public bool raw_input;

		//public TargetIndexMode target_mode;
		public PathFollowMode  follow_mode;

		public Tween tween;

		public Vector3? last_starting_pos;

		//public int    PointsLeft            	 => Mathf.Abs(target_index - index);

		public MPNode GetTargetNode(MPPath path) {
			if(path.HasRawIndicies && follow_mode == PathFollowMode.Raw)
				return path.Nodes.WrapGet(target_end ? path.RawToCalculatedIndicies.Last() : path.RawToCalculatedIndicies.WrapGet(target_index));
			else
				return path.Nodes.WrapGet(target_end ? path.Nodes.Count - 1 : target_index);
		}

		public void UpdateRawIndex(MPPath path)
		{
			// Figure out what our raw index is.
			if (path.HasRawIndicies) {

				int ind = index % (path.Nodes.Count - 1);

				index_raw.index = 0;
				for (int i = 0; i < path.Nodes.Count; i++) {
					MPNode node = path.Nodes[i];
					if (i != 0 && node.is_raw_node)
						index_raw.index++;

					index_raw.on_raw_segment = node.is_raw_node;

					if (i == ind) break;
				}
			} else {
				index_raw = (0, false);
			}
		}

		public void IncrementTargetIndex(int num, MPPath path)
		{
			if (region_path && follow_mode != PathFollowMode.Raw) {
				if (just_started && !translated_to_calc) {
					index              = path.RawToCalculatedIndicies[index];
					target_index       = path.RawToCalculatedIndicies[target_index];
					translated_to_calc = true;
				}

				int raw_index = path.CalculatedToRawIndicies[target_index];
				raw_index    += num;
				target_index =  path.RawToCalculatedIndicies.ClampGet(raw_index);
			} else {
				target_index += num;
			}
		}

		public PathFollowState(int index, int targetIndex) : this()
		{
			this.index        = index;
			target_index      = targetIndex;
			just_started      = true;
			last_starting_pos = null;
			on_path           = false;
		}

		public static PathFollowState Default = new PathFollowState {
			//path 			= null,
			index             = 0,
			index_raw         = (0, false),
			target_index      = 0,
			target_end        = false,
			direction         = PathDirection.Forwards,
			loop              = false,
			loop_count        = -1,
			on_path           = false,
			region_path       = false,
			just_started      = true,
			last_starting_pos = null,
			//raw_input = false,
			//target_mode  = TargetIndexMode.Calculated,
			follow_mode  = PathFollowMode.Calculated,
		};
	}

	public struct PathFollowOutput {
		public Vector3 direction;
		public float?  speed; 		// NOTE(CL): We may want to tell the actor to change speed, like slowing down at certain points along the path.

		public MPAction      action;
		public MPNode?       actionStart;
		public MPNode?       actionTarget;
		public Option<float> actionHeight;

		public bool        reached_target;
		public (bool, int) reached_index;
		public (bool, int) reached_region_node;

		public void SetReachedTarget(MPPath path, PathFollowState state)
		{
			reached_target = true;
			SetReachedNode(path, state);
		}

		public void SetReachedNode(MPPath path, PathFollowState state)
		{
			reached_index  = (true, state.index);
			if (path.Nodes.WrapGet(state.index).is_raw_node)
				reached_region_node = (true, state.index);
		}
		// No movement
		public static PathFollowOutput Default = new PathFollowOutput {
			direction           = Vector3.zero,
			speed               = null,
			action              = MPAction.Move,
			actionTarget        = null,
			reached_target      = false,
			reached_index       = (false, 0),
			reached_region_node = (false, 0),
		};

		public static PathFollowOutput NoMovement = new PathFollowOutput {
			direction           = Vector3.zero,
			speed               = 0,
			action              = MPAction.Move,
			actionTarget        = null,
			reached_target      = false,
			reached_index       = (false, 0),
			reached_region_node = (false, 0),

		};
	}

	//TODO: We still need to deal with closed paths properly!
	//TODO: Add projecting along a paths to deal with inconsistent speeds when points are very close together.
	public static partial class MotionPlanning
	{
		static void GetTargetIndiciesAndDirection(MPPath path, ref PathFollowState state, out int dir, out int target/*, out int? target_raw*/)
		{
			if (state.loop || state.target_end) {
				if(path.HasRawIndicies)
					target = path.HasRawIndicies ? path.RawToCalculatedIndicies.Last() : path.Nodes.Count - 1;
				else
					target = path.Nodes.Count - 1;
			} else {
				target = state.target_index;
			}

			/*if (path.HasRawIndicies)
				target = state.target_end ? path.RawToCalculatedIndicies.Last() : path.RawToCalculatedIndicies.WrapGet(state.target_index);*/

			dir = 0;

			int dir_forwards = 0, dir_backwards = 0;

			if(state.follow_mode == PathFollowMode.Calculated) {
				dir_forwards  = dir = (int) Mathf.Sign(target      - state.index);
				dir_backwards = dir = (int) Mathf.Sign(state.index - target);
			} else {
				dir_forwards  = dir = (int) Mathf.Sign(target                 - state.index_raw.index);
				dir_backwards = dir = (int) Mathf.Sign(state.index_raw.index - target);
			}

			switch (state.direction) {
				case PathDirection.Forwards:
					dir = dir_forwards;
						break;

				case PathDirection.Backwards:
					dir = (int)Mathf.Sign(state.index - target);
					break;

				case PathDirection.Closest:
					int dist_forwards  = Mathf.Abs(target - state.index);
					int dist_backwards = Mathf.Abs(state.index - target);

					// Default to moving forwards
					if (dist_forwards >= dist_backwards) {
						dir = dir_forwards;
					} else if (dist_backwards > dist_forwards) {
						dir = dir_backwards;
					}
					break;
			}
		}

		static void GetNodes(MPPath path, ref PathFollowState state, int dir, out MPNode prev, out MPNode next)
		{
			if (path.HasRawIndicies && state.follow_mode == PathFollowMode.Raw) {
				prev = path.Nodes.WrapGet(path.RawToCalculatedIndicies.WrapGet(state.index_raw.index));
				next = path.Nodes.WrapGet(!state.on_path ? path.RawToCalculatedIndicies.WrapGet(state.index_raw.index) : path.RawToCalculatedIndicies.WrapGet(state.index_raw.index + dir));
			} else {
				prev = path.Nodes.WrapGet(state.index);
				next = !state.on_path ? path.Nodes.WrapGet(state.index) : path.Nodes.WrapGet(state.index + dir);

				/*prev = path.Nodes.WrapGet(path.RawToCalculatedIndicies.WrapGet(state.index));
				next = path.Nodes.WrapGet(!state.on_path ? path.RawToCalculatedIndicies.WrapGet(state.index) : path.RawToCalculatedIndicies.WrapGet(state.index + dir));*/
			}
		}

		static void IncrementIndex(MPPath path, ref PathFollowState _state, int i)
		{
			if(_state.follow_mode == PathFollowMode.Raw && path.HasRawIndicies) {
				_state.index_raw.index += i;
				_state.index           =  path.RawToCalculatedIndicies.WrapGet(_state.index_raw.index);

				_state.UpdateRawIndex(path);
			} else {
				_state.index += i;
				if (_state.loop) {
					_state.index %= path.Nodes.Count;
				}
				_state.UpdateRawIndex(path);
			}
		}

		// Raw nodes:		 Nodes specified by an outside input. It could be simply the start and the goal position, or the nodes of a region index.
		// Calculated nodes: What the pathfinding system spits out.
		// Obviously, there may be more calculated nodes than base nodes.

		public static PathFollowOutput FollowPath(MPPath path, ref PathFollowState state, Vector3 position, float speed)
		{
			PathFollowOutput output = PathFollowOutput.Default;

			state.UpdateRawIndex(path);

			if (!state.translated_to_calc && state.just_started && state.region_path &&
				state.follow_mode != PathFollowMode.Raw && state.index >= 0)
			{
				state.index			= path.RawToCalculatedIndicies[state.index];
				state.target_index	= path.RawToCalculatedIndicies[state.target_index];

			}

			GetTargetIndiciesAndDirection(path, ref state, out int dir, out int target_index);
			GetNodes(path, ref state, dir, out MPNode prev_node, out MPNode next_node);

			//state.UpdateRawIndex(path);

			Vector3 dir_to_next = (next_node.point - position).normalized;
			Vector3 prev_position;

			// If we're already at the target index, don't do anything.
			if (state.on_path && at_target_index(ref state) && !state.loop) {
				output.reached_target = true;
				return output;
			}

			// Figure out the position of the previous node
			if (state.on_path) {
				prev_position = prev_node.point;
			} else if (state.just_started) {
				prev_position = position;
			} else {
				prev_position = state.last_starting_pos.GetValueOrDefault(position);
			}

			// If the first node is an action, we need to just do that action.
			if (state.just_started ) {
				state.just_started = false;

				state.start_index		= state.index_raw.index;
				state.last_starting_pos = position;

				// Properly handle if we started the path standing exactly on the current index node
				if(Vector3.Distance(position, next_node.point) <= Mathf.Epsilon || dir_to_next.magnitude <= Mathf.Epsilon) {
					state.last_starting_pos = next_node.point;
					state.on_path           = true;

					//IncrementIndex(path, ref state, 1);
					GetNodes(path, ref state, dir, out prev_node, out next_node);
					//update_nodes(ref state);

					if (at_target_index(ref state) && !state.loop)
						output.SetReachedTarget(path, state);

				} else if (Vector3.Distance(position, prev_node.point) <= Mathf.Epsilon) {
					state.on_path = true;
					GetNodes(path, ref state, dir, out prev_node, out next_node);
				}

				if (state.on_path && prev_node.action != MPAction.Move) {

					output.action       = prev_node.action;
					output.actionHeight = prev_node.action_height;

					output.actionStart  = prev_node;
					output.actionTarget = next_node;

					state.last_starting_pos = next_node.point;

					IncrementIndex(path, ref state, 1);
					GetNodes(path, ref state, dir, out prev_node, out next_node);
					//update_nodes(ref state);

					if (at_target_index(ref state) && !state.loop)
						output.SetReachedTarget(path, state);

					return output;
				}
			}


			state.just_started = false;

			output.direction = dir_to_next;
			output.speed     = null;

			if(GameController.DebugMode) {
				Draw.ingame.CrossXZ(position,        Color.red);
				Draw.ingame.CrossXZ(prev_position,   Color.blue);
				Draw.ingame.CrossXZ(next_node.point, Color.green);
			}

			if (/*Vector2.Distance(position.xy(), next_node.point.xy()) < 0.2f ||*/ WillPassTarget2D(prev_position, next_node.point,
																									 position + (next_node.point - position).normalized * (speed * Time.deltaTime))) {

				var dist = Vector3.Distance(position, next_node.point);

				void set_spd_dir(Vector3 target)
				{
					output.direction = (target - position);
					output.speed     = Mathf.Min(speed, Vector3.Distance(position, target) / Time.deltaTime);
				}

				var previous_prev_node = prev_node;
				var previous_next_node = next_node;

				MPNode action_start_node	= default;
				MPNode action_end_node		= default;

				bool did_increment = false;

				// If we're on the path, then we need to increment. Otherwise, we need to switch to being on the path
				if (state.on_path) {
					//if(prev_node.action != MPAction.Move)
					did_increment = true;
					IncrementIndex(path, ref state, dir);
					GetNodes(path, ref state, dir, out action_start_node, out action_end_node);
					GetNodes(path, ref state, dir, out prev_node,         out next_node);
					set_spd_dir(next_node.point);
				} else {
					state.on_path = true;

					//if(prev_node.action != MPAction.Move)
						GetNodes(path, ref state, dir, out action_start_node, out action_end_node);
				}

				output.direction = (next_node.point - position);
				output.speed     = Mathf.Min(speed, Vector3.Distance(position, next_node.point) / Time.deltaTime);

				if (at_target_index(ref state) && !state.loop) {
					output.SetReachedTarget(path, state);
				} else {

					//MPNode  next_next_node = next_node;
					Vector3 next_next_dir  = (next_node.point - prev_node.point).normalized;

					Vector3 proj_point = next_node.point + next_next_dir * Mathf.Min(1 , (speed * Time.deltaTime) - dist);

					output.direction = (proj_point - position).normalized;
					output.speed     = Mathf.Min(speed, Vector3.Distance(prev_node.point, next_node.point) / Time.deltaTime);

					output.action = prev_node.action;

					if (output.action != MPAction.Move) {
						output.actionStart  = action_start_node;
						output.actionTarget = action_end_node;
						output.actionHeight = prev_node.action_height;
					}

					output.SetReachedNode(path, state);

					//if(!did_increment) IncrementIndex(path, ref state, dir);
				}
			}

			return output;

			bool at_target_index(ref PathFollowState _state)
			{
				if(_state.follow_mode == PathFollowMode.Raw) {
					return path.CalculatedToRawIndicies[_state.index] == target_index;
				} else {
					return _state.index == target_index;
				}
			}
		}

		public static PathFollowOutput TweenAlongPath(MPPath path, ref PathFollowState state, Vector3 position, TweenerTo tweener, DOGetter<Vector3> getter, DOSetter<Vector3> setter)
		{
			PathFollowOutput output = PathFollowOutput.Default;

			state.UpdateRawIndex(path);

			GetTargetIndiciesAndDirection(path, ref state, out int dir, out int target_index);
			GetNodes(path, ref state, dir, out MPNode prev_node, out MPNode next_node);

			if (state.just_started && !state.on_path && Vector3.Distance(position, next_node.point) <= 0.1f) {
				setter(next_node.point);
				state.on_path = true;

				GetNodes(path, ref state, dir, out prev_node, out next_node);
			}

			// If we're already at the target index, don't do anything.
			if (state.on_path && state.index == target_index) {
				output.reached_target = true;
				return output;
			}

			if (state.just_started) {
				state.just_started = false;

				state.last_starting_pos = position;


				StartTween(ref state, next_node.point);

				// TODO: Still support actions...?
				/*if(state.on_path && prev_node.action != MPAction.Move) {
					output.action       = prev_node.action;
					output.actionStart  = prev_node;
					output.actionTarget = next_node;

					state.last_starting_pos = next_node.point;

					state.index++;
					if (state.index == target_index)
						output.SetReachedTarget(path, state);

					return output;
				}*/

				// Properly handle if we started the path standing exactly on the current index node
				if(Vector3.Distance(position, next_node.point) <= Mathf.Epsilon) {
					state.last_starting_pos = next_node.point;

					IncrementIndex(path, ref state, 1);
					GetNodes(path, ref state, dir, out prev_node, out next_node);
					//update_nodes(ref state);

					if (at_target_index(ref state) && !state.loop)
						output.SetReachedTarget(path, state);
				}
			}

			state.just_started = false;

			if (state.tween == null || !state.tween.IsActive() || state.tween.IsComplete()) {

				//setter(next_node.point);

				if(state.on_path) {
					IncrementIndex(path, ref state, dir);
					GetNodes(path, ref state, dir, out prev_node, out next_node);
				} else {
					state.on_path = true;
				}

				if (at_target_index(ref state)) {
					output.SetReachedTarget(path, state);
				} else {

					StartTween(ref state, next_node.point);
					output.SetReachedNode(path, state);
				}
			}

			void StartTween(ref PathFollowState pfs, Vector3 target)
			{
				pfs.tween?.Kill(true);
				pfs.tween = tweener.ApplyTo(getter, setter, target);
			}

			return output;

			bool at_target_index(ref PathFollowState _state)
			{
				if(_state.follow_mode == PathFollowMode.Raw) {
					return path.CalculatedToRawIndicies[_state.index] == target_index;
				} else {
					return _state.index == target_index;
				}
			}
		}


		static Vector3[] tempPathPoints = new Vector3[2048];

		public static PathUpdateResult Pathing_UpdateState(ref PathingState pstate, Vector3 current_pos)
		{
			var result = new PathUpdateResult();

			//result.vt_test = (5, 5);

			switch(pstate.state)
			{
				case MPState.Idle: break;
				case MPState.PathError: result.calculation_failed = true; break;

				case MPState.Calculating: {
					bool error = false;

					if (!pstate.settings.AsyncCalculate) {
						(NNInfo info, bool ok) navMeshPos = GetPosOnNavmesh(current_pos);

						//Retry again with larger radius if not ok
						if (!navMeshPos.ok) {
							navMeshPos = GetPosOnNavmesh(current_pos, searchRadius: 1.5f);
							if(!navMeshPos.ok) {
								pstate.state              = MPState.PathError;
								result.calculation_failed = true;
								break;
							}
						}

						(MPPath Path, bool ok) pathingResult = (null, false);

						if (pstate.settings.Target == PathTarget.Position) {
							(NNInfo info, bool ok) target_hit = GetPosOnNavmesh(pstate.settings.TargetPosition);
							if (target_hit.ok) {
								pathingResult = GetNPCPathTo(navMeshPos.info.position, target_hit.info.position);
							}
							else {
                                error        = true;
                                pstate.error = MPError.NotOnNavmesh;
                            }
                        }
						else if(pstate.settings.Target == PathTarget.RegionPath)
						{
							var regionObject = RegionController.Live.GetByID(pstate.settings.TargetRegionPathID);

							if (regionObject is RegionPath path) {

								int num = 0;

								for (int i = 0; i < path.Points.Count; i++) {
									tempPathPoints[i] = path.GetWorldPoint(i);
									num++;
								}

								int   starting_node = 0;
								float last_dist     = float.PositiveInfinity;
								float dist;

								if (pstate.settings.StartFromClosestPoint) {
									for (int i = 0; i < tempPathPoints.Length; i++) {
										dist = Vector3.Distance(current_pos, tempPathPoints[i]);

										if (dist < last_dist) {
											last_dist     = dist;
											starting_node = i;
										}
									}
								}

								pathingResult = GetPathTo(navMeshPos.info.position, num, starting_node, true, tempPathPoints);
							}
						}

						if (pathingResult.ok && pathingResult.Path.Nodes.Count > 0) {
							pstate.Path  = pathingResult.Path;
							pstate.state = MPState.Running;

							pstate.current_segment = 0;
							pstate.prev_node       = ( new MPNode(), false );
							pstate.starting_pos    = current_pos;
						}
						else {
							error        = true;
                            pstate.error = MPError.CouldNotFind;
						}
					}

					if (error) {
						pstate.state 				= MPState.PathError;
						result.calculation_failed 	= true;
					}
				}
				break;

				case MPState.Running: {

					UpdateSpeedDir(ref pstate, current_pos, ref result);
				} break;

				case MPState.FollowerAction:{

				} break;
			}

			return result;
		}

		static void UpdateSpeedDir(ref PathingState pstate, Vector3 current_pos, ref PathUpdateResult result)
		{
			if (pstate.current_segment >= pstate.Path.Nodes.Count) {
				result.reached_path_end = true;
				return;
			}

			var target = pstate.Path.Nodes[pstate.current_segment];

			//Start moving towards the first node in the path if we don't have a previous one.
			var prev_point = ( pstate.prev_node.exists ) ? pstate.prev_node.node.point : pstate.starting_pos;

			//Run through the path
			/*if (!WillPassTarget(current_pos, target.point, prev_point, pstate.follower_speed)) {
				pstate.follower_dir = target.point - current_pos;
				pstate.follower_speed = pstate.settings.Speed;
				/*Follower.MP_SetDirection(target.point - follower_pos);
				Follower.MP_SetSpeed(speed);#1#
				result.reached_vert = false;
			}
			else
			{
				pstate.follower_dir   = target.point - current_pos;
				pstate.follower_speed = Mathf.Min(pstate.settings.Speed, Vector3.Distance(current_pos, target.point) / Time.deltaTime);
				/*Follower.MP_SetDirection(target.point - follower_pos);
				Follower.MP_SetSpeed( Mathf.Min(speed, Vector3.Distance(follower_pos, target.point) / Time.deltaTime));#1#

				pstate.prev_node = (target, true);
				pstate.current_segment++;

				result.reached_vert = true;
				result.vert_index   = pstate.current_segment;

				result.node = target;

				result.action = target.action;

				if (result.node.is_region_path_node) {
					result.reached_node = true;
					result.node_index = result.node.region_path_node_index;
				}

				if (pstate.current_segment >= pstate.Path.Nodes.Count) {
					pstate.current_segment = 0;
					if (!pstate.Path.Closed)
					{
						pstate.follower_speed = 0;
						pstate.state = MPState.Idle;

						result.reached_path_end = true;
					}
				}
			}*/
		}

		public static bool WillPassTarget(Vector3 position, Vector3 target, float speed, float dt)
			=> WillPassTarget(position, target, (target - position).normalized * speed, dt);

		public static bool WillPassTarget(Vector3 position, Vector3 target, Vector3 velocity, float dt)
		{
			return Vector3.Dot(
				(target - position).normalized,
				(target - (position + velocity * dt)).normalized) < 0;
		}

		public static bool WillPassTargetMeasuringFromStart(Vector3 start, Vector3 target, Vector3 next_position)
		{
			return (Vector3.Distance(start, target) <=
					Vector3.Distance(start, next_position));

			/*var nextPoint = follower_pos + ((target - follower_pos).normalized * spd * Time.deltaTime);
			return Vector3.Distance(prevPoint, nextPoint) + 0.1f >= Vector3.Distance(prevPoint, target);*/

		}

		public static bool WillPassTarget2D(Vector3 start, Vector3 target, Vector3 next_position)
		{
			Vector2 start_xz  = start.xz();
			Vector2 target_xz = target.xz();
			Vector2 next_xz   = next_position.xz();

			return (Vector2.Distance(start_xz, target_xz) <=
					Vector2.Distance(start_xz, next_xz));

			/*var nextPoint = follower_pos + ((target - follower_pos).normalized * spd * Time.deltaTime);
			return Vector3.Distance(prevPoint, nextPoint) + 0.1f >= Vector3.Distance(prevPoint, target);*/

		}
	}
}
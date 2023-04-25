using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anjin.Regions;
using Anjin.Util;
using AStar;
using Cysharp.Threading.Tasks;
using Drawing;
using MoonSharp.Interpreter;
using Pathfinding;
using Pathfinding.Recast;
using Pathfinding.Util;
using Pathfinding.Voxels;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using Util;
using Vexe.Runtime.Extensions;

namespace Anjin.MP
{
	//
	//	NEW SHIT
	//
	[Flags]
	public enum GraphLayer {
		Main	= 1 << 0,
		ParkAI	= 1 << 2,
		Water	= 1 << 3
	}

	public struct CalcSettings
	{
		public bool       process;
		public bool       funnel;
		public bool       simplify;
		public bool       closed;
		public GraphLayer layer;

		public static CalcSettings Default = new CalcSettings
		{
			process  = true,
			funnel   = true,
			simplify = true,
			closed   = false,
			layer = GraphLayer.Main,
		};

		public static GraphMask LayerToMask(GraphLayer layer)
		{
			GraphMask mask = 0;

			if(AStarHelper.Current) {
				if ((layer & GraphLayer.Main) == GraphLayer.Main && AStarHelper.MainMask.HasValue) mask |= AStarHelper.MainMask.Value;
				if ((layer & GraphLayer.ParkAI) == GraphLayer.ParkAI && AStarHelper.ParkAIMask.HasValue) mask |= AStarHelper.ParkAIMask.Value;
				if ((layer & GraphLayer.Water) == GraphLayer.Water && AStarHelper.WaterMask.HasValue) mask |= AStarHelper.WaterMask.Value;

			}

			/*switch (layer) {
				case GraphLayer.Main:

					/*else
						return GraphMask.FromGraphName(AStarHelper.MAIN_GRAPH_NAME);#1#

					break;

				case GraphLayer.ParkAI:

					if (AStarHelper.Current && AStarHelper.ParkAIMask.HasValue)
						return AStarHelper.ParkAIMask.Value;
					/*else
						return GraphMask.FromGraphName(AStarHelper.PARKAI_GRAPH_NAME);#1#
					break;

				case GraphLayer.Water:

					if (AStarHelper.Current && AStarHelper.WaterMask.HasValue)
						return AStarHelper.WaterMask.Value;
					/*else
						return GraphMask.FromGraphName(AStarHelper.PARKAI_GRAPH_NAME);#1#
					break;
			}*/

			return mask;
		}

		public GraphMask LayerToMask() => LayerToMask(layer);
	}

	public static partial class MotionPlanning
	{
		//	Public API
		//------------------------------------------------------------------------------

		// Path Calculation
		public static async UniTask<(ABPath, bool)> CalcRawPath(Vector3 start, Vector3 end, object claimer, CalcSettings? settings = null)
		{
			CalcSettings validSettings = settings.GetValueOrDefault(CalcSettings.Default);

			var path = ABPath.Construct(start, end, _onCalcComplete);
			path.nnConstraint = new NNConstraint() {
				graphMask = validSettings.LayerToMask()
			};
			path.Claim(claimer);

			_pathStates[path.pathID] = false;

			AstarPath.StartPath(path);

			await UniTask.WaitUntilValueChanged(path, _isPathDoneOrError);
			_pathStates.Remove(path.pathID);

			return (path, !path.error);
		}

		public static async UniTask<(MPPath, bool)>
			CalcPath(Vector3 start, Vector3 end, CalcSettings? settings = null)
		{
			CalcSettings validSettings = settings.GetValueOrDefault(CalcSettings.Default);

			(ABPath rawPath, bool ok) = await CalcRawPath(start, end, _claimer, settings);

			if (rawPath == null || rawPath.error)
				return (null, false);

			MPPath result;

			//TODO: Send settings to processing
			if (validSettings.process)
			{
				result = ProcessRawPath(rawPath, validSettings.closed).Item1;
			}
			else
			{
				result = AStarABPathToMPPath(rawPath);
			}

			rawPath.Release(_claimer);

			return (result, true);
		}


		public static async UniTask<(MPPath, bool)>
			CalcRegionPath(RegionPath path, CalcSettings? settings = null)
		{
			if (path.Points.Count < 2) return (null, false);

			// TODO: Remove allocation here
			List<ABPath> _regionSegments = ListPool<ABPath>.Claim();

			CalcSettings validSettings = settings.GetValueOrDefault(CalcSettings.Default);

			for (int i = 0; i < path.Points.Count - 1; i++)
			{
				Vector3 start = path.GetWorldPoint(i);
				Vector3 end   = path.GetWorldPoint(i + 1);

				var abpath = ABPath.Construct(start, end, _onCalcComplete);
				abpath.nnConstraint = new NNConstraint() {
					graphMask = validSettings.LayerToMask()
				};
				_regionSegments.Add(abpath);
			}

			MPPath result = new MPPath
			{
				Start       = path.Points[0].point,
				Destination = path.Points[path.Points.Count - 1].point,
			};

			List<Path> _paths = ListPool<Path>.Claim();
			_paths.Add(null);

			for (int i = 0; i < _regionSegments.Count; i++)
			{
				AstarPath.StartPath(_regionSegments[i]);
				_paths.Add(_regionSegments[i]);
			}

			_groups[_regionSegments[0]] = _paths;

			await UniTask.WaitUntilValueChanged(_regionSegments[0], _isGroupDoneOrError);

			for (int i = 0; i < _regionSegments.Count; i++)
			{
				var    rpath = _regionSegments[i];
				MPPath segment;

				if (validSettings.process)
				{
					segment = ProcessRawPath(rpath, validSettings.closed).Item1;
				}
				else
				{
					segment = AStarABPathToMPPath(rpath);
				}

				for (int j = 0; j < segment.Nodes.Count; j++)
				{
					var n = segment.Nodes[j];
					n.segment_id     = i;
					segment.Nodes[j] = n;
				}

				if(segment.Nodes.Count > 0) {
					var first = segment.Nodes[0];
					first.is_raw_node    = true;
					first.raw_node_index = i;
					segment.Nodes[0]     = first;

					if (i == _regionSegments.Count - 1) {
						var last = segment.Nodes[segment.Nodes.Count - 1];
						last.is_raw_node                       = true;
						last.raw_node_index                    = i + 1;
						segment.Nodes[segment.Nodes.Count - 1] = last;
					} else {
						segment.Nodes.RemoveAt(segment.Nodes.Count - 1);
					}

					result.Nodes.AddRange(segment.Nodes);
				}
			}

			result.BuildRawIndicies();

			for (int i = 0; i < result.Nodes.Count; i++) {
				var n = result.Nodes[i];
				n.index         = i;
				result.Nodes[i] = n;
			}

			_groups.Remove(_regionSegments[0]);
			ListPool<Path>.Release(_paths);
			ListPool<ABPath>.Release(_regionSegments);

			return (result, true);
		}

		//	Internals
		//-----------------------------------------------------------------------------

		// Path Calculation
		private static Dictionary<ushort, bool> _pathStates = new Dictionary<ushort, bool>();

		private static OnPathDelegate   _onCalcComplete     = OnCalcComplete;
		private static Func<Path, bool> _isPathDoneOrError  = IsPathDoneOrError;
		private static Func<Path, bool> _isGroupDoneOrError = GroupDoneOrError;

		private static void OnCalcComplete(Path p) => _pathStates[p.pathID] = true;

		private static bool IsPathDoneOrError(Path path)
		{
			if (path == null || path.error || !_pathStates.ContainsKey(path.pathID)) return true;
			return _pathStates[path.pathID];
		}

		private static Dictionary<Path, List<Path>> _groups = new Dictionary<Path, List<Path>>();

		private static bool GroupDoneOrError(Path path)
		{
			if (!_groups.TryGetValue(path, out var paths)) return true;

			if (paths[0] == null)
			{
				paths.RemoveAt(0);
				return false;
			}

			bool done = true;
			for (int i = 0; i < paths.Count; i++)
			{
				var rpath = paths[i];
				if (rpath != null && !rpath.error && rpath.CompleteState != PathCompleteState.Complete)
				{
					done = false;
					break;
				}
			}

			return done;
		}

		// Empty class so MotionPlanning (a static class) can claim the path
		// I'm not entirely sure I HAVE to claim it but better safe than sorry
		private class Claimer { }

		private static Claimer _claimer = new Claimer();
	}


	//
	//	OLD SHIT
	//

	public enum MPAction
	{
		Move,       // Move to the next point on the ground. Could be at any speed.
		FallDown,   // Falling off a ledge, trying to land on the next point.
		JumpUp,     // Jump up a ledge.
		JumpAcross, // Jump across to the next point with varying height.
		Teleport    // Teleport to the next point.
	}

	[MoonSharpUserData]
	public struct MPNode : IEquatable<MPNode>
	{
		public Vector3       point;
		public Vector3       up; // TODO: We'll need this for properly detecting how close an agent's position is to a target point
		public MPAction      action;
		public Option<float> action_height;

		public int index;

		public float speedMultiplier;

		//Some MPNodes will be verts spit out by the navmesh system and not based on any kind of preset path.
		public bool is_raw_node;
		public int  raw_node_index;

		public int segment_id;


		public MPNode(Vector3 point, MPAction action, int segment = 0)
		{
			up = Vector3.up;

			this.point    = point;
			this.action   = action;
			action_height = 0;

			index  = -1;

			speedMultiplier = 1;

			is_raw_node    = false;
			raw_node_index = -1;

			segment_id = segment;
		}

		public Color GetSegmentColor() => Color.HSVToRGB(((float) segment_id % 255) / 255, 0.5f, 1);

		public override string ToString()
		{
			return $"MPNode ({point}, {action}, {action_height}, {speedMultiplier})";
		}

		public bool Equals(MPNode other)
		{
			return point.Equals(other.point) && action == other.action && speedMultiplier.Equals(other.speedMultiplier);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is MPNode other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = point.GetHashCode();
				hashCode = (hashCode * 397) ^ (int) action;
				hashCode = (hashCode * 397) ^ speedMultiplier.GetHashCode();
				return hashCode;
			}
		}
	}

	//	 UNUSED, AS FAR AS I CAN TELL

	//Defines a path for serialization storage.
	/*public class MPPathDef
	{
		public List<MPNode> Nodes;
		public bool         Closed;


		public MPPathDef()
		{
			Nodes = new List<MPNode>();
		}
	}*/


	// NOTE (C.L.): MAYBE there's a use for keeping the base nodes & processed nodes, but we don't have it currently. Removed ProcessedNodes.
	public class MPPath
	{
		public List<MPNode> Nodes;
		public List<int>    RawToCalculatedIndicies;
		public List<int>    CalculatedToRawIndicies;
		public bool         Closed;

		public Vector3 Start       = Vector3.zero;
		public Vector3 Destination = Vector3.zero;

		public bool HasRawIndicies { get; private set; }

		public MPPath()
		{
			Nodes                   = new List<MPNode>();
			RawToCalculatedIndicies = new List<int>();
			CalculatedToRawIndicies = new List<int>();
		}

		public void BuildRawIndicies()
		{
			RawToCalculatedIndicies.Clear();
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (Nodes[i].is_raw_node) {
					RawToCalculatedIndicies.Add(i);
					CalculatedToRawIndicies.Add(Nodes[i].raw_node_index);
				} else {
					CalculatedToRawIndicies.Add(-1);
				}
			}

			HasRawIndicies = RawToCalculatedIndicies.Count > 0;
		}

		public MPNode GetNode(int index, PathFollowMode mode = PathFollowMode.Raw)
		{
			if (mode == PathFollowMode.Raw && HasRawIndicies) {
				return Nodes[RawToCalculatedIndicies[index]];
			} else {
				return Nodes[index];
			}
		}

		public float Length()
		{
			float len = 0;
			for (int i = 0; i < (Closed ? Nodes.Count - 1 : Nodes.Count); i++) {

				len += Vector3.Distance(Nodes[i].point, Nodes.WrapGet(i + 1).point);
			}

			return len;
		}

		/*public bool ReachesDestination(float allowance = 0.5f)
		{
			if (Nodes.Count == 0) return false;
			var last = Nodes.Last().point;
			return Vector3.Distance(last, Destination) <= allowance;
		}*/
	}

	public static partial class MotionPlanning
	{
		public const float NAVMESH_SAMPLE_RADIUS = 0.5f;

		//	GETTING BARE PATHS
		//--------------------------------------------------------------------
		static NavMeshPath navmesh_path;
		static ABPath      astar_path;

		static StartEndModifier startEndModifier = new StartEndModifier
		{
			//exactStartPoint = StartEndModifier.Exactness.ClosestOnNode,
			exactEndPoint = StartEndModifier.Exactness.ClosestOnNode,
		};

		public static (MPPath, bool) GetNPCPathTo(Vector3 start, Vector3 destination, bool closed = false)
		{
			Profiler.BeginSample("GetPathTo");

			//NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, navmesh_path);

			//Construct base path and calculate
			astar_path = ABPath.Construct(start, destination);

			AstarPath.StartPath(astar_path);
			AstarPath.BlockUntilCalculated(astar_path); // TODO use delegate in ABPath.Construct instead!

			startEndModifier.Apply(astar_path);
			//Astar_ApplyFunnel(astar_path);

			var mp_path = new MPPath
			{
				Start       = start,
				Destination = destination
			};

			/*for (int i = 0; i < astar_path.vectorPath.Count; i++)
			{
				Vector3 corner = astar_path.vectorPath[i];
				mp_path.BaseNodes.Add(new MPNode(corner, MPAction.Move, i));
			}

			mp_path.BaseNodes.Add(new MPNode(destination, MPAction.Move, mp_path.BaseNodes.Count));*/

			//	Split the path into parts
			//-------------------------------------------------------------------------


			//Reuse the funnel procedure
			var parts = Funnel.SplitIntoParts(astar_path);

			//Get the length of the longest part
			int maxLength = 0;
			for (int i = 0; i < parts.Count; i++)
			{
				var len = Mathf.Abs(parts[i].endIndex - parts[i].startIndex);
				if (len > maxLength)
					maxLength = len;
			}

			var temp_pos   = ListPool<Vector3>.Claim(maxLength);
			var temp_nodes = ListPool<GraphNode>.Claim(maxLength);

			for (int i = 0; i < parts.Count; i++)
			{
				var part = parts[i];

				for (int j = part.startIndex; j < part.endIndex; j++)
				{
					temp_pos.Add(astar_path.vectorPath[j]);
					temp_nodes.Add(astar_path.path[j]);
				}

				//Not a link. It's a segment we need to do potentially destructive processing on
				if (!part.isLink)
				{
					var temp_path = ABPath.FakePath(temp_pos, temp_nodes);

					//Do processing here!
					Astar_ApplyFunnel(temp_path, true, false);

					if (i == parts.Count - 1)
						temp_path.vectorPath.Add(destination);

					/*if(i == parts.Count-1)
						 startEndModifier.Apply(temp_path);*/

					for (int j = 0; j < temp_path.vectorPath.Count; j++)
						mp_path.Nodes.Add(new MPNode(temp_path.vectorPath[j], MPAction.Move, i));
				}
				else
				{
					if (part.startIndex < astar_path.path.Count - 1)
					{
						var l1 = NodeLink2.GetNodeLink(astar_path.path[part.startIndex]);
						var l2 = NodeLink2.GetNodeLink(astar_path.path[part.startIndex + 1]);

						//Debug.Log("Add Link To Path " + Time.time);

						AddLinkToPath(new MPNode(astar_path.vectorPath[part.startIndex], MPAction.Move, i), l1, l2, astar_path.path[part.startIndex], mp_path, i);
					}
				}


				temp_nodes.Clear();
				temp_pos.Clear();
			}

			//Astar_ApplyFunnel(astar_path, true, false);

			ListPool<Vector3>.Release(ref temp_pos);
			ListPool<GraphNode>.Release(ref temp_nodes);


			var mp = AStarABPathToMPPath(astar_path);
			if (mp != null)
				mp.Closed = closed;

			Profiler.EndSample();

			return (mp_path, astar_path.CompleteState == PathCompleteState.Complete);
		}

		//Path to a point (non-async)
		public static (MPPath, bool) GetPathTo(Vector3 start, Vector3 destination, bool closed = false)
		{
			/*if(navmesh_path == null) navmesh_path = new NavMeshPath();
			navmesh_path.ClearCorners();*/

			Profiler.BeginSample("GetPathTo");

			//NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, navmesh_path);

			//Construct base path and calculate
			astar_path = ABPath.Construct(start, destination);

			AstarPath.StartPath(astar_path);
			AstarPath.BlockUntilCalculated(astar_path); // TODO use delegate in ABPath.Construct instead!

			startEndModifier.Apply(astar_path);
			Astar_ApplyFunnel(astar_path);

			var mp_path = new MPPath
			{
				Start       = start,
				Destination = destination
			};

			for (int i = 0; i < astar_path.vectorPath.Count; i++)
			{
				Vector3 corner = astar_path.vectorPath[i];
				mp_path.Nodes.Add(new MPNode(corner, MPAction.Move, i));
			}

			mp_path.Nodes.Add(new MPNode(destination, MPAction.Move, mp_path.Nodes.Count));

			Profiler.EndSample();

			return (mp_path, astar_path.CompleteState == PathCompleteState.Complete);
		}

		public static (MPPath, bool) ProcessRawPath(ABPath raw, bool closed = false)
		{
			if (raw == null) return (null, false);

			Profiler.BeginSample("ProcessRawPath");
			startEndModifier.Apply(raw);

			var mp_path = new MPPath
			{
				Start       = raw.startPoint,
				Destination = raw.endPoint
			};

			bool  canBeSimplified = true;
			float deltaY          = Mathf.Abs(raw.startPoint.y - raw.endPoint.y);
			for (int i = 0; i < raw.vectorPath.Count - 1; i++)
			{
				float segDelta = Mathf.Abs(raw.vectorPath[i].y - raw.vectorPath[i + 1].y);
				if (segDelta < deltaY - 0.1f || segDelta > deltaY + 0.1f)
				{
					canBeSimplified = false;
					break;
				}
			}

			// See if the two points can be simplified down to a straight line, but ONLY if the path doesn't have any vertical bends.
			if (canBeSimplified && !RaycastOnGraph(raw.startPoint, raw.endPoint, out var hit))
			{
				mp_path.Nodes.Add(new MPNode(raw.startPoint, MPAction.Move));
				mp_path.Nodes.Add(new MPNode(raw.endPoint, MPAction.Move));
				return (mp_path, true);
			}

			//	Split the path into parts
			//-------------------------------------------------------------------------

			//Reuse the funnel procedure
			List<Funnel.PathPart> parts = Funnel.SplitIntoParts(raw);

			//Get the length of the longest part
			int maxLength = 0;
			for (int i = 0; i < parts.Count; i++)
			{
				var len = Mathf.Abs(parts[i].endIndex - parts[i].startIndex);
				if (len > maxLength)
					maxLength = len;
			}

			var temp_pos   = ListPool<Vector3>.Claim(maxLength);
			var temp_nodes = ListPool<GraphNode>.Claim(maxLength);

			for (int i = 0; i < parts.Count; i++)
			{
				var part = parts[i];

				for (int j = part.startIndex; j < part.endIndex; j++)
				{
					temp_pos.Add(raw.vectorPath[j]);
					temp_nodes.Add(raw.path[j]);
				}

				//Not a link. It's a segment we need to do potentially destructive processing on
				if (!part.isLink)
				{
					var temp_path = ABPath.FakePath(temp_pos, temp_nodes);

					//Do processing here!

					// Make sure to apply splitAtEveryPortal. Otherwise the funnel spits out paths that go through the air for whatever reasons.
					Astar_ApplyFunnel(temp_path, true, true, FunnelModifier.FunnelQuality.High);


					const float MAX_SIMPLIFY_DISTANCE = 4;
					const float MAX_SIMPLIFY_ANGLE    = 5;
					float       distSoFar             = 0;
					float       dirChangeSoFar        = 0;

					// Simplify funnel path
					if (temp_path.vectorPath.Count >= 2)
					{
						Vector3 dir = temp_path.vectorPath[1] - temp_path.vectorPath[0];

						Vector3 p1, p2;
						for (int j = 1; j < temp_path.vectorPath.Count - 1; j++)
						{
							p1 = temp_path.vectorPath[j];
							p2 = temp_path.vectorPath[j + 1];

							distSoFar += Vector3.Distance(p1, p2);
							Vector3 nextDir = p2 - p1;

							var angle = Vector3.Angle(dir, nextDir);

							if (dirChangeSoFar < MAX_SIMPLIFY_ANGLE &&
							    distSoFar < MAX_SIMPLIFY_DISTANCE)
							{
								if (angle < 2f)
								{
									temp_path.vectorPath.RemoveAt(j--);
									dirChangeSoFar += angle;
									//Debug.Log(j + ": " + angle);
								}
							}
							else
							{
								dirChangeSoFar = 0;
								distSoFar      = 0;
							}

							dir = nextDir;

							//DebugDraw.DrawMarker(p1, 0.3f, Color.green, 10, false);
						}
					}


					//AStar_Radius(temp_path, 1f);
					//AStar_ApplySmoothing(temp_path, SimpleSmoothModifier.SmoothType.Bezier);

					// Make sure the last segment's endpoint is at the destination,
					// but attempt to simplify so we don't have a sudden turn at the end of the path.
					if (i == parts.Count - 1)
					{
						var count = temp_path.vectorPath.Count;

						temp_path.vectorPath.Add(mp_path.Destination);

						// TODO: FInd some way to make this insure we're simplifying a path that's actually on the navmesh.
						// We can't have a path in the air!
						/*if(count > 1 && !RaycastOnGraph(temp_path.vectorPath[count - 1], temp_path.vectorPath[count - 2], out var info)) {

							temp_path.vectorPath.RemoveAt(count - 1);
						}*/
					}

					for (int j = 0; j < temp_path.vectorPath.Count; j++)
						mp_path.Nodes.Add(new MPNode(temp_path.vectorPath[j], MPAction.Move, i));
				}
				else
				{
					if (part.startIndex < raw.path.Count - 1)
					{
						var l1 = NodeLink2.GetNodeLink(raw.path[part.startIndex]);
						var l2 = NodeLink2.GetNodeLink(raw.path[part.startIndex + 1]);

						if (l1 != null && l2 != null)
						{
							AddLinkToPath(new MPNode(raw.vectorPath[part.startIndex], MPAction.Move, i), l1, l2, raw.path[part.startIndex], mp_path, i);
						}
						else
						{
							AstarLink adder_astarLink_1 = NodeLinkAdder.GetNodeLink(raw.path[part.startIndex]);
							AstarLink adder_astarLink_2 = NodeLinkAdder.GetNodeLink(raw.path[part.startIndex + 1]);

							AddLinkToPath(new MPNode(raw.vectorPath[part.startIndex], MPAction.Move, i), adder_astarLink_1, adder_astarLink_2, raw.path[part.startIndex], mp_path, i);
						}
					}
				}


				temp_nodes.Clear();
				temp_pos.Clear();
			}

			ListPool<Vector3>.Release(ref temp_pos);
			ListPool<GraphNode>.Release(ref temp_nodes);

			MPPath mp = AStarABPathToMPPath(raw);
			if (mp != null)
				mp.Closed = closed;

			Profiler.EndSample();

			return (mp_path, raw.CompleteState == PathCompleteState.Complete);
		}

		/*private static void ApplyFunnel(Path p, FunnelModifier.FunnelQuality quality = FunnelModifier.FunnelQuality.Medium, bool unwrap = false, bool splitAtEveryPortal = false)
		{
		}*/

		static void AddLinkToPath(MPNode firstNode, NodeLink2 link_1, NodeLink2 link_2, GraphNode node, MPPath path, int segment = 0)
		{
			var meta = link_2.GetComponent<NodeLinkMetadata>();
			if (link_1 == null || link_2 == null || meta == null) return;

			Vector3 a, b;
			if (link_1.startNode == node)
			{
				a = link_1.StartTransform.position;
				b = link_1.EndTransform.position;
			}
			else
			{
				a = link_1.EndTransform.position;
				b = link_1.StartTransform.position;
			}

			/*LinkType type = LinkType.None;
			switch (link_1.link_type) {
				case AStarLinkType.None: break;

				case AStarLinkType.JumpAuto: type = LinkType.JumpUpDown; break;

				case AStarLinkType.JumpUp:   type = LinkType.JumpDown; break;
				case AStarLinkType.JumpDown: type = LinkType.JumpDown; break;
			}*/

			AddLinkToPath(firstNode, a, b, AStarLinkType.None, default, node, path, segment);
		}

		static void AddLinkToPath(MPNode firstNode, AstarLink astarLink1, AstarLink astarLink2, GraphNode node, MPPath path, int segment = 0)
		{
			//var meta = link_2.GetComponent<NodeLinkMetadata>();
			if (astarLink1 == null || astarLink2 == null) return;

			Vector3 a, b;
			if (astarLink1.n1 == node)
			{
				a = astarLink1.p1;
				b = astarLink1.p2;
			}
			else
			{
				a = astarLink1.p2;
				b = astarLink1.p1;
			}


			AddLinkToPath(firstNode, a, b, astarLink1.link_type, new Option<float>(astarLink1.height) {IsSet = astarLink1.override_height}, node, path, segment);
		}

		static void AddLinkToPath(MPNode firstNode, Vector3 a, Vector3 b, AStarLinkType type, Option<float> height, GraphNode node, MPPath path, int segment = 0)
		{
			DebugDraw.DrawMarker(a, 0.5f, Color.red, 3, false);
			DebugDraw.DrawMarker(b, 0.5f, Color.green, 3, false);

			firstNode.action_height = height;

			switch (type)
			{
				// Note(C.L. 7-17-22): We are just treating None as Jump Auto currently
				case AStarLinkType.None:
				case AStarLinkType.JumpAuto:
					if (a.y <= b.y)
					{
						firstNode.action = MPAction.JumpUp;

						//Figure out if we need to move the point further towards the edge so the parabola doesn't collide with the geometry.
						// TODO(C.L.): this breaks jumping across. Needs to be more selective.
						/*float   start_height = 0.5f;
						Vector3 start        = new Vector3(a.x, b.y + start_height, a.z);
						Vector3 end          = new Vector3(b.x, b.y + start_height, b.z);
						Vector3 pos          = start;
						float   dist         = (b.y - a.y);

						float      iter = 6;
						RaycastHit hit  = new RaycastHit();
						for (int j = 0; j < iter; j++)
						{
							pos = Vector3.Lerp(start, end, j / iter);

							Physics.Raycast(pos, Vector3.down, out temp_hits[j], dist + 1f, walkable_mask);

							if (Mathf.Abs(temp_hits[j].point.y - b.y) <= 0.2f)
							{
								Debug.DrawLine(b, temp_hits[j].point, Color.red, 3, false);
								hit = temp_hits[j];
								break;
							}
						}*/

						path.Nodes.Add(firstNode);
						path.Nodes.Add(new MPNode(b, MPAction.Move, segment));
					}
					else
					{
						firstNode.action = MPAction.FallDown;
						path.Nodes.Add(firstNode);
					}

					break;

				case AStarLinkType.JumpUp:
					firstNode.action = MPAction.JumpUp;
					path.Nodes.Add(firstNode);
					path.Nodes.Add(new MPNode(b, MPAction.Move, segment));
					break;

				case AStarLinkType.JumpDown:
					firstNode.action = MPAction.FallDown;
					path.Nodes.Add(firstNode);
					path.Nodes.Add(new MPNode(b, MPAction.Move, segment));
					break;

				case AStarLinkType.JumpAcross:
					firstNode.action = MPAction.JumpAcross;
					path.Nodes.Add(firstNode);
					path.Nodes.Add(new MPNode(b, MPAction.Move, segment));
					break;
			}
		}


		public static (MPPath, bool) GetPathTo(Vector3 start, bool closed = false, params Vector3[] points) => GetPathTo(start, points.Length, 0, closed, points);

		public static (MPPath, bool) GetPathTo(Vector3 start, int number, int starting_index = 0, bool closed = false, params Vector3[] points)
		{
			if (points.Length == 0 || number == 0 || number > points.Length) return (null, false);

			if (navmesh_path == null) navmesh_path = new NavMeshPath();

			Vector3 p1 = start;
			Vector3 p2 = points[starting_index];

			var path = new MPPath();
			path.Closed = closed;

			//Debug.Log(starting_index);

			int count = 0;
			for (int i = starting_index; count < number + (closed ? 1 : 0); i++, count++)
			{
				if (i >= number) i = 0;

				navmesh_path.ClearCorners();
				bool success = NavMesh.CalculatePath(p1, p2, NavMesh.AllAreas, navmesh_path);
				if (success)
				{
					AppendNavmeshPath(path, navmesh_path);

					//TODO: Move this somewhere else? GetPathTo isn't supposed to be JUST for region paths.
					if (path.Nodes.Count > 0)
					{
						MPNode node;
						if (i == 0)
						{
							node                        = path.Nodes[0];
							node.is_raw_node    = true;
							node.raw_node_index = 0;
							path.Nodes[0]               = node;
						}

						node                        = path.Nodes.Last();
						node.is_raw_node    = true;
						node.raw_node_index = i;
						path.Nodes.SetLast(node);
					}
				}
				//Debug.Log(i);

				if (count < number - 1)
				{
					p1 = points[i];
					p2 = points[i + 1];
				}
				else
				{
					p1 = points[i];
					p2 = start;
				}
			}

			return (path, true);
		}

		public static MPPath NavMeshPathToMPPath(NavMeshPath nm_path)
		{
			if (nm_path == null) return null;

			var path = new MPPath();

			AppendNavmeshPath(path, nm_path);

			/*for (int i = 0; i < nm_path.corners.Length; i++) {
				path.BaseNodes.Add(new MPNode(nm_path.corners[i], MPAction.Move));
			}*/

			return path;
		}

		public static void AppendNavmeshPath(MPPath path, NavMeshPath nm_path)
		{
			if (nm_path == null || path == null) return;

			for (int i = 0; i < nm_path.corners.Length; i++)
			{
				var node = new MPNode(nm_path.corners[i], MPAction.Move);

				//Hopefully prevent duplicate nodes
				if (path.Nodes.Count < 1 || (path.Nodes.Count >= 1 && !node.Equals(path.Nodes[path.Nodes.Count - 1])))
					path.Nodes.Add(new MPNode(nm_path.corners[i], MPAction.Move));
			}
		}

		public static MPPath AStarABPathToMPPath(ABPath ab_path)
		{
			if (ab_path == null || ab_path.error) return null;

			var path = new MPPath();
			AppendABPath(path, ab_path);
			return path;
		}


		static RaycastHit[] temp_hits     = new RaycastHit[32];
		static LayerMask    walkable_mask = LayerMask.GetMask("Walkable");

		public static void AppendABPath(MPPath path, ABPath ab_path)
		{
			for (int i = 0; i < ab_path.vectorPath.Count; i++)
			{
				var node = new MPNode(ab_path.vectorPath[i], MPAction.Move);
				path.Nodes.Add(node);
			}
		}

#if UNITY_EDITOR
		static Vector3[]     _drawn_points;
		static List<Vector3> _scratchPoints;
#endif

		public static void DrawMPPathInEditorALINE(MPPath path, Color? color = null)
		{
#if UNITY_EDITOR
			if (_scratchPoints == null) _scratchPoints = new List<Vector3>();
			_scratchPoints.Clear();

			CommandBuilder builder = Draw.editor;

			MPNode node;
			for (int i = 0; i < path.Nodes.Count; i++)
			{
				node = path.Nodes[i];
				using (builder.WithColor(node.is_raw_node ? Color.magenta : Color.white))
				{
					builder.Circle(node.point, Vector3.up, 0.15f);
				}

				_scratchPoints.Add(node.point);
			}

			if (path.Closed && path.Nodes.Count > 1)
			{
				_scratchPoints.Add(path.Nodes[0].point);
			}

			builder.Polyline(_scratchPoints, color.GetValueOrDefault(Color.HSVToRGB(0.55f, 0.6f, 0.8f)));
#endif
		}

		public static void DrawPathInEditor(MPPath path, Color? color = null)
		{
			if (path == null) return;
#if UNITY_EDITOR
			DrawNodesInEditor(path.Nodes, color, path.Closed);

			Handles.color = Color.red;
			for (int i = 0; i < path.Nodes.Count; i++)
			{
				Handles.Label(path.Nodes[i].point, i.ToString());
			}

			Handles.color = Color.white;
#endif
		}

		public static void DrawNodesInEditor(List<MPNode> nodes, Color? color = null, bool closed = false)
		{
#if UNITY_EDITOR
			if (_drawn_points == null) _drawn_points = new Vector3[2048];
			int num                                  = 0;

			Handles.color = Color.white;
			for (int i = 0; i < nodes.Count; i++)
			{
				//Handles.color = nodes[i].GetSegmentColor();

				if (nodes[i].is_raw_node)
					Handles.color = Color.magenta;
				else
					Handles.color = Color.white;

				switch (nodes[i].action) {
					case MPAction.Move: 		Handles.color = Color.white; 	break;
					case MPAction.FallDown: 	Handles.color = Color.red; 		break;
					case MPAction.JumpAcross: 	Handles.color = Color.magenta; 	break;
					case MPAction.JumpUp: 		Handles.color = ColorsXNA.Orange; break;
				}

				Handles.CircleHandleCap(0, nodes[i].point, Quaternion.Euler(90, 0, 0), 0.15f, EventType.Repaint);

				_drawn_points[i] = nodes[i].point;
				num++;
			}

			if (closed)
			{
				_drawn_points[num] = nodes[0].point;
				num++;
			}

			Handles.color = color.GetValueOrDefault(Color.HSVToRGB(0.55f, 0.6f, 0.8f));
			Handles.DrawAAPolyLine(4, num, _drawn_points);
#endif
		}

		public static void DrawPolyLineInEditor(List<Vector3> points, Color? col = null, bool closed = false, Action<Vector3> draw_point_func = null)
		{
#if UNITY_EDITOR
			if (_drawn_points == null) _drawn_points = new Vector3[2048];
			int num                                  = 0;

			Handles.color = Color.white;
			for (int i = 0; i < points.Count; i++)
			{
				draw_point_func?.Invoke(points[i]);

				_drawn_points[i] = points[i];
				num++;
			}

			if (closed)
			{
				_drawn_points[num] = points[0];
				num++;
			}

			Handles.color = col.GetValueOrDefault(Color.HSVToRGB(0.55f, 0.6f, 0.8f));
			Handles.DrawAAPolyLine(4, num, _drawn_points);
#endif
		}

		public static void OnGraphChange()
		{
			_nearestNodeConstraint = new NNConstraint
			{
				constrainDistance    = true,
				walkable             = true,
				constrainWalkability = true,
			};

			// Mask is now set from layer where used
			/*if (AstarPath.active != null)
			{
				var graph = AstarPath.active.data.FindGraph(g => g is RecastGraph);

				if (graph != null)
					_nearestNodeConstraint.graphMask = GraphMask.FromGraph(graph);
			}*/
		}

		private static NNConstraint _nearestNodeConstraint = new NNConstraint
		{
			constrainDistance    = true,
			walkable             = true,
			constrainWalkability = true,
		};

		public static (NNInfo, bool) GetPosOnNavmesh(Vector3 pos, GraphLayer layer = GraphLayer.Main, float searchRadius = NAVMESH_SAMPLE_RADIUS)
		{
			Profiler.BeginSample("GetPosOnNavmesh");

			if (AstarPath.active == null)
			{
				Profiler.EndSample();
				return (new NNInfo(), false);
			}

			_nearestNodeConstraint.graphMask = CalcSettings.LayerToMask(layer);

			var prevDistance = AstarPath.active.maxNearestNodeDistance;
			AstarPath.active.maxNearestNodeDistance = searchRadius;

			var info = AstarPath.active.GetNearest(pos, _nearestNodeConstraint);

			AstarPath.active.maxNearestNodeDistance = prevDistance;

			Profiler.EndSample();

			return (info, info.node != null);
		}

		public static bool RaycastOnGraph(Vector3 source, Vector3 target, out GraphHitInfo info, GraphLayer layer = GraphLayer.Main, float searchRadius = NAVMESH_SAMPLE_RADIUS, bool snapEnd = true)
		{
			Profiler.BeginSample("RaycastOnGraph");

			info = new GraphHitInfo();

			if (AstarPath.active == null || AstarPath.active.graphs.Length <= 0)
			{
				Profiler.EndSample();
				return false;
			}

			IRaycastableGraph graph = null;
			switch (layer) {
				case GraphLayer.Main:	graph = AStarHelper.Current.MainGraph;   break;
				case GraphLayer.ParkAI: graph = AStarHelper.Current.ParkAIGraph; break;
				case GraphLayer.Water:  graph = AStarHelper.Current.WaterGraph;  break;
			}

			/*for (int i = 0; i < AstarPath.active.graphs.Length; i++)
			{
				if (AstarPath.active.graphs[i] is RecastGraph g && g.name == AStarHelper.PARKAI_GRAPH_NAME)
				{
					graph = g;
					break;
				}
			}*/

			if (graph == null) {
				Profiler.EndSample();
				return false;
			}

			var prevDistance = AstarPath.active.maxNearestNodeDistance;
			AstarPath.active.maxNearestNodeDistance = searchRadius;

			/*Profiler.BeginSample("Nearest 1");
			var nearest1 = AstarPath.active.GetNearest(source, _nearestNodeConstraint);
			Profiler.EndSample();*/

			Vector3 _target = target;

			if (snapEnd)
			{
				Profiler.BeginSample("Nearest 2");

				_nearestNodeConstraint.graphMask = CalcSettings.LayerToMask(layer);
				_target = AstarPath.active.GetNearest(target, _nearestNodeConstraint).position;
				Profiler.EndSample();
			}

			AstarPath.active.maxNearestNodeDistance = prevDistance;
			/*if (nearest2.node == null)
				return false;*/

			Profiler.BeginSample("Linecast");
			var result = graph.Linecast(source, _target, null, out info);
			Profiler.EndSample();

			Profiler.EndSample();
			return result;
		}

		static string walkable_tag   = "NAV_WALKABLE";
		static string unwalkable_tag = "NAV_UNWALKABLE";
		public static string exclude_tag	 = "NAV_EXCLUDE";

		public static void SetRecastWalkableStatus(RasterizationMesh mesh, GameObject obj)
		{
			if (mesh == null || obj == null) return;

			mesh.area = -1;

			if (obj.CompareTag(walkable_tag))
			{
				mesh.area = 0;
				return;
			}
			else if (obj.CompareTag(unwalkable_tag))
			{
				mesh.area = -1;
				return;
			}

			var parent = obj.transform.parent;

			while (parent != null)
			{
				if (parent.CompareTag(walkable_tag))
				{
					mesh.area = 0;
					return;
				}
				else if (parent.CompareTag(unwalkable_tag))
				{
					mesh.area = -1;
					return;
				}

				parent = parent.parent;
			}
		}

		public static void SetRecastWalkableStatusBurst(ref RecastMeshGathererBurst.GatheredMesh mesh, GameObject obj, RecastMeshGathererBurst gatherer)
		{
			if (obj == null) return;

			mesh.area = -1;

			var mod = obj.GetComponent<RecastMeshObj>();
			if (mod)
			{
				mesh.solid = mod.solid;
			}
			else mesh.solid = true;

			if (gatherer.UseWalkableMask) {
				if (((1 << obj.layer) & gatherer.WalkableMask) != gatherer.WalkableMask) {
					mesh.area = -1;
					return;
				} else {
					mesh.area = 0;
					return;
				}
			}

			if (gatherer.UseUnwalkableMask) {
				if (((1 << obj.layer) & gatherer.UnwalkableMask) == gatherer.UnwalkableMask) {
					mesh.area = -1;
					return;
				}

			}

			if (obj.CompareTag(walkable_tag))
			{
				mesh.area = 0;
				return;
			}
			else if (obj.CompareTag(unwalkable_tag))
			{
				mesh.area = -1;
				return;
			}

			var parent = obj.transform.parent;

			while (parent != null)
			{
				if (parent.CompareTag(walkable_tag))
				{
					mesh.area = 0;
					return;
				}
				else if (parent.CompareTag(unwalkable_tag))
				{
					mesh.area = -1;
					return;
				}

				parent = parent.parent;
			}

		}

		/*public void SimplifyNavMeshPath(NavMeshPath path)
		{
			int        lastSimplifiy = 0;
			NavMeshHit raycastHit;

			for (int i = 0; i < cornerCount; i++)
			{
				var point = results[i];

				var type = query.GetPolygonType(point.polygon);

				var  newPoint = new CharacterPathPoint(CharacterPathPoint.Type.Walk, results[i].polygon, results[i].position);
				bool addPoint = true;

				if (i < cornerCount - 1)
				{
					//Figure out the type of point
					if (type == NavMeshPolyTypes.Ground)
					{
						if (SimplifyWithRaytrace && i != 0 && i < cornerCount - 1)
						{
							if (Mathf.Abs(results[i + 1].position.y - results[lastSimplifiy].position.y) > 0.2f
								|| NavMesh.Raycast(
									results[lastSimplifiy].position,
									results[i + 1].position,
									out raycastHit,
									NavMesh.AllAreas))
							{
								lastSimplifiy = i;
							}
							else
							{
								addPoint = false;
							}
						}
					}
					//Handle off mesh connections
					else if (type == NavMeshPolyTypes.OffMeshConnection)
					{
						var nextPoint = results[i + 1];
						var diff      = nextPoint.position.y - point.position.y;

						if (NavMesh.SamplePosition(
							point.position,
							out NavMeshHit hit,
							0.5f,
							1 << NavMesh.GetAreaFromName("SurfaceLink")))
						{
							//Just add the point if it's a surface link
							addPoint = false;
							i++;
						}
						else
						{
							if (diff >= -0.3f)
							{
								//We need to add a command to actually walk to the point before jumping link actions, otherwise it'll do it early.
								Path.Points.Add(newPoint);
								if (diff > 0.3f)
									newPoint.type = CharacterPathPoint.Type.JumpUp;
								else if (Mathf.Abs(diff) < 0.3f)
									newPoint.type = CharacterPathPoint.Type.JumpAcross;
							}
							else
								newPoint.type = CharacterPathPoint.Type.FallDown;
						}
					}
				}

				if (addPoint) Path.Points.Add(newPoint);
				UnsimplifiedPath.Points.Add(newPoint);
			}
		}*/


		//Path to a point (async)
		//	public PathRequest GetPathToAsync(Vector3 point);
		//	public (bool, MPPath) CheckGetPathRequest(PathRequest request);

		//	PATH PROCESSING
		//--------------------------------------------------------------------

		//	public void ProcessFullPath(MPPath path);
		//	public void ProcessPath(MPPath path, int start, int end);
	}

	public class RaycastModifierUtil
	{
#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Raycast Simplifier Modifier")]
		public static void AddComp(UnityEditor.MenuCommand command)
		{
			(command.context as Component).gameObject.AddComponent(typeof(RaycastModifier));
		}
#endif

		/// <summary>Use Physics.Raycast to simplify the path</summary>
		public bool useRaycasting = true;

		/// <summary>
		/// Layer mask used for physics raycasting.
		/// All objects with layers that are included in this mask will be treated as obstacles.
		/// If you are using a grid graph you usually want this to be the same as the mask in the grid graph's 'Collision Testing' settings.
		/// </summary>
		public LayerMask mask = -1;

		/// <summary>
		/// Checks around the line between two points, not just the exact line.
		/// Make sure the ground is either too far below or is not inside the mask since otherwise the raycast might always hit the ground.
		///
		/// See: https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html
		/// </summary>
		[Tooltip("Checks around the line between two points, not just the exact line.\nMake sure the ground is either too far below or is not inside the mask since otherwise the raycast might always hit the ground.")]
		public bool thickRaycast;

		/// <summary>Distance from the ray which will be checked for colliders</summary>
		[Tooltip("Distance from the ray which will be checked for colliders")]
		public float thickRaycastRadius;

		/// <summary>
		/// Check for intersections with 2D colliders instead of 3D colliders.
		/// Useful for 2D games.
		///
		/// See: https://docs.unity3d.com/ScriptReference/Physics2D.html
		/// </summary>
		[Tooltip("Check for intersections with 2D colliders instead of 3D colliders.")]
		public bool use2DPhysics;

		/// <summary>
		/// Offset from the original positions to perform the raycast.
		/// Can be useful to avoid the raycast intersecting the ground or similar things you do not want to it intersect
		/// </summary>
		[Tooltip("Offset from the original positions to perform the raycast.\nCan be useful to avoid the raycast intersecting the ground or similar things you do not want to it intersect")]
		public Vector3 raycastOffset = Vector3.zero;

		/// <summary>Use raycasting on the graphs. Only currently works with GridGraph and NavmeshGraph and RecastGraph. </summary>
		[Tooltip("Use raycasting on the graphs. Only currently works with GridGraph and NavmeshGraph and RecastGraph. This is a pro version feature.")]
		public bool useGraphRaycasting;

		/// <summary>
		/// Higher quality modes will try harder to find a shorter path.
		/// Higher qualities may be significantly slower than low quality.
		/// [Open online documentation to see images]
		/// </summary>
		[Tooltip("When using the high quality mode the script will try harder to find a shorter path. This is significantly slower than the greedy low quality approach.")]
		public Quality quality = Quality.Medium;

		public enum Quality
		{
			/// <summary>One iteration using a greedy algorithm</summary>
			Low,
			/// <summary>Two iterations using a greedy algorithm</summary>
			Medium,
			/// <summary>One iteration using a dynamic programming algorithm</summary>
			High,
			/// <summary>Three iterations using a dynamic programming algorithm</summary>
			Highest
		}

		static readonly int[]         iterationsByQuality = new[] {1, 2, 1, 3};
		static          List<Vector3> buffer              = new List<Vector3>();
		static          float[]       DPCosts             = new float[16];
		static          int[]         DPParents           = new int[16];

		Filter cachedFilter = new Filter();

		NNConstraint cachedNNConstraint = NNConstraint.None;

		class Filter
		{
			public          Path                         path;
			public readonly System.Func<GraphNode, bool> cachedDelegate;

			public Filter()
			{
				cachedDelegate = this.CanTraverse;
			}

			bool CanTraverse(GraphNode node)
			{
				return path.CanTraverse(node);
			}
		}

		public void Apply(Path p)
		{
			if (!useRaycasting && !useGraphRaycasting) return;

			var points = p.vectorPath;
			cachedFilter.path = p;

			// Use the same graph mask as the path.
			// We don't want to use the tag mask or other options for this though since then the linecasting will be will confused.
			cachedNNConstraint.graphMask = p.nnConstraint.graphMask;

			if (ValidateLine(null, null, p.vectorPath[0], p.vectorPath[p.vectorPath.Count - 1], cachedFilter.cachedDelegate, cachedNNConstraint))
			{
				// A very common case is that there is a straight line to the target.
				var s = p.vectorPath[0];
				var e = p.vectorPath[p.vectorPath.Count - 1];
				points.ClearFast();
				points.Add(s);
				points.Add(e);
			}
			else
			{
				int iterations = iterationsByQuality[(int) quality];
				for (int it = 0; it < iterations; it++)
				{
					if (it != 0)
					{
						Polygon.Subdivide(points, buffer, 3);
						Memory.Swap(ref buffer, ref points);
						buffer.ClearFast();
						points.Reverse();
					}

					points = quality >= Quality.High ? ApplyDP(p, points, cachedFilter.cachedDelegate, cachedNNConstraint) : ApplyGreedy(p, points, cachedFilter.cachedDelegate, cachedNNConstraint);
				}

				if ((iterations % 2) == 0) points.Reverse();
			}

			p.vectorPath = points;
		}

		List<Vector3> ApplyGreedy(Path p, List<Vector3> points, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
		{
			bool canBeOriginalNodes = points.Count == p.path.Count;
			int  startIndex         = 0;

			while (startIndex < points.Count)
			{
				Vector3 start     = points[startIndex];
				var     startNode = canBeOriginalNodes && points[startIndex] == (Vector3) p.path[startIndex].position ? p.path[startIndex] : null;
				buffer.Add(start);

				// Do a binary search to find the furthest node we can see from this node
				int mn = 1, mx = 2;
				while (true)
				{
					int endIndex = startIndex + mx;
					if (endIndex >= points.Count)
					{
						mx = points.Count - startIndex;
						break;
					}

					Vector3 end     = points[endIndex];
					var     endNode = canBeOriginalNodes && end == (Vector3) p.path[endIndex].position ? p.path[endIndex] : null;
					if (!ValidateLine(startNode, endNode, start, end, filter, nnConstraint)) break;
					mn =  mx;
					mx *= 2;
				}

				while (mn + 1 < mx)
				{
					int     mid      = (mn + mx) / 2;
					int     endIndex = startIndex + mid;
					Vector3 end      = points[endIndex];
					var     endNode  = canBeOriginalNodes && end == (Vector3) p.path[endIndex].position ? p.path[endIndex] : null;

					if (ValidateLine(startNode, endNode, start, end, filter, nnConstraint))
					{
						mn = mid;
					}
					else
					{
						mx = mid;
					}
				}

				startIndex += mn;
			}

			Memory.Swap(ref buffer, ref points);
			buffer.ClearFast();
			return points;
		}

		List<Vector3> ApplyDP(Path p, List<Vector3> points, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
		{
			if (DPCosts.Length < points.Count)
			{
				DPCosts   = new float[points.Count];
				DPParents = new int[points.Count];
			}

			for (int i = 0; i < DPParents.Length; i++) DPCosts[i] = DPParents[i] = -1;
			bool canBeOriginalNodes                               = points.Count == p.path.Count;

			for (int i = 0; i < points.Count; i++)
			{
				float   d                   = DPCosts[i];
				Vector3 start               = points[i];
				var     startIsOriginalNode = canBeOriginalNodes && start == (Vector3) p.path[i].position;
				for (int j = i + 1; j < points.Count; j++)
				{
					// Total distance from the start to this point using the best simplified path
					// The small additive constant is to make sure that the number of points is kept as small as possible
					// even when the total distance is the same (which can happen with e.g multiple colinear points).
					float d2 = d + (points[j] - start).magnitude + 0.0001f;
					if (DPParents[j] == -1 || d2 < DPCosts[j])
					{
						var endIsOriginalNode = canBeOriginalNodes && points[j] == (Vector3) p.path[j].position;
						if (j == i + 1 || ValidateLine(startIsOriginalNode ? p.path[i] : null, endIsOriginalNode ? p.path[j] : null, start, points[j], filter, nnConstraint))
						{
							DPCosts[j]   = d2;
							DPParents[j] = i;
						}
						else
						{
							break;
						}
					}
				}
			}

			int c = points.Count - 1;
			while (c != -1)
			{
				buffer.Add(points[c]);
				c = DPParents[c];
			}

			buffer.Reverse();
			Memory.Swap(ref buffer, ref points);
			buffer.ClearFast();
			return points;
		}

		/// <summary>
		/// Check if a straight path between v1 and v2 is valid.
		/// If both n1 and n2 are supplied it is assumed that the line goes from the center of n1 to the center of n2 and a more optimized graph linecast may be done.
		/// </summary>
		protected bool ValidateLine(GraphNode n1, GraphNode n2, Vector3 v1, Vector3 v2, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
		{
			if (useRaycasting)
			{
				// Use raycasting to check if a straight path between v1 and v2 is valid
				if (use2DPhysics)
				{
					if (thickRaycast && thickRaycastRadius > 0 && Physics2D.CircleCast(v1 + raycastOffset, thickRaycastRadius, v2 - v1, (v2 - v1).magnitude, mask))
					{
						return false;
					}

					if (Physics2D.Linecast(v1 + raycastOffset, v2 + raycastOffset, mask))
					{
						return false;
					}
				}
				else
				{
					// Perform a normal raycast
					// This is done even if a thick raycast is also done because thick raycasts do not report collisions for
					// colliders that overlapped the (imaginary) sphere at the origin of the thick raycast.
					// If this raycast was not done then some obstacles could be missed.
					// This is done before the normal raycast for performance.
					// Normal raycasts are cheaper, so if it can be used to rule out a line earlier that's good.
					if (Physics.Linecast(v1 + raycastOffset, v2 + raycastOffset, mask))
					{
						return false;
					}

					// Perform a thick raycast (if enabled)
					if (thickRaycast && thickRaycastRadius > 0)
					{
						// Sphere cast doesn't detect collisions which are inside the start position of the sphere.
						// That's why we do an additional check sphere which is slightly ahead of the start and which will catch most
						// of these omissions. It's slightly ahead to avoid false positives that are actuall behind the agent.
						if (Physics.CheckSphere(v1 + raycastOffset + (v2 - v1).normalized * thickRaycastRadius, thickRaycastRadius, mask))
						{
							return false;
						}

						if (Physics.SphereCast(new Ray(v1 + raycastOffset, v2 - v1), thickRaycastRadius, (v2 - v1).magnitude, mask))
						{
							return false;
						}
					}
				}
			}

			if (useGraphRaycasting)
			{
#if !ASTAR_NO_GRID_GRAPH
				bool betweenNodeCenters = n1 != null && n2 != null;
#endif
				if (n1 == null) n1 = AstarPath.active.GetNearest(v1, nnConstraint).node;
				if (n2 == null) n2 = AstarPath.active.GetNearest(v2, nnConstraint).node;

				if (n1 != null && n2 != null)
				{
					// Use graph raycasting to check if a straight path between v1 and v2 is valid
					NavGraph graph  = n1.Graph;
					NavGraph graph2 = n2.Graph;

					if (graph != graph2)
					{
						return false;
					}

					var rayGraph = graph as IRaycastableGraph;
#if !ASTAR_NO_GRID_GRAPH
					GridGraph gg = graph as GridGraph;
					if (betweenNodeCenters && gg != null)
					{
						// If the linecast is exactly between the centers of two nodes on a grid graph then a more optimized linecast can be used.
						// This method is also more stable when raycasting along a diagonal when the line just touches an obstacle.
						// The normal linecast method may or may not detect that as a hit depending on floating point errors
						// however this method never detect it as an obstacle (and that is very good for this component as it improves the simplification).
						return !gg.Linecast(n1 as GridNodeBase, n2 as GridNodeBase, filter);
					}
					else
#endif
					if (rayGraph != null)
					{
						return !rayGraph.Linecast(v1, v2, out GraphHitInfo _, null, filter);
					}
				}
			}

			return true;
		}
	}
}
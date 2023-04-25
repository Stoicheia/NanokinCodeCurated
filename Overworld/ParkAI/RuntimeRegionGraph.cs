using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Regions;
using ImGuiNET;
using Sirenix.Utilities;
using UnityEngine;
using Util;
using Vexe.Runtime.Extensions;
using g = ImGuiNET.ImGui;
using Random = System.Random;

namespace Anjin.Nanokin.ParkAI
{
	public enum NodeType : byte {
		Empty,
		Shape,
		Portal,
		Spawn,
		Exit,

		PointOfInterest,
		Stall,

		Resting
	}


	public class Graph {
		public RegionGraph graph;
		public uint        ID;

		public List<Node> nodes;

		public Dictionary<string, Node> id_registry;

		public List<Node> AllPortals;
		public List<Node> AllShapes;
		public List<uint> ChildIDs;

		public List<Node[]>                   PortalPaths;
		public Dictionary<uint, List<Node[]>> PortalPathReg;

		public Graph(RegionGraph graph)
		{
			this.graph  = graph;
			ID          = (uint)Guid.NewGuid().ToString().GetHashCode();
			nodes       = new List<Node>();
			id_registry = new Dictionary<string, Node>();
			AllPortals  = new List<Node>();
			AllShapes   = new List<Node>();
			ChildIDs    = new List<uint>();

			PortalPaths   = new List<Node[]>();
			PortalPathReg = new Dictionary<uint, List<Node[]>>();
		}
	}

	public class Node {
		public NodeType type = NodeType.Empty;
		public uint     id   = 0;

		// Base
		public Graph graph;

		public List<AdjacentNode> adjacent = new List<AdjacentNode>();
		public List<Node>         children = new List<Node>();

		public IAgentBrain brain;

		public bool enabled = true;

		// All paths that contain this node
		public List<(Node[] array, int index)> paths = new List<(Node[], int)>();

		public bool[] OccupancySlots;

		public Vector3[] WalkablePoints;
		public bool      HasWalkablePoints;

		public List<NearbyPOI> NearbyPOIs = new List<NearbyPOI>();

		// Region interface
		public bool                  is_scene_obj = false;
		public SceneRegionObjectBase scene_object;

		public RegionObject        region_object;
		public RegionObjectSpatial spatial;
		public RegionShape2D       Shape;

		// Different Nodes
		public ParkAIGraphPortal portal;
		public GraphLocation     portal_destination = new GraphLocation(null, null);

		// Interactable Nodes
		public Slot[] usage_slots;
		public Slot[] queue_slots;

		// Stalls
		public StallType   stall_type;
		public ParkAIStall stall;

		// Resting
		public RestPoint[] rest_spaces;

		// Points of interest
		public POIType         poi_type;
		public int             poi_attraction_distance;
		public (bool, Vector3) poi_world_focus_point = (false, Vector3.zero);
		public Vector3[]       poi_spaces;

		// Utility
		public bool GetOpenUsageSlot(out Slot slot)
		{
			slot = null;
			if(usage_slots != null) {
				for (int i = 0; i < usage_slots.Length; i++) {
					if (!usage_slots[i].occupied) {
						slot = usage_slots[i];
						return true;
					}
				}
			}

			return false;
		}

		public bool GetOpenQueueSlot(out Slot slot)
		{
			// Search the queue from the back to the front, finding the next empty slot

			slot = null;
			if(queue_slots != null && queue_slots.Length > 0) {
				for (int i = queue_slots.Length - 1, j = 0; i >= 0; i--, j++) {
					var qs = queue_slots[i];

					// If the last slot on the list is occupied, we can't fill it. The people inside the queue will need to move forward
					if (j == 0 && qs.occupied)
						return false;

					// If we're at the first slot on the list and it isn't occupied, take it.
					// Otherwise only take an open slot if the next slot towards the front of the queue is occupied.
					if ((i == 0 && !qs.occupied) || (!qs.occupied && queue_slots[i - 1].occupied)) {
						slot = queue_slots[i];
						return true;
					}
				}
			}

			return false;
		}

		public bool AnyUsageSlots()
		{
			for (int i = 0; i < usage_slots.Length; i++) {
				if (!usage_slots[i].occupied)
					return true;
			}
			return false;
		}

		public bool AnyQueueSlots()
		{
			for (int i = 0; i < queue_slots.Length; i++) {
				if (!queue_slots[i].occupied)
					return true;
			}
			return false;
		}

		public bool NobodyInQueue()
		{
			for (int i = 0; i < queue_slots.Length; i++) {
				if (queue_slots[i].occupied)
					return false;
			}
			return true;
		}

		public bool TryGetRandomPointInside(out Vector3 point, Random rand)
		{
			point = Vector3.zero;
			if (type != NodeType.Shape) return false;

			if (HasWalkablePoints) {
				point = WalkablePoints.RandomElement(rand);
				point = Matrix4x4.TRS(Shape.Transform.Position, Shape.Transform.Rotation, Vector3.one).MultiplyPoint3x4(point);
				return true;
			}

			if(Shape != null) {
				point = Shape.GetRandomWorldPointInside();
				return true;
			}

			return false;
		}
	}

	public struct AdjacentNode
	{
		public Node     			 node;

		public RegionObject          obj;
		public RegionSpatialLinkBase link;

		public SceneRegionObjectBase scene_obj;

		public uint                  ID;

		public (Vector3 L, Vector3 R)? link_1_edge;
		public (Vector3 L, Vector3 R)? link_2_edge;

		public Vector3? link_1_worldpos;
		public Vector3? link_2_worldpos;

		// Utility
	}

	public class Slot {
		public bool    occupied;
		public Vector3 world_pos;
		public Vector3 direction;

		public Slot next;
		public Slot previous;

		public int index = -1;
	}

	public struct NearbyPOI
	{
		public int  distance;
		public uint ID;
		public Node node;
	}

	public struct GraphLocation
	{
		public Graph  graph;
		public Node   node;

		public RegionObjectSpatial region_obj;

		public GraphLocation(Graph graph, Node node, RegionObjectSpatial region_obj = null)
		{
			this.node  = node;
			this.graph = graph;

			if (node != null && region_obj == null)
				this.region_obj  = node.spatial;
			else this.region_obj = region_obj;
		}

		public bool IsValid => graph != null && node != null;

		public override string ToString()
		{
			return $"Graph: {graph.ID}, "                                          +
				   $"Node: {(node.spatial != null ? node.spatial.Name : "null")} " +
				   $"(G: {(node.spatial   != null ? node.spatial.ID : "null")}, R: {node.id})";
		}
	}

	public interface IRuntimeGraphHolder {
		void OnNodeDisable(RuntimeParkAIGraph graph, Node node);
		void OnNodeEnable(RuntimeParkAIGraph  graph, Node node);
	}

	public class RuntimeParkAIGraph {

		public List<Graph>       Graphs;
		public List<Node>        Nodes;
		public List<IAgentBrain> Brains;

		public Dictionary<RegionGraph, Graph> GraphsToRuntimeGraphs;

		public Dictionary<string, Node> RegionIDRegistry;
		public Dictionary<string, Node> RegionNameRegistry;


		private static List<Node> _scratch_nodes = new List<Node>();

		public IRuntimeGraphHolder Holder;

		public RuntimeParkAIGraph(IRuntimeGraphHolder holder)
		{
			Graphs    		= new List<Graph>();
			Nodes     		= new List<Node>();
			Brains 			= new List<IAgentBrain>();

			GraphsToRuntimeGraphs = new Dictionary<RegionGraph, Graph>();

			RegionIDRegistry   = new Dictionary<string, Node>();
			RegionNameRegistry = new Dictionary<string, Node>();

			Holder = holder;
		}

		public void AddGraph(RegionGraph graph)
		{
			if (graph == null) return;

			Graph graph_runtime = new Graph(graph);
			GraphsToRuntimeGraphs[graph] = graph_runtime;

			//Generate nodes
			for (int i = 0; i < graph.GraphObjects.Count; i++) {
				RegionObject robj = graph.GraphObjects[i];

				Node node = NodeFromRegionObject(robj);

				if(node != null) {
					node.graph                         = graph_runtime;
					graph_runtime.id_registry[robj.ID] = node;
					graph_runtime.nodes.Add(node);

					RegionIDRegistry[robj.ID] = node;
					if(!robj.Name.IsNullOrWhitespace() && !RegionNameRegistry.ContainsKey(robj.Name))
						RegionNameRegistry[robj.Name] = node;

					if(node.type == NodeType.Portal)
						graph_runtime.AllPortals.Add(node);

					if(node.type == NodeType.Shape)
						graph_runtime.AllShapes.Add(node);

					Nodes.Add(node);
				}
			}

			//Link nodes up
			for (int i = 0; i < graph.SpatialLinks.Count; i++) {
				RegionSpatialLinkBase link = graph.SpatialLinks[i];

				LinkFromSpatial(graph_runtime, link);
			}

			//Find portal sequences
			for (int i = 0; i < graph.Sequences.Count; i++) {
				var seq = graph.Sequences[i].Objects;

				if (seq.Count == 0) continue;
				RegionObject first = seq.First();
				RegionObject last = seq.Last();

				if (seq.Count == 1 && first == last) continue;	// No 1-long sequences

				if (first is ParkAIGraphPortal p1 && last is ParkAIGraphPortal p2) {

					Node find(RegionObject obj)
					{
						if (graph_runtime.id_registry.TryGetValue(obj.ID, out Node node))
							return node;

						return null;
					}

					_scratch_nodes.Clear();

					Node first_node = find(first);
					if (first_node == null) continue;

					Node last_node = find(last);
					if (last_node == null) continue;
					//temp_list_1.Add(first_node.node);

					for (int j = 0; j < seq.Count; j++) {
						RegionObject obj = seq[j];
						if (!graph_runtime.id_registry.ContainsKey(obj.ID)) continue;
						if (obj is RegionShape2D)
							_scratch_nodes.Add(graph_runtime.id_registry[obj.ID]);
					}

					//temp_list_1.Add(last_node.node);

					var len = _scratch_nodes.Count;

					var arr_1 = new Node[len];
					var arr_2 = new Node[len];

					for (int j = 0; j < len; j++) {
						arr_1[j]           = _scratch_nodes[j];
						arr_2[len -(j +1)] = _scratch_nodes[j];
					}

					graph_runtime.PortalPaths.Add(arr_1);
					graph_runtime.PortalPaths.Add(arr_2);

					var id1 = first_node.id;
					var id2 = last_node.id;

					if (!graph_runtime.PortalPathReg.ContainsKey(id1))
						graph_runtime.PortalPathReg[id1] = new List<Node[]>();

					if (!graph_runtime.PortalPathReg.ContainsKey(id2))
						graph_runtime.PortalPathReg[id2] = new List<Node[]>();

					graph_runtime.PortalPathReg[id1].Add(arr_1);
					graph_runtime.PortalPathReg[id2].Add(arr_2);

					for (int j = 0; j < _scratch_nodes.Count; j++) {
						_scratch_nodes[j].paths.Add((arr_1, j));
						_scratch_nodes[j].paths.Add((arr_2, len -(j +1)));
					}

				}
			}

			/*void BuildNearbyRecursive(PointOfInterestNode poi_node, RegionObjNodeBase _node, int distance, int depth)
			{
				foreach (var child in _node.Children) {
					if (child is PointOfInterestNode poi) {



						foreach (var adjacentNode in poi.Adjacent) {

						}

					}
				}
			}*/

			//Build nearby nodes
			/*foreach (var graphNode in GraphNodes) {
				foreach (var child in graphNode.Children) {
					if (child is PointOfInterestNode poi) {

						foreach (var adjacentNode in poi.Adjacent) {

						}

					}
				}
			}*/

			Graphs.Add(graph_runtime);
			//GraphNodes.Add(node);
		}

		public void LinkPortals()
		{
			for (int i = 0; i < Graphs.Count; i++) {
				for (int j = 0; j < Graphs[i].AllPortals.Count; j++) {
					var portal = Graphs[i].AllPortals[j];

					if (portal.region_object.ParentGraph == null) continue;

					if (!GraphsToRuntimeGraphs.TryGetValue(portal.region_object.ParentGraph, out Graph dest_graph))
						continue;

					if(!dest_graph.id_registry.TryGetValue(portal.portal.TargetPortalID, out var dest_node))
						continue;

					portal.portal_destination.graph = dest_graph;
					portal.portal_destination.node  = dest_node;
				}
			}
		}

		public void AddRuntimeObjects()
		{
			for (var i = 0; i < RegionController.trackedSceneObjects.Count; i++) {
				SceneRegionObjectBase obj = RegionController.trackedSceneObjects[i];

				for (int j = 0; j < Nodes.Count; j++) {
					if (Nodes[j].type != NodeType.Shape) continue;
					var shape = Nodes[j];

					if (shape.Shape != null) {

						bool    intersects        = false;
						Vector3 intersectionPoint =  obj.transform.localToWorldMatrix.MultiplyPoint3x4(obj.LinkOffset);
						switch (shape.Shape.Type) {
							case RegionShape2D.ShapeType.Rect:
								Vector3 box = new Vector3(shape.Shape.RectSize.x, 1, shape.Shape.RectSize.y);
								if (IntersectionUtil.Intersection_PointBox(intersectionPoint, shape.Shape.Transform.matrix, box))
									intersects = true;
								break;

							case RegionShape2D.ShapeType.Circle:
								if (IntersectionUtil.Intersection_PointCylinder(intersectionPoint, shape.Shape.Transform.matrix, shape.Shape.CircleRadius, 1))
									intersects = true;
								break;

							case RegionShape2D.ShapeType.Polygon:
								if (shape.Shape.TrianulationValid()) {

									var points = shape.Shape.PolygonPoints;
									var tris   = shape.Shape.PolygonTriangulation;

									for (int k = 0; k < tris.Length; k += 3) {
										Vector2 p1 = shape.Shape.PolygonPoints[tris[k]];
										Vector2 p2 = shape.Shape.PolygonPoints[tris[k + 1]];
										Vector2 p3 = shape.Shape.PolygonPoints[tris[k + 2]];

										if (IntersectionUtil.Intersection_PointTriangle(intersectionPoint, shape.Shape.Transform.matrix, p1, p2, p3, 1)) {
											intersects = true;
										}

									}
								}

								break;
						}

						if (intersects) {

							if (obj is SceneParkAIRestPoint rest) {
								var graph = shape.graph;

								Node node = new Node();
								node.id           = (uint)DataUtil.MakeShortID(10).GetHashCode();
								node.type         = NodeType.Resting;

								node.scene_object = rest;
								node.is_scene_obj = true;

								node.rest_spaces    = rest.Points.ToArray();
								node.OccupancySlots = new bool[rest.Points.Count];

								shape.adjacent.Add(new AdjacentNode {
									node            = node,
									scene_obj       = obj,
									ID              = node.id,
									link_1_worldpos = rest.transform.position,
									link_2_worldpos = rest.transform.position,
								});

								AddAdjacent(node, shape, rest.transform.position, rest.transform.position, null, null);

								graph.nodes.Add(node);
							}

						}
					}
				}
			}
		}

		public Node NodeFromRegionObject(RegionObject obj)
		{
			if (obj == null) return null;

			Node node 			= new Node();
			node.region_object 	= obj;
			node.id            	= (uint) obj.ID.GetHashCode();

			if (obj is RegionObjectSpatial spatial)
				node.spatial = spatial;

			ParkAIPointOfInterest     poi                = obj.Metadata.OfType<ParkAIPointOfInterest>().FirstOrDefault();
			ParkAIRestingPoint        resting            = obj.Metadata.OfType<ParkAIRestingPoint>().FirstOrDefault();
			PointDistributionMetadata point_distribution = obj.Metadata.OfType<PointDistributionMetadata>().FirstOrDefault();
			WalkableSurfaceGrid 	  walkable_points 	 = obj.Metadata.OfType<WalkableSurfaceGrid>().FirstOrDefault();
			ParkAIStall 	  		  stall 	 		 = obj.Metadata.OfType<ParkAIStall>().FirstOrDefault();

			switch (obj) {
				case ParkAIGraphPortal 		portal:
					node.type   = NodeType.Portal;
					node.portal = portal;
					break;

				case RegionShape2D shape:
					if (poi != null) {
						node.type     = NodeType.PointOfInterest;
						node.poi_type = poi.Type;

						if (poi.HasFocusPoint)
							node.poi_world_focus_point = (true, shape.Transform.Position + poi.FocusPointOffset);

						if (point_distribution != null && point_distribution.number > 0) {
							node.poi_spaces = new Vector3[point_distribution.number];
							point_distribution.GetFor(shape, node.poi_spaces);
							node.OccupancySlots = new bool[point_distribution.number];
						}

					} else if (resting != null) {
						node.type           = NodeType.Resting;
						node.rest_spaces    = resting.spaces.ToArray();
						node.OccupancySlots = new bool[resting.spaces.Count];

					} else if(stall != null) {

						node.type       = NodeType.Stall;
						node.stall_type = stall.Type;
						node.stall      = stall;

						AddQueueSlots(node, stall.Queue, 		shape.Transform.matrix);
						AddUsageSlots(node, stall.UsageSlots, 	shape.Transform.matrix);

						if(stall.Brain != null) {
							RegisterBrainForNode(node, stall.Brain);
						}

					} else {
						node.type  = NodeType.Shape;
						node.Shape = shape;
					}

					if (walkable_points != null) {
						int num = walkable_points.Points.Count;
						node.WalkablePoints = new Vector3[num];
						for (int j = 0; j < num; j++) {
							Vector3 pt = walkable_points.Points[j];
							node.WalkablePoints[j] = new Vector3(pt.x, 0, pt.z);
						}

						node.HasWalkablePoints = node.WalkablePoints.Length > 0;
					}

					break;
			}


			return node;
		}

		void RegisterBrainForNode<T>(Node node, AgentBrain<T> sourceBrain)
			where T : struct, IControlledAgentState
		{
			AgentBrain<T> brain = sourceBrain.Instantiate();
			node.brain = brain;
			Brains.Add(brain);
			brain.Init();
		}

		void AddAdjacent(Node                  self, Node     other,
						 Vector3?              pos1, Vector3? pos2, (Vector3 L, Vector3 R)? edge1, (Vector3 L, Vector3 R)? edge2,
						 RegionSpatialLinkBase link = null)
		{
			self.adjacent.Add(new AdjacentNode {
				node            = other,
				obj             = other.region_object,
				ID              = other.id,
				link            = link,
				link_1_worldpos = pos1, 	link_2_worldpos = pos2,
				link_1_edge     = edge1, 	link_2_edge     = edge2,
			});
		}

		public void LinkFromSpatial(Graph graph, RegionSpatialLinkBase link)
		{
			if (graph.id_registry.TryGetValue(link.FirstID,  out var first) &&
				graph.id_registry.TryGetValue(link.SecondID, out var second)) {

				Vector3? firstPos  = null;
				Vector3? secondPos = null;

				(Vector3 L, Vector3 R)? firstEdge  = null;
				(Vector3 L, Vector3 R)? secondEdge = null;

				if (link is RegionShape2DLink link2D) {
					if (first.region_object is RegionShape2D shape1)
						firstPos = link2D.GetPosition(shape1).GetValueOrDefault().GetWorldPosition(shape1);

					if (second.region_object is RegionShape2D shape2)
						secondPos = link2D.GetPosition(shape2).GetValueOrDefault().GetWorldPosition(shape2);

					if (link2D.Type == RegionShape2DLink.LinkAreaType.Plane) {
						var dir   = link2D.GetDirection();
						var width = link2D.PlaneWidth / 2;

						if(firstPos.HasValue) {
							var cross = Vector3.Cross(dir, Vector3.up) * width;

							firstEdge = (firstPos.Value - cross, firstPos.Value + cross);
						}

						if (secondPos.HasValue) {
							var cross = Vector3.Cross(dir, Vector3.up) * width;
							secondEdge = (secondPos.Value - cross, secondPos.Value + cross);
						}
					}
				}

				AddAdjacent(first,  second, firstPos,  secondPos, firstEdge,  secondEdge, link);
				AddAdjacent(second, first,  secondPos, firstPos,  secondEdge, firstEdge,  link);
			}
		}

		public void AddQueueSlots(Node node, ParkAIQueue queue, Matrix4x4? worldTransform)
		{
			if (node == null || queue == null) return;

			if (queue.GetNumSlots(out int num)) {
				node.queue_slots = new Slot[num];
				for (int i = 0; i < num; i++) {
					if (queue.GetSlotAt(i, out Vector3 position, out Vector3 direction)) {
						node.queue_slots[i] = new Slot {
							world_pos = worldTransform?.MultiplyPoint3x4(position) ?? position,
							direction = direction,
							index     = i,
							occupied  = false,
						};
					}
				}
			} else return;

			// Link slots together
			for (int i = 0; i < node.queue_slots.Length; i++) {
				var slot = node.queue_slots[i];

				slot.previous   = (i == node.queue_slots.Length - 1) 	? null : node.queue_slots[i + 1];
				slot.next 		= (i == 0) 								? null : node.queue_slots[i - 1];
			}
		}

		public void AddUsageSlots(Node node, List<UsageSlot> slots, Matrix4x4? worldTransform)
		{
			if (node == null || slots == null) return;

			node.usage_slots = new Slot[slots.Count];
			Slot prev = null;
			for (int i = 0; i < slots.Count; i++) {
				var slot = slots[i];

				node.usage_slots[i] = new Slot {
					world_pos = worldTransform?.MultiplyPoint3x4(slot.position) ?? slot.position,
					direction = MathUtil.DegreeToVector3XZ(slot.direction),
					index     = i,
					occupied  = false,
				};
			}
		}

		public bool TryGetNodeFromString(string nameOrID, out Node node)
		{
			node = null;

			if (nameOrID == null || nameOrID.Length <= 0) return false;

			if (nameOrID[0] == '$') {
				if(RegionIDRegistry.TryGetValue(nameOrID.Substring(1), out node))	return true;
			} else {
				if(RegionNameRegistry.TryGetValue(nameOrID, out node))				return true;
			}

			return false;
		}

		public void EnableNode(string regionNameOrID)
		{
			if(TryGetNodeFromString(regionNameOrID, out Node node)) EnableNode(node);
		}

		public void DisableNode(string regionNameOrID)
		{
			if(TryGetNodeFromString(regionNameOrID, out Node node)) DisableNode(node);
		}

		public void EnableNode(Node node)
		{
			if (node.enabled) return;
			node.enabled = true;
			Holder?.OnNodeEnable(this, node);
		}

		public void DisableNode(Node node)
		{
			if (!node.enabled) return;
			node.enabled = false;
			Holder?.OnNodeDisable(this, node);
		}

		private static StringBuilder _sb = new StringBuilder();

		public void DrawImgui()
		{
			for (int i = 0; i < Graphs.Count; i++) {
				Graph graph = Graphs[i];
				g.PushID(i);

				AImgui.Text($"{i + 1}: {graph.ID}", ColorsXNA.Goldenrod);
				g.Indent(20);

				for (int j = 0; j < graph.nodes.Count; j++) {
					Node node = graph.nodes[j];
					g.PushID(j);

					_sb.Clear();
					_sb.Append($"{node.id.ToString()} ({(node.enabled ? "enabled" : "disabled")})");

					if (node.region_object != null)
						_sb.Append($": {node.region_object.Name}");


					if (g.Button(!node.enabled ? "Enable" : "Disable")) {
						if(node.enabled) DisableNode(node); else EnableNode(node);
					}

					g.SameLine();
					g.Text(_sb.ToString());



					g.PopID();
				}

				g.Unindent(20);
				g.PopID();
			}
		}
	}
}
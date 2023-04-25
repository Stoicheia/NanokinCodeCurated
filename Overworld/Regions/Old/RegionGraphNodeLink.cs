using UnityEngine;

namespace Anjin.Regions
{
	/*public class RegionNodeLink : RegionObjectSpatial,
	                                   IRegionGraphObjectSpatial<Vector2>,
	                                   IEqualityComparer<RegionNodeLink>{
		public RegionNode Node1;
		public RegionNode Node2;

		public ParkAIGraphNodeLinkPosition Node1Position;
		public ParkAIGraphNodeLinkPosition Node2Position;

		public Vector2 Node1LinkAreaPos => GetAreaPosFromLinkPos(Node1, Node1Position);
		public Vector2 Node2LinkAreaPos => GetAreaPosFromLinkPos(Node2, Node2Position);

		public RegionShapeLinkArea area;
		public IRegionShapeArea<Vector2> Area => area;

		public bool LinksTo(RegionNode node) => node == Node1 || node == Node2;

		public RegionNode Opposite(RegionNode node)
		{
			if (!Valid) return null;
			if (node == Node1) return Node2;
			if (node == Node2) return Node1;
			return null;
		}

		public ParkAIGraphNodeLinkPosition? GetNodePos(RegionNode node)
		{
			if (node == Node1) return Node1Position;
			if (node == Node2) return Node2Position;
			return null;
		}

		public Vector2 GetAreaPosFromLinkPos(RegionNode node,ParkAIGraphNodeLinkPosition pos)
		{
			var worldPos = pos.GetWorldPosition(node);
			return area.WorldPosToAreaPos(worldPos);
		}

		public float GetLength()
		{
			return Vector3.Distance(Node1Position.GetWorldPosition(Node1), Node2Position.GetWorldPosition(Node2));
		}

		public Vector3 GetDirection()
		{
			return (Node2Position.GetWorldPosition(Node2) - Node1Position.GetWorldPosition(Node1)).normalized;
		}

		public bool Valid => Node1 != null && Node2 != null;

		public RegionNodeLink(RegionGraph parentGraph, RegionNode node1, RegionNode node2) : base(parentGraph)
		{
			//Transform = new GraphObjectTransform();

			Node1 = node1;
			Node2 = node2;

			Node1Position = ParkAIGraphNodeLinkPosition.GetNewNodePosition(node1);
			Node2Position = ParkAIGraphNodeLinkPosition.GetNewNodePosition(node2);

			area = new RegionShapeLinkArea(this);
		}

		public void Destroy()
		{
			Node1.Links.Remove(this);
			Node2.Links.Remove(this);
			Alive = false;
		}

		public GraphObjectTransform GetTransform() => Transform;

		public bool Equals(RegionNodeLink x, RegionNodeLink y)
		{
			if ((object)x == null || (object)y == null) return false;
			return
				( x.Node1 == y.Node1 && x.Node2 == y.Node2 ) ||
				( x.Node1 == y.Node2 && x.Node2 == y.Node1 );
		}

		public static bool operator ==(RegionNodeLink x, RegionNodeLink y)
		{
			return x.Equals(y);
		}

		public static bool operator !=(RegionNodeLink x, RegionNodeLink y)
		{
			return !x.Equals(y);
		}

		public int GetHashCode(RegionNodeLink obj)
		{
			return Node1.GetHashCode() + Node2.GetHashCode();
		}

		//public override ISpatialLinkPosition<T> NewLinkPosition<T>() => null;
	}*/

	public enum CompassDirection
	{
		E = 0, NE = 45, N = 90, NW = 135, W = 180, SW = 225, S = 270, SE = 315
	}

	public static class CompassDirectionExtensions
	{
		public static Vector2 ToVector2(this CompassDirection dir)
		{
			switch(dir)
			{
				case CompassDirection.E: 	return Vector2.right;
				case CompassDirection.NE: 	return Vector2.right + Vector2.up;
				case CompassDirection.N: 	return Vector2.up;
				case CompassDirection.NW: 	return Vector2.left + Vector2.up;
				case CompassDirection.W: 	return Vector2.left;
				case CompassDirection.SW: 	return Vector2.left + Vector2.down;
				case CompassDirection.S: 	return Vector2.down;
				case CompassDirection.SE: 	return Vector2.right + Vector2.down;
				default:
					return Vector2.zero;
			}
		}

		public static Vector2 GetEdgeOffset(this CompassDirection dir, Vector2 rectSize, float pos)
		{
			switch(dir)
			{
				//N or S: Return horizontal offset
				case CompassDirection.N:
				case CompassDirection.S:	return new Vector2(rectSize.x * Mathf.Clamp(pos, -1, 1), 0);

				case CompassDirection.E:
				case CompassDirection.W:	return new Vector2(0, rectSize.y * Mathf.Clamp(pos, -1, 1));

				//Any of the corners return zero
				default: return Vector2.zero;
			}
		}

		/// <summary>
		/// Takes a CompassDirection and a normalized (-1 to 1) 2D position and returns that position snapped to that edge.
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="pos2D"></param>
		/// <returns></returns>
		public static Vector2 SnapVector2ToEdge(this CompassDirection dir, Vector2 pos2D)
		{
			switch(dir)
			{
				case CompassDirection.E:  return new Vector2(1		 , pos2D.y);
				case CompassDirection.N:  return new Vector2(pos2D.x , 		 1);
				case CompassDirection.W:  return new Vector2(-1		 , pos2D.y);
				case CompassDirection.S:  return new Vector2(pos2D.x , 		-1);

				//For corners we don't care
				case CompassDirection.NE: return new Vector2( 1,  1);
				case CompassDirection.NW: return new Vector2(-1,  1);
				case CompassDirection.SW: return new Vector2(-1, -1);
				case CompassDirection.SE: return new Vector2( 1, -1);

				default: return pos2D;
			}
		}

		/// <summary>
		/// Given a 2D position, Returns the edge position of the direciton.
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="pos2D"></param>
		/// <returns></returns>
		public static float Vector2ToEdgePos(this CompassDirection dir, Vector2 pos2D)
		{
			var snapPos = dir.SnapVector2ToEdge(pos2D);

			switch(dir)
			{
				case CompassDirection.E:
				case CompassDirection.W: return Mathf.Clamp( snapPos.y , -1, 1);

				case CompassDirection.N:
				case CompassDirection.S: return Mathf.Clamp( snapPos.x , -1, 1);

				//For corners we don't care
				case CompassDirection.NE:
				case CompassDirection.SE:
				case CompassDirection.NW:
				case CompassDirection.SW: return 0;
			}

			return 0;
		}
	}

	/*public struct ParkAIGraphNodeLinkPosition
	{
		public enum Type
		{
			Empty, RectEdge, RectInside, CircleEdge, CircleInside
		}

		public ParkAIGraphNodeLinkPosition(CompassDirection rectEdge, float rectEdgePos) : this()
		{
			type = Type.RectEdge;
			rect_edge = rectEdge;
			rect_edge_pos = rectEdgePos;
		}

		public ParkAIGraphNodeLinkPosition(Vector2 rectInnerPos) : this()
		{
			type = Type.RectInside;
			rect_inner_pos = rectInnerPos;
		}

		public ParkAIGraphNodeLinkPosition(float circleDir) : this()
		{
			type = Type.CircleEdge;
			circle_dir = circleDir;
		}

		public ParkAIGraphNodeLinkPosition(float circleDir, float circleInnerPos) : this()
		{
			type = Type.CircleInside;
			circle_dir = circleDir;
			circle_inner_pos = circleInnerPos;
		}

		public Type type;

		////////////////////////////////////
		//***********Rect Edge***********#1#/
		////////////////////////////////////

		/// <summary>
		/// Edge: Which edge of a rectangle area the position is on. If this is a corner of the rectangle, the
		/// 	  position is considered snapped to that corner. Otherwise, use rect_edge_pos to find the position.
		/// </summary>
		public CompassDirection rect_edge;
		public float 			rect_edge_pos;

		//Rect Inside
		/// <summary>
		/// Inside: A normalized (-1 to 1) value saying where the link position is inside a rectangle shape.
		/// </summary>
		public Vector2 			rect_inner_pos;

		////////////////////////////////////
		//*************Cirlce************#1#/
		////////////////////////////////////

		/// <summary>
		/// Edge: 	Where the position is on the circle's edge in Degrees.
		/// Inside:	The direction the position is from the circle's center.
		/// </summary>
		public float circle_dir;
		/// <summary>
		/// Inside: A normalized value (0 to 1) of how far away the position is from the center to the edge of the circle.
		/// </summary>
		[RangeSlider(0,1)]
		public float circle_inner_pos;

		public Vector3 GetWorldPosition(RegionNode node)
		{
			var v = node.area.AreaPosToWorldPos(GetGraphPosition(node));
			return v;
		}

		public Vector2 GetGraphPosition(RegionNode node)
		{
			if (node == null || node.area == null) return Vector3.zero;

			Vector2 nodePos = Vector2.zero;
			Vector2 pos;

			switch(type)
			{
				case Type.RectEdge: nodePos = (rect_edge.ToVector2())
				                              + rect_edge.GetEdgeOffset(Vector2.one, rect_edge_pos); break;

				case Type.RectInside: nodePos = rect_inner_pos; break;

				case Type.CircleEdge: pos = new Vector2(Mathf.Cos(circle_dir * Mathf.Deg2Rad), Mathf.Sin(circle_dir * Mathf.Deg2Rad));
					nodePos               = ( pos );
					break;

				case Type.CircleInside: pos = new Vector2(Mathf.Cos(circle_dir * Mathf.Deg2Rad), Mathf.Sin(circle_dir * Mathf.Deg2Rad));
					nodePos                 = ( pos * circle_inner_pos ); break;

			}

			return nodePos;
		}


		/// <summary>
		/// Sets the position based on a normalized vector2 with origin at node's center.
		/// </summary>
		public void SetPosition(Vector2 pos2D, RegionNode node, bool checkTypeChange = true, float edgeSnapThreshold = 0.08f)
		{
			if(checkTypeChange) CheckTypeChangeCondition(pos2D, node, edgeSnapThreshold);

			switch(type)
			{
				case Type.RectEdge:
					rect_edge_pos = rect_edge.Vector2ToEdgePos(pos2D);
					break;

				case Type.RectInside:
					rect_inner_pos = new Vector2(Mathf.Clamp( pos2D.x , -1,1), Mathf.Clamp( pos2D.y , -1, 1));
					break;

				case Type.CircleEdge:
					circle_dir = 90 - MathUtil.Angle(pos2D.normalized);
					/*pos     = new Vector2(Mathf.Cos(circle_dir * Mathf.Deg2Rad), Mathf.Sin(circle_dir * Mathf.Deg2Rad));
					nodePos = ( pos * node.area.CircleRadius );#1#

					break;

				case Type.CircleInside:
					circle_dir = 90 - MathUtil.Angle(pos2D.normalized);
					circle_inner_pos = Mathf.Clamp(pos2D.magnitude, 0, 1);


					break;
			}
		}

		public void CheckTypeChangeCondition(Vector2 pos2D, RegionNode node, float edgeSnapThreshold = 0.1f)
		{
			if (node == null || node.area == null || node.area.Type == RegionShapeArea2D.AreaType.Empty)
			{
				type = Type.Empty;
				return;
			}

			switch(node.area.Type)
			{
				//If we're a rect and we're close to the edge, we should snap to the edge. Otherwise we need to be an inside point.
				case RegionShapeArea2D.AreaType.Rect:

					var rsize = node.area.RectSize;

					bool xedge = Mathf.Abs(pos2D.x) >= 1 - edgeSnapThreshold / rsize.x;
					bool yedge = Mathf.Abs(pos2D.y) >= 1 - edgeSnapThreshold / rsize.y;
					bool corner = xedge && yedge;

					if (corner)
					{
						//snap to the nearest corner
						type = Type.RectEdge;

						//Left Side
						if (pos2D.x < 0)
						{
							//Bottom
							if (pos2D.y < 0) rect_edge = CompassDirection.SW;
							if (pos2D.y > 0) rect_edge = CompassDirection.NW;
						}
						//Right Side
						else if (pos2D.x > 0)
						{
							if (pos2D.y < 0) rect_edge = CompassDirection.SE;
							if (pos2D.y > 0) rect_edge = CompassDirection.NE;
						}
					}
					else if (xedge || yedge)
					{
						//snap to nearest edge
						type = Type.RectEdge;

						if (!xedge)
						{
							if (pos2D.y < 0) rect_edge = CompassDirection.S;
							if (pos2D.y > 0) rect_edge = CompassDirection.N;
						}
						else if (!yedge)
						{
							if (pos2D.x < 0) rect_edge = CompassDirection.W;
							if (pos2D.x > 0) rect_edge = CompassDirection.E;
						}
					}
					else
					{
						//Make inner
						type = Type.RectInside;
					}

					break;

				case RegionShapeArea2D.AreaType.Circle:

					var onEdge = pos2D.magnitude >= 1-edgeSnapThreshold / node.area.CircleRadius;

					//Debug.Log(pos2D.magnitude);

					if (onEdge) type  = Type.CircleEdge;
					else 		type  = Type.CircleInside;

					break;


				/*case ParkAIGraphArea.AreaType.Polygon:
					break;#1#
			}
		}

		public static ParkAIGraphNodeLinkPosition GetNewNodePosition(RegionNode node)
		{
			switch(node.area.Type)
			{
				case RegionShapeArea2D.AreaType.Rect: 	return new ParkAIGraphNodeLinkPosition(new Vector2(0,0));
				case RegionShapeArea2D.AreaType.Circle: 	return new ParkAIGraphNodeLinkPosition(0f, 0f);

				default:							 	return new ParkAIGraphNodeLinkPosition {type = Type.Empty};
			}
		}
	}*/
}
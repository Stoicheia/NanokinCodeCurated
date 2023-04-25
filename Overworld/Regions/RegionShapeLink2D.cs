using UnityEngine;
using Util;

namespace Anjin.Regions
{
	public class RegionShape2DLink : RegionSpatialLink<RegionShape2D, ShapeLink2DPosition>
	{
		public enum LinkAreaType { Line, Plane }
		public LinkAreaType Type;

		public float   PlaneWidth;
		public Vector2 PlaneNormal;

		public override bool IsPointOverlapping(Vector3 point)
		{
			return false;
		}

		public Vector2 AreaPosToNormalizedPos(Vector2 areaPos)
		{
			var dist = GetLength();

			switch(Type)
			{
				case LinkAreaType.Line:  return new Vector2(areaPos.x / dist, 0);
				case LinkAreaType.Plane: return new Vector2(areaPos.x / dist, areaPos.y / PlaneWidth);
			}

			return Vector2.zero;
		}

		public Vector2 GetAreaPositionOnFirstShape()  => GetAreaPosFromLinkPos(First,  FirstTransform);
		public Vector2 GetAreaPositionOnSecondShape() => GetAreaPosFromLinkPos(Second, SecondTransform);

		public Vector2 GetAreaPosFromLinkPos(RegionShape2D shape, ShapeLink2DPosition pos)
		{
			var worldPos = pos.GetWorldPosition(shape);
			return WorldPosToAreaPos(worldPos);
		}

		public Vector2 NormalizedPosToAreaPos(Vector2 normalizedPos)
		{
			var dist = GetLength();

			switch(Type)
			{
				case LinkAreaType.Line:  return new Vector2(normalizedPos.x * dist, 0);
				case LinkAreaType.Plane: return new Vector2(normalizedPos.x * dist, normalizedPos.y * PlaneWidth);
			}

			return Vector2.zero;
		}

		public Vector3 AreaPosToWorldPos(Vector2 areaPos)
		{
			var length = GetLength();
			var dir    = GetDirection();

			var origin = FirstTransform.GetWorldPosition(First);
			Vector3 areaPos3D = new Vector3(areaPos.x *length, 0, areaPos.y);
			Vector3 final = areaPos3D + origin;

			return final;
		}

		public Vector2 WorldPosToAreaPos(Vector3 worldPos)
		{
			var length = GetLength();

			var correctedPos = worldPos - FirstTransform.GetWorldPosition(First);
			var normal       = GetDirection();

			normal = ( Matrix4x4.Rotate(Quaternion.Euler(normal)).inverse.MultiplyVector(
							 Type == LinkAreaType.Line ?
								 Vector3.up :
								 new Vector3(PlaneNormal.x, PlaneNormal.y, 0).normalized
						 ));

			var planeVec = Vector3.ProjectOnPlane(correctedPos,  normal);

			if (Type == LinkAreaType.Line)  return new Vector2(planeVec.x /length, planeVec.z );
			if (Type == LinkAreaType.Plane) return new Vector2(planeVec.x /length, planeVec.z / PlaneWidth);

			else return Vector2.zero;
		}

		public Vector2 GetRandomAreaPointInside()
		{
			return Vector2.zero;
		}

		public Vector3 GetRandomWorldPointInside()
		{
			return Vector2.zero;
		}

		public override Vector3 GetFocusPos()
		{
			var pos1 = FirstTransform.GetWorldPosition(First);
			var pos2 = SecondTransform.GetWorldPosition(Second);
			return Vector3.Lerp(pos1, pos2, 0.5f);
		}

		public RegionShape2DLink(RegionShape2D first,       RegionShape2D second) : base(first, second) { }
		public RegionShape2DLink(RegionGraph   parentGraph, RegionShape2D first, RegionShape2D second) : base(parentGraph, first, second) { }
	}

	public struct ShapeLink2DPosition : ISpatialLinkPosition<RegionShape2D>
	{
		public enum Type {
			Empty,
			RectEdge, RectInside,
			CircleEdge, CircleInside,
			PolyEdge, PolyInside,
		}

		public ShapeLink2DPosition(CompassDirection rectEdge, float rectEdgePos) : this()
		{
			type          = Type.RectEdge;
			rect_edge     = rectEdge;
			rect_edge_pos = rectEdgePos;
		}

		public ShapeLink2DPosition(Vector2 rectInnerPos) : this()
		{
			type           = Type.RectInside;
			rect_inner_pos = rectInnerPos;
		}

		public ShapeLink2DPosition(float circleDir) : this()
		{
			type       = Type.CircleEdge;
			circle_dir = circleDir;
		}

		public ShapeLink2DPosition(float circleDir, float circleInnerPos) : this()
		{
			type             = Type.CircleInside;
			circle_dir       = circleDir;
			circle_inner_pos = circleInnerPos;
		}

		public ShapeLink2DPosition(int polyEdgeInd, float polyEdgePos) : this()
		{
			type = Type.PolyEdge;
			poly_edge_ind 	= polyEdgeInd;
			polyEdgePos 	= polyEdgePos;
		}

		public ShapeLink2DPosition(int polyTriInd, Barycentric polyTriPos) : this()
		{
			type         = Type.PolyInside;
			poly_tri_ind = polyTriInd;
			poly_tri_pos = polyTriPos;
		}

		public Type type;

		////////////////////////////////////
		//***********Rect Edge************//
		////////////////////////////////////

		/// <summary>
		/// Edge: Which edge of a rectangle area the position is on. If this is a corner of the rectangle, the
		/// 	  position is considered snapped to that corner. Otherwise, use rect_edge_pos to find the position.
		/// </summary>
		public CompassDirection rect_edge;
		public float rect_edge_pos;

		//Rect Inside
		/// <summary>
		/// Inside: A normalized (-1 to 1) value saying where the link position is inside a rectangle shape.
		/// </summary>
		public Vector2 rect_inner_pos;

		////////////////////////////////////
		//*************Circle*************//
		////////////////////////////////////

		/// <summary>
		/// Edge: 	Where the position is on the circle's edge in Degrees.
		/// Inside:	The direction the position is from the circle's center.
		/// </summary>
		public float circle_dir;
		/// <summary>
		/// Inside: A normalized value (0 to 1) of how far away the position is from the center to the edge of the circle.
		/// </summary>
		[Range(0,1)]
		public float circle_inner_pos;

		////////////////////////////////////
		//*************Polygon************//
		////////////////////////////////////
		public int 	 poly_edge_ind;
		public float poly_edge_pos;

		/// <summary>
		/// An index of a triangle that the point exists on.
		/// </summary>
		public int 		poly_tri_ind;

		/// <summary>
		/// Barycentric coordinates on the triangle.
		/// </summary>
		public Barycentric poly_tri_pos;

		public Vector3 GetWorldPosition(RegionShape2D shape)
		{
			return shape.AreaPosToWorldPos(GetGraphPosition(shape));
		}

		public Vector3 GetWorldPosition(RegionShape2D shape, Vector2 offset)
		{
			return shape.AreaPosToWorldPos(GetGraphPosition(shape, offset));
		}

		public Vector2 GetGraphPosition(RegionShape2D shape) => GetGraphPosition(shape, Vector2.zero);
		public Vector2 GetGraphPosition(RegionShape2D node, Vector2 offset)
		{
			if (node == null) return Vector3.zero;

			Vector2 nodePos = offset;
			Vector2 pos;

			if(node.Type == RegionShape2D.ShapeType.Empty && type != Type.Empty) {
				type = Type.Empty;
			}

			switch(type)
			{
				case Type.RectEdge: {
					nodePos += rect_edge.ToVector2()
					           + rect_edge.GetEdgeOffset(Vector2.one, rect_edge_pos);
				} break;

				case Type.RectInside: {
					nodePos += rect_inner_pos;
				} break;

				case Type.CircleEdge: {
					pos     =  new Vector2(Mathf.Cos(circle_dir * Mathf.Deg2Rad), Mathf.Sin(circle_dir * Mathf.Deg2Rad));
					nodePos += pos;
				} break;

				case Type.CircleInside: {
					pos     =  new Vector2(Mathf.Cos(circle_dir * Mathf.Deg2Rad), Mathf.Sin(circle_dir * Mathf.Deg2Rad));
					nodePos += ( pos * circle_inner_pos );
				} break;

				case Type.PolyEdge:{

					if(node.GetPolyEdge(poly_edge_ind, out var p1, out var p2)) {
						pos     =  Vector2.Lerp(p1, p2, poly_edge_pos);
						nodePos += pos;
					}

				} break;

				case Type.PolyInside: {
					if(node.PolygonPoints.Count > 2 && node.TriangleExists(poly_tri_ind)) {
						if(node.GetTriangleAreaPoints(poly_tri_ind, out var p1, out var p2, out var p3)) {
							pos     =  poly_tri_pos.Interpolate(p1, p2, p3);
							nodePos += pos;
						}
					}
				} break;
			}

			return nodePos;
		}


		/// <summary>
		/// Sets the position based on an area position.
		/// </summary>
		public void SetPosition(Vector2 pos2D, RegionShape2D node, bool checkTypeChange = true, float edgeSnapThreshold = 0.08f)
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
					nodePos = ( pos * node.area.CircleRadius );*/

					break;

				case Type.CircleInside:
					circle_dir       = 90 - MathUtil.Angle(pos2D.normalized);
					circle_inner_pos = Mathf.Clamp(pos2D.magnitude, 0, 1);
					break;

				case Type.PolyEdge: {
					//Find closest edge
					var (ind, dist, pos) = node.GetClosestEdge(pos2D);

					poly_edge_ind = ind;
					poly_edge_pos = pos;

				} break;

				case Type.PolyInside:
					if (node.GetTriangleIndex(pos2D, out var tri)) {
						poly_tri_ind = tri;
						if(node.GetTriangleAreaPoints(tri, out var p1, out var p2, out var p3)) {
							poly_tri_pos = new Barycentric(p1, p2, p3, pos2D);
						}
					}
					break;
			}
		}

		public void CheckTypeChangeCondition(Vector2 pos2D, RegionShape2D node, float edgeSnapThreshold = 0.1f)
		{
			if (node == null || node.Type == RegionShape2D.ShapeType.Empty)
			{
				type = Type.Empty;
				return;
			}

			switch(node.Type)
			{
				//If we're a rect and we're close to the edge, we should snap to the edge. Otherwise we need to be an inside point.
				case RegionShape2D.ShapeType.Rect:

					var rsize = node.RectSize;

					bool xedge  = Mathf.Abs(pos2D.x) >= 1 - edgeSnapThreshold / rsize.x;
					bool yedge  = Mathf.Abs(pos2D.y) >= 1 - edgeSnapThreshold / rsize.y;
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

				case RegionShape2D.ShapeType.Circle:

					var onEdge = pos2D.magnitude >= 1 -edgeSnapThreshold / node.CircleRadius;

					//Debug.Log(pos2D.magnitude);

					if (onEdge) type = Type.CircleEdge;
					else 		type = Type.CircleInside;

					break;


				case RegionShape2D.ShapeType.Polygon:

					var (ind, dist, pos) = node.GetClosestEdge(pos2D);

					if (dist < 0.25f || !node.PolygonContainsPoint(pos2D))
						type = Type.PolyEdge;
					else
						type = Type.PolyInside;

					break;
			}
		}

		public static ShapeLink2DPosition GetNewNodePosition(RegionShape2D shape)
		{
			switch(shape.Type)
			{
				case RegionShape2D.ShapeType.Rect:   return new ShapeLink2DPosition(new Vector2(0,0));
				case RegionShape2D.ShapeType.Circle: return new ShapeLink2DPosition(0f, 0f);
				case RegionShape2D.ShapeType.Polygon: return new ShapeLink2DPosition(0, 0f);

				default: return new ShapeLink2DPosition {type = Type.Empty};
			}
		}
	}

}
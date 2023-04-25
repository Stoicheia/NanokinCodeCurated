using System;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Util.Procedural;
using Vexe.Runtime.Extensions;
using Random = UnityEngine.Random;

namespace Anjin.Regions
{
	/// <summary>
	/// A 2D shape (moveable in 3D space).
	/// </summary>
	public class RegionShape2D : RegionObjectSpatial, ILinkable<RegionShape2D, ISpatialLinkPosition<RegionShape2D>>
	{
		public enum ShapeType { Empty, Rect, Circle, Polygon, /*Spline,*/ }
		public ShapeType Type;

		public Vector2       RectSize     = Vector2.one;
		public float         CircleRadius = 1;

		public List<Vector2> 	PolygonPoints;
		public int[] 			PolygonTriangulation;
		public float[] 			PolygonTriangulationWeights;

		public RegionShape2D() 															: this(ShapeType.Empty) { }
		public RegionShape2D(ShapeType type = ShapeType.Empty) 							: this(null, type) 		{ }
		public RegionShape2D(RegionGraph parentGraph, ShapeType type = ShapeType.Empty) : base(parentGraph)
		{
			PolygonPoints = new List<Vector2>();
			Type = type;
		}

		public ISpatialLinkPosition<RegionShape2D> NewLinkPosition()
		{
			ShapeLink2DPosition pos;

			switch(Type)
			{
				case ShapeType.Rect:   pos = new ShapeLink2DPosition(new Vector2(0, 0)); break;
				case ShapeType.Circle: pos = new ShapeLink2DPosition(0f, 0f); break;

				default: pos = new ShapeLink2DPosition{type = ShapeLink2DPosition.Type.Empty}; break;
			}
			return pos;
		}

		public RegionSpatialLinkBase LinkTo(ILinkableBase other)
		{
			if(other is RegionShape2D shape)
			{
				RegionShape2DLink link = new RegionShape2DLink(ParentGraph, this, shape);

				return link;
			}

			return null;
		}

		public void InsertPolygonPoint(int index, Vector2 vert)
		{
			if(index >= 0 && index <= PolygonPoints.Count) {
				PolygonPoints.Insert(index, vert);
				TriangulatePolygon();
			}
		}

		public void TriangulatePolygon()
		{
			if (PolygonPoints == null || PolygonPoints.Count <= 3) return;

			// TODO: Remove allocation?
			var tris = new List<int>();
			Triangulator.Triangulate(PolygonPoints, tris);
			PolygonTriangulation = tris.ToArray();

			PolygonTriangulationWeights = new float[PolygonTriangulation.Length / 3];

			float totalArea = Mathf.Abs(Triangulator.Area(PolygonPoints));

			for (int i = 0; i < PolygonTriangulation.Length; i += 3 ) {
				var v1 = PolygonPoints[ PolygonTriangulation[i]];
				var v2 = PolygonPoints[ PolygonTriangulation[i + 1]];
				var v3 = PolygonPoints[ PolygonTriangulation[i + 2]];

				float triArea = Triangulator.TriangleArea(v1, v2, v3);
				PolygonTriangulationWeights[i / 3] = triArea / totalArea;
			}
		}

		public bool TrianulationValid() => PolygonTriangulation != null && PolygonTriangulationWeights != null;

		public bool GetTriangleIndex(Vector2 areaPos, out int index) {
			var r = GetTrianglePointIndex(areaPos, out var i);
			index = i / 3;
			return r;
		}

		public bool GetTrianglePointIndex(Vector2 areaPos, out int index)
		{
			index = -1;
			if (TrianulationValid()) {
				float sign (Vector2 p1, Vector2 p2, Vector2 p3) => ( p1.x - p3.x ) * ( p2.y - p3.y ) - ( p2.x - p3.x ) * ( p1.y - p3.y );
				for (int i = 0; i < PolygonTriangulation.Length; i += 3 ) {

					var v1 = PolygonPoints[ PolygonTriangulation[i]];
					var v2 = PolygonPoints[ PolygonTriangulation[i + 1]];
					var v3 = PolygonPoints[ PolygonTriangulation[i + 2]];


					var d1 = sign(areaPos, v1, v2);
					var d2 = sign(areaPos, v2, v3);
					var d3 = sign(areaPos, v3, v1);

					bool  has_neg, has_pos;

					has_neg = ( d1 < 0 ) || ( d2 < 0 ) || ( d3 < 0 );
					has_pos = ( d1 > 0 ) || ( d2 > 0 ) || ( d3 > 0 );

					if (!( has_neg && has_pos )) {
						index = i;
						return true;
					}
				}
			}

			return false;
		}

		public bool TriangleExists(int ind) => PolygonTriangulation.Length > ind * 3;

		public bool GetTriangleAreaPoints(int ind, out Vector2 p1, out Vector2 p2, out Vector2 p3)
		{
			if (!TriangleExists(ind)) {
				p1 = Vector2.zero;
				p2 = Vector2.zero;
				p3 = Vector2.zero;
				return false;
			}

			/*var tri = PolygonTriangulation[ind * 3];
			p1 = PolygonPoints[tri];
			p2 = PolygonPoints[tri + 1];
			p3 = PolygonPoints[tri + 2];*/

			var t1 = PolygonTriangulation[ind * 3];
			var t2 = PolygonTriangulation[ind * 3 + 1];
			var t3 = PolygonTriangulation[ind * 3 + 2];
			p1 = PolygonPoints[t1];
			p2 = PolygonPoints[t2];
			p3 = PolygonPoints[t3];
			return true;
		}

		public bool GetPolyEdge(int ind, out Vector2 p1, out Vector2 p2)
		{
			if (ind < 0) {
				p1 = Vector2.zero;
				p2 = Vector2.zero;
				return false;
			}
			else if (ind == PolygonPoints.Count-1) {
				p1 = PolygonPoints[ind];
				p2 = PolygonPoints[0];
				return true;
			}

			p1 = PolygonPoints[ind];
			p2 = PolygonPoints[ind + 1];
			return true;
		}

		public bool GetPointOnPolyEdge(int ind, float lerp, out Vector2 point)
		{
			if (GetPolyEdge(ind, out var p1, out var p2)) {
				point = Vector2.Lerp(p1, p2, lerp);
				return true;
			}

			point = Vector2.zero;
			return false;
		}

		public float GetArea()
		{
			switch (Type) {
				case ShapeType.Rect:	return RectSize.x * RectSize.y;

				case ShapeType.Circle: 	return Mathf.PI * CircleRadius * CircleRadius;

				case ShapeType.Polygon:
					if (!TrianulationValid()) return 0;

					float total = 0;
					for (int i = 0; i < PolygonTriangulation.Length; i += 3) {
						Vector2 p1 = PolygonPoints[PolygonTriangulation[i]];
						Vector2 p2 = PolygonPoints[PolygonTriangulation[i + 1]];
						Vector2 p3 = PolygonPoints[PolygonTriangulation[i + 2]];

						Vector3 wp1 = new Vector3(p1.x, 0, p1.y);
						Vector3 wp2 = new Vector3(p2.x, 0, p2.y);
						Vector3 wp3 = new Vector3(p3.x, 0, p3.y);

						float a = Vector3.Distance(wp1, wp2);
						float b = Vector3.Distance(wp2, wp3);
						float c = Vector3.Distance(wp3, wp1);

						float p = (a + b + c) / 2;

						total += Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));
					}

					return total;
			}

			return 0;
		}

		public (int ind, float dist, float pos) GetClosestEdge(Vector2 point)
		{
			int ind = -1;
			float smallest_dist = float.PositiveInfinity;
			float pos = 0;

			for (int i = 0; i < PolygonPoints.Count; i++) {
				if (GetPolyEdge(i, out var p1, out var p2)) {
					float dist;
					float t = 0;

					Vector2 AB = p2 - p1;

					if (AB.magnitude.Sqr() == 0.0f)
						dist = Vector2.Distance(point, p1);
					else
					{
						t    = Mathf.Clamp(Vector2.Dot(point - p1, AB) / (AB.sqrMagnitude), 0, 1);
						Vector2 proj = p1 + t * ( p2 - p1 );
						dist = Vector2.Distance(point, proj);
					}

					if (dist < smallest_dist) {
						smallest_dist = dist;
						ind = i;
						pos = t;
					}
				}
			}

			return ( ind, smallest_dist, pos);
		}

		public bool PolygonContainsPoint(Vector2 p)
		{
			var j      = PolygonPoints.Count - 1;
			var inside = false;

			for (int i = 0; i < PolygonPoints.Count; j = i++)
			{
				var pi = PolygonPoints[i];
				var pj = PolygonPoints[j];

				if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
				    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
					inside = !inside;
			}

			return inside;
		}

		//TODO: Implement this in some way.
		public override bool IsPointOverlapping(Vector3 point)
		{
			return false;
		}

		#region 3D position helpers

		public Vector3 AreaPosToWorldPos(Vector2 areaPos)
		{
			Vector2 sizedPos;

			switch(Type)
			{
				case ShapeType.Empty:  sizedPos = areaPos; break;
				case ShapeType.Rect:   sizedPos = areaPos * RectSize; break;
				case ShapeType.Circle: sizedPos = areaPos * CircleRadius;break;

				case ShapeType.Polygon:
					sizedPos = areaPos;
					break;

				default: return Vector3.zero;
			}

			var areaPosToWorldPos = Transform.Position + (Vector3)( Matrix4x4.TRS(Vector3.zero, Transform.Rotation, Transform.Scale) * new Vector3(sizedPos.x, 0, sizedPos.y) );
			return areaPosToWorldPos;
		}

		public Vector2 WorldPosToAreaPos(Vector3 worldPos)
		{
			var correctedPos = worldPos - Transform.Position;
			var projectedVec = Vector3.ProjectOnPlane(correctedPos, Transform.Rotation.normalized * Vector3.up);
			var rot          = Transform.Rotation.eulerAngles;

			//Take the rotation out
			var rotationCorrection = Matrix4x4.Rotate(Quaternion.Euler(rot)).inverse.MultiplyVector(projectedVec);
			rotationCorrection.Scale(Transform.Scale);

			//Debug.Log(worldPos + " " + correctedPos + " " + rotationCorrection);

			var v = new Vector2(rotationCorrection.x, rotationCorrection.z);

			switch(Type)
			{
				case ShapeType.Empty:  v = Vector2.zero; break;
				case ShapeType.Rect:   v = v / RectSize; break;
				case ShapeType.Circle: v = v / CircleRadius; break;
			}

			return v;
		}

		public Vector2 AreaPosToNormalizedPos(Vector2 areaPos)
		{
			switch(Type)
			{
				case ShapeType.Rect: return new Vector2(areaPos.x / ( RectSize.x * Transform.Scale.x.Sqr() ),
					areaPos.y                                    / ( RectSize.y * Transform.Scale.z.Sqr() ));

				case ShapeType.Circle: return new Vector2(areaPos.x / ( CircleRadius * Transform.Scale.x.Sqr() ),
					areaPos.y                                      / ( CircleRadius * Transform.Scale.z.Sqr() ));
				//case ShapeType.Polygon: 	break;
			}

			return Vector2.zero;
		}

		public Vector2 NormalizedPosToAreaPos(Vector2 normalizedPos)
		{
			switch(Type)
			{
				case ShapeType.Rect: return new Vector2(normalizedPos.x * ( RectSize.x * Transform.Scale.x.Sqr() ),
					normalizedPos.y                                    * ( RectSize.y * Transform.Scale.z.Sqr() ));

				case ShapeType.Circle: return new Vector2(normalizedPos.x * ( CircleRadius * Transform.Scale.x.Sqr() ),
					normalizedPos.y                                      * ( CircleRadius * Transform.Scale.z.Sqr() ));
				//case ShapeType.Polygon: 	break;
			}

			return Vector2.zero;
		}

		public Vector2 GetRandomAreaPointInside(int? seed = null)
		{
			if(seed.HasValue) Random.InitState(seed.Value);

			Vector2 randPoint = Vector2.zero;
			switch (Type)
			{
				case ShapeType.Rect:
					randPoint = new Vector2( Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
					break;
				case ShapeType.Circle:
					var p = Random.insideUnitCircle;
					randPoint = new Vector2(p.x, p.y);
					break;
				case ShapeType.Polygon:
					if (TrianulationValid()) {
						float r = Random.value;

						int i;
						for (i = 0; i < PolygonTriangulationWeights.Length-1; i += 1 ) {
							r -= PolygonTriangulationWeights[i];
							if (r <= 0) break;
						}

						float r1 = Random.value;
						float r2 = Random.value;
						i *= 3;

						var A = PolygonPoints[ PolygonTriangulation[i]];
						var B = PolygonPoints[ PolygonTriangulation[i + 1]];
						var C = PolygonPoints[ PolygonTriangulation[i + 2]];

						randPoint = ( ( 1 - Mathf.Sqrt(r1) 			) * A ) +
									( ( Mathf.Sqrt(r1) * ( 1 - r2 ) ) * B ) +
									( ( Mathf.Sqrt(r1) * r2 		) * C );
					}
					break;
			}
			return randPoint;
		}

		public Vector3 GetRandomWorldPointInside(int? seed = null) => AreaPosToWorldPos(GetRandomAreaPointInside(seed));

		#endregion

		public Rect GetPolyBounds()
		{
			Rect bounds = Rect.zero;

			for (int i = 0; i < PolygonPoints.Count; i++) {
				Vector2 pt = PolygonPoints[i];
				if (pt.x > 0) {
					if (pt.x > bounds.xMax) bounds.xMax = pt.x;
				} else {
					if (pt.x < bounds.xMin) bounds.xMin = pt.x;
				}

				if (pt.y > 0) {
					if (pt.y > bounds.yMax) bounds.yMax = pt.y;
				} else {
					if (pt.y < bounds.yMin) bounds.yMin = pt.y;
				}
			}

			return bounds;
		}
	}

	public class RegionShape3D : RegionObjectSpatial
	{
		public enum ShapeType { Empty, Box, Cylinder, Sphere, Polygon, /*Spline,*/ }
		public ShapeType Type;

		public Vector3 BoxSize 		= Vector3.one;
		public float CylinderRadius = 1;
		public float SphereRadius 	= 1;

		public List<Vector3> PolygonPoints;

		public RegionShape3D(ShapeType type = ShapeType.Empty) : this(null, type) { }
		public RegionShape3D(RegionGraph parentGraph, ShapeType type = ShapeType.Empty) : base(parentGraph)
		{
			PolygonPoints = new List<Vector3>();
			Type = type;
		}

		public override bool IsPointOverlapping(Vector3 point)
		{
			Vector3 inversePoint = Transform.matrix.inverse.MultiplyPoint(point);

			switch (Type)
			{
				case ShapeType.Empty: return false;	//Empty can't overlap?

				case ShapeType.Box:{
					//Do bounds check
					Bounds b = new Bounds(Vector3.zero, BoxSize * 2);
					return b.Contains(inversePoint);
				}

				case ShapeType.Cylinder:{
				} break;

				case ShapeType.Sphere:{
					return (Vector3.Distance(Vector3.zero, inversePoint) <= SphereRadius);
				}

				case ShapeType.Polygon:		break;
			}

			return false;
		}

		//public override ISpatialLinkPosition<T> NewLinkPosition<T>() => null;
	}

	public class RegionShapeArea2D : IRegionShapeArea<Vector2>
	{
		public enum AreaType { Empty, Rect, Circle, Polygon, /*Spline,*/ }
		public AreaType Type;

		/*public string id    { get; }
		public bool   Alive { get; private set; }*/

		public IRegionGraphObjectSpatial<Vector2> parent;

		public Vector2       RectSize     = Vector2.one;
		public float         CircleRadius = 1;
		public List<Vector2> PolygonPoints;


		public RegionShapeArea2D(IRegionGraphObjectSpatial<Vector2> _parent, AreaType _type = AreaType.Empty)
		{
			Type = _type;
			/*id = DataUtil.MakeShortID(4);
			Alive = true;*/
			parent = _parent;

			PolygonPoints = new List<Vector2>();
		}

		public GraphObjectTransform GetTransform() => parent.GetTransform();
		private GraphObjectTransform Transform => GetTransform();

		/*public void Destroy()
		{
			Alive = false;
		}*/

		public Vector3 AreaPosToWorldPos(Vector2 areaPos)
		{
			Vector2 sizedPos;

			switch(Type)
			{
				case AreaType.Rect: 	sizedPos = areaPos * RectSize; break;
				case AreaType.Circle: 	sizedPos = areaPos * CircleRadius;break;
				//case AreaType.Polygon: 	break;
				default: 				return Vector3.zero;
			}

			var areaPosToWorldPos = Transform.Position + (Vector3)( Matrix4x4.TRS(Vector3.zero, Transform.Rotation, Transform.Scale) * new Vector3(sizedPos.x, 0, sizedPos.y) );
			return areaPosToWorldPos;
		}

		/*public bool AreaPosInNode()
		{
			return true;
		}*/

		public Vector2 WorldPosToAreaPos(Vector3 worldPos)
		{
			var correctedPos = worldPos - Transform.Position;
			var projectedVec = Vector3.ProjectOnPlane(correctedPos, Transform.Rotation.normalized * Vector3.up);
			var rot = Transform.Rotation.eulerAngles;

			//Take the rotation out
			var rotationCorrection = Matrix4x4.Rotate(Quaternion.Euler(rot)).inverse.MultiplyVector(projectedVec);
			rotationCorrection.Scale(Transform.Scale);

			//Debug.Log(worldPos + " " + correctedPos + " " + rotationCorrection);

			var v = new Vector2(rotationCorrection.x, rotationCorrection.z);

			switch(Type)
			{
				case AreaType.Empty: 	v = Vector2.zero; break;
				case AreaType.Rect: 	v = v / RectSize; break;
				case AreaType.Circle: 	v = v / CircleRadius; break;
			}

			return v;
		}

		public Vector2 AreaPosToNormalizedPos(Vector2 areaPos)
		{
			switch(Type)
			{
				case AreaType.Rect:		return new Vector2(areaPos.x / ( RectSize.x * Transform.Scale.x.Sqr() ),
														   areaPos.y / ( RectSize.y * Transform.Scale.z.Sqr() ));

				case AreaType.Circle: 	return new Vector2(areaPos.x / ( CircleRadius * Transform.Scale.x.Sqr() ),
														   areaPos.y / ( CircleRadius * Transform.Scale.z.Sqr() ));
				//case AreaType.Polygon: 	break;
			}

			return Vector2.zero;
		}

		public Vector2 NormalizedPosToAreaPos(Vector2 normalizedPos)
		{
			switch(Type)
			{
				case AreaType.Rect: return new Vector2(normalizedPos.x * ( RectSize.x * Transform.Scale.x.Sqr() ),
					normalizedPos.y                                    * ( RectSize.y * Transform.Scale.z.Sqr() ));

				case AreaType.Circle: return new Vector2(normalizedPos.x * ( CircleRadius * Transform.Scale.x.Sqr() ),
					normalizedPos.y                                      * ( CircleRadius * Transform.Scale.z.Sqr() ));
				//case AreaType.Polygon: 	break;
			}

			return Vector2.zero;
		}

		public Vector2 GetRandomAreaPointInside()
		{
			Vector2 randPoint = Vector2.zero;
			switch (Type)
			{
				case AreaType.Rect:
					randPoint = new Vector2( Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
					break;
				case AreaType.Circle:
					var p = Random.insideUnitCircle;
					randPoint = new Vector2(p.x, p.y);
					break;
			}
			return randPoint;
		}

		public Vector3 GetRandomWorldPointInside()
		{
			return AreaPosToWorldPos(GetRandomAreaPointInside());
		}

		/*public struct SplinePoint
		{
			public Vector3 Transform.Position;
			public
		}*/
	}


}
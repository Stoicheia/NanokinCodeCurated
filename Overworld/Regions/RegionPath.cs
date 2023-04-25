using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;

namespace Anjin.Regions
{
	public struct PathVert
	{
		public Vector3 point;
	}

	public class RegionPath : RegionObjectSpatial
	{
		public List<PathVert> Points;
		public bool           Closed;

		public RegionPath() : this(false) { }
		public RegionPath(bool closed) : this(null, closed) { }

		public RegionPath(RegionGraph parentGraph, bool closed) : base(parentGraph)
		{
			Points = new List<PathVert>();
			Closed = closed;
		}

		public void InsertPoint(int index, Vector3 vert)
		{
			if(index >= 0 && (index <= (Closed ? Points.Count:Points.Count - 1)))
				Points.Insert(index, new PathVert {point = vert});
		}

		public void SetWorldPoint(int ind, Vector3 point) {
			Points[ind] = new PathVert { point = point - Transform.Position };
		}

		public Vector3 GetWorldPoint(int ind) => Transform.Position + Points.WrapGet(ind).point;

		public override bool IsPointOverlapping(Vector3 point) => false;

		public void AddWorldPoint(Vector3 newPos)
		{
			Points.Add(new PathVert {point = newPos - Transform.Position});
		}

		public override Vector3 GetFocusPos()
		{
			Vector3 sum = Vector3.zero;
			if (Points.Count == 0) return Transform.Position;

			for (int i = 0; i < Points.Count; i++) {
				sum += Points[i].point;
			}

			return sum / Points.Count + Transform.Position;
		}

		public bool GetPathEdge(int ind, out Vector3 p1, out Vector3 p2)
		{
			if (ind < 0) {
				p1 = Vector3.zero;
				p2 = Vector3.zero;
				return false;
			}
			else if (ind == Points.Count -1) {
				p1 = Points[ind].point;
				p2 = Points[0].point;
				return true;
			}

			p1 = Points[ind].point;
			p2 = Points[ind + 1].point;
			return true;
		}

		public (int ind, float dist, float pos) GetClosestEdge(Vector3 point)
		{
			int   ind           = -1;
			float smallest_dist = float.PositiveInfinity;
			float pos           = 0;

			for (int i = 0; i < Points.Count; i++) {
				if (GetPathEdge(i, out var p1, out var p2)) {
					float dist;
					float t = 0;

					Vector3 AB = p2 - p1;

					if (AB.magnitude * AB.magnitude == 0.0f)
						dist = Vector3.Distance(point, p1);
					else
					{
						t = Mathf.Clamp(Vector3.Dot(point - p1, AB) / (AB.sqrMagnitude), 0, 1);
						Vector3 proj = p1 + t                       * ( p2 - p1 );
						dist = Vector3.Distance(point, proj);
					}

					if (dist < smallest_dist) {
						smallest_dist = dist;
						ind           = i;
						pos           = t;
					}
				}
			}

			return ( ind, smallest_dist, pos);
		}
	}
}
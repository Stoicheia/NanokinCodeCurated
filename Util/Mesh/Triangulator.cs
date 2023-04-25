using System.Collections.Generic;
using AmplifyImpostors;
using UnityEngine;

namespace Util.Procedural
{
	public static class Triangulator
	{
		private static int[] _tempVerts = new int[10000];

		public static void /*int[]*/ Triangulate(List<Vector2> polyPoints, List<int> outIndices /*, bool invertY = true*/)
		{
			outIndices.Clear();
			//List<int> indices = new List<int>();

			int n = polyPoints.Count;
			if (n < 3)
			{
				return;
			}

			int[] V = _tempVerts;

			if (Area(polyPoints) > 0)
			{
				for (int v = 0; v < n; v++)
					V[v] = v;
			}
			else
			{
				for (int v = 0; v < n; v++)
					V[v] = (n - 1) - v;
			}

			int nv    = n;
			int count = 2 * nv;
			for (int m = 0, v = nv - 1; nv > 2;)
			{
				if ((count--) <= 0)
					return;

				int u = v;
				if (nv <= u)
					u = 0;
				v = u + 1;
				if (nv <= v)
					v = 0;
				int w = v + 1;
				if (nv <= w)
					w = 0;

				if (Snip(polyPoints, u, v, w, nv, V))
				{
					int a, b, c, s, t;
					a = V[u];
					b = V[v];
					c = V[w];
					outIndices.Add(a);
					outIndices.Add(b);
					outIndices.Add(c);
					m++;
					for (s = v, t = v + 1; t < nv; s++, t++)
						V[s] = V[t];
					nv--;
					count = 2 * nv;
				}
			}

			outIndices.Reverse();
			//return OutIndices.ToArray();
		}

		public static float Area(List<Vector2> polygon)
		{
			int   n = polygon.Count;
			float A = 0.0f;
			for (int p = n - 1, q = 0; q < n; p = q++)
			{
				Vector2 pval = polygon[p];
				Vector2 qval = polygon[q];
				A += pval.x * qval.y - qval.x * pval.y;
			}

			return (A * 0.5f);
		}

		public static float TriangleArea(Vector2 A, Vector2 B, Vector2 C) => Vector3.Cross(A - B, A - C).magnitude * 0.5f;

		static bool Snip(List<Vector2> polygon, int u, int v, int w, int n, int[] V)
		{
			int     p;
			Vector2 A = polygon[V[u]];
			Vector2 B = polygon[V[v]];
			Vector2 C = polygon[V[w]];
			if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
				return false;
			for (p = 0; p < n; p++)
			{
				if ((p == u) || (p == v) || (p == w))
					continue;
				Vector2 P = polygon[V[p]];
				if (InsideTriangle(P, A, B, C))
					return false;
			}

			return true;
		}

		static bool InsideTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
		{
			bool b1, b2, b3;

			b1 = pt.Cross(v1, v2) < 0.0f;
			b2 = pt.Cross(v2, v3) < 0.0f;
			b3 = pt.Cross(v3, v1) < 0.0f;

			return ((b1 == b2) && (b2 == b3));
		}
	}
}
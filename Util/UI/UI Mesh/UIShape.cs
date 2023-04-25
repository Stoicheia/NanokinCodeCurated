using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Procedural;

namespace Anjin.EditorUtility.UIShape
{
	[Serializable]
	public struct UIShapeVert
	{
		[FormerlySerializedAs("anchor")]
		public Vector2 Anchor;
		[FormerlySerializedAs("offset")]
		public Vector2 Offset;
		[FormerlySerializedAs("scale")]
		public float Scale;

		public Vector2 GetLocalPosition(RectTransform parent)
		{
			float x = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, Anchor.x);
			float y = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, Anchor.y);

			return new Vector2(x, y) + Offset;
		}
	}

	public enum UIShapeLayerType
	{
		Solid,
		Line
	}

	[Serializable]
	public struct UIShapeLayer
	{
		[FormerlySerializedAs("type")]
		public UIShapeLayerType Type;
		[FormerlySerializedAs("color")]
		public Color Color;
		[FormerlySerializedAs("offset")]
		public float Offset;

		[FormerlySerializedAs("split_scale")]
		public bool SplitScale;
		[FormerlySerializedAs("scale"), HideIf("SplitScale")]
		public float Thickness;
		[FormerlySerializedAs("inset"), ShowIf("SplitScale")]
		public float Inset;
		[FormerlySerializedAs("outset"), ShowIf("SplitScale")]
		public float Outsed;

		[HideInInspector]
		[FormerlySerializedAs("manual_miter")]
		public bool DisableMiter;

		[HideInInspector]
		[FormerlySerializedAs("miter_angle")]
		public float MiterAngle;

		public UIShapeLayer(UIShapeLayerType type)
		{
			Type = type;

			Color     = Color.white;
			Thickness = 1;
			Offset    = 0;

			SplitScale = false;
			Inset      = 1;
			Outsed     = 1;

			DisableMiter = false;
			MiterAngle   = 0;
		}
	}


	[Serializable]
	public class UIShape
	{
		private static List<int>     _triangulationResult = new List<int>();
		private static List<Vector2> _points              = new List<Vector2>();
		private static List<float>   _scales              = new List<float>();
		private static List<Segment> _segments            = new List<Segment>();

		public bool IsQuad = false;

		[FormerlySerializedAs("Verticies")]
		public List<UIShapeVert> Vertices = new List<UIShapeVert>
		{
			new UIShapeVert { Anchor = new Vector2(0, 0) },
			new UIShapeVert { Anchor = new Vector2(0, 1) },
			new UIShapeVert { Anchor = new Vector2(1, 1) },
			new UIShapeVert { Anchor = new Vector2(1, 0) }
		};

		public struct Segment
		{
			public Vector3 p1;
			public Vector3 p2;
			public float   t1;
			public float   t2;

			public Vector3 v1;
			public Vector3 v2;
			public Vector3 v3;
			public Vector3 v4;
		}

		public void BuildLayerGeometry(
			UIShapeLayer   layer,
			RectTransform  rectTransform,
			Color          baseColor,
			List<UIVertex> verts,
			List<int>      tris)
		{
			for (int i = 0; i < Vertices.Count; i++)
			{
				UIShapeVert vert = Vertices[i];

				_points.Add(vert.GetLocalPosition(rectTransform));
				_scales.Add(1 + vert.Scale);
			}

			switch (layer.Type)
			{
				case UIShapeLayerType.Solid:
					if (Vertices.Count == 4 && IsQuad)
					{
						AddQuad(verts, tris,
							_points[0],
							_points[1],
							_points[2],
							_points[3],
							layer.Color * baseColor, layer.Thickness);
					}
					else
					{
						AddPolygon(verts, tris,
							layer.Color * baseColor,
							layer.Thickness);
					}

					break;

				case UIShapeLayerType.Line:
					AddPolyLine(
						verts,
						tris,
						layer.Color * baseColor,
						layer.Offset,
						layer.SplitScale
							? layer.Inset
							: layer.Thickness / 2,
						layer.SplitScale
							? layer.Outsed
							: layer.Thickness / 5);

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			_points.Clear();
			_scales.Clear();
		}

		public static void AddQuad(
			[NotNull] List<UIVertex> verts,
			[NotNull] List<int>      tris,
			Vector3                  downLeft,
			Vector3                  upLeft,
			Vector3                  upRight,
			Vector3                  downRight,
			Color                    color,
			float                    scale = 0)
		{
			int index = verts.Count;

			Vector3 dl = downLeft;
			Vector3 ul = upLeft;
			Vector3 dr = downRight;
			Vector3 ur = upRight;

			verts.Add(new UIVertex { color = color, position = dl + GetMidpointAngle(dl, dr, ul) * scale, uv0 = new Vector2(0, 0) });
			verts.Add(new UIVertex { color = color, position = ul + GetMidpointAngle(ul, dl, ur) * scale, uv0 = new Vector2(0, 1) });
			verts.Add(new UIVertex { color = color, position = ur + GetMidpointAngle(ur, ul, dr) * scale, uv0 = new Vector2(1, 1) });
			verts.Add(new UIVertex { color = color, position = dr + GetMidpointAngle(dr, ur, dl) * scale, uv0 = new Vector2(1, 0) });

			tris.Add(index + 0);
			tris.Add(index + 2);
			tris.Add(index + 1);

			tris.Add(index + 3);
			tris.Add(index + 2);
			tris.Add(index + 0);
		}

		public static void AddPolygon(
			[NotNull] List<UIVertex> outVertices,
			List<int>                outTris,
			Color                    color,
			float                    scale
		)
		{
			Triangulator.Triangulate(_points, _triangulationResult);

			int count = outVertices.Count;
			foreach (int i in _triangulationResult)
			{
				outTris.Add(i + count);
			}

			_triangulationResult.Clear();

			int alt = 0;
			for (int i = 0; i < _points.Count; i++)
			{
				Vector3 point = _points[i];
				Vector3 prev  = i <= 0 ? _points[_points.Count - 1] : _points[i - 1];
				Vector3 next  = i < _points.Count - 1 ? _points[i + 1] : _points[0];

				Vector3 dir = GetMidpointAngle(point, prev, next);

				Vector2 uv = new Vector2(0.5f, 0.5f);
				/*switch (alt) {
					case 0: uv = new Vector2(0.95f, 1); 	alt++;   break;
					case 1: uv = new Vector2(0.95f, 1); 	alt++;   break;
					case 2: uv = new Vector2(0.5f, 0.5f); alt = 0; break;
				}*/

				outVertices.Add(new UIVertex { color = color, position = point + dir * scale, uv0 = uv });
			}
		}

		public static void AddPolyLine(
			List<UIVertex> outVerts,
			List<int>      outTris,
			Color          color,
			float          offset,
			float          inset,
			float          outset)
		{
			_segments.Clear();

			for (var i = 0; i < _points.Count; i++)
			{
				Segment? prev = null;
				if (i > 0)
				{
					prev = i > 0
						? _segments[i - 1]
						: _segments[_segments.Count - 1];
				}

				Vector2 now = _points[i];
				Vector2 next = i < _points.Count - 1
					? _points[i + 1]
					: _points[0];


				float s1 = _scales[i];
				float s2 = i < _scales.Count - 1
					? _scales[i + 1]
					: _scales[0];

				Segment s = MakeLineSegment(
					now,
					next,
					s1,
					s2,
					prev,
					inset,
					outset,
					inset,
					outset);

				_segments.Add(s);
			}

			float thickness = Mathf.Abs(inset) + Mathf.Abs(outset);

			for (int i = 0; i < _segments.Count; i++)
			{
				int iprev = i > 0
					? i - 1
					: _segments.Count - 1;

				// int inext = i < _segments.Count - 1 ? i + 1 : 0;

				Segment prev = _segments[iprev];
				Segment now  = _segments[i];

				Vector3 to_next = now.p1 - now.p2;
				Vector3 to_prev = prev.p2 - prev.p1;

				// We need to find the proper direction between the next and previous point
				Vector3 mid_dir = GetMidpointAngle(now.p1, prev.p1, now.p2);

				float angleBetweenSegments = Vector2.Angle(to_prev, to_next) * Mathf.Deg2Rad;

				// Positive sign means the line is turning in a 'clockwise' direction
				float sign = Mathf.Sign(Vector3.Cross(to_next.normalized, to_prev.normalized).z);

				//TODO: Make this an option:
				// float miterDistance = thickness / (2 * Mathf.Tan (angleBetweenSegments / 2));
				float miterDistance = thickness * now.t1 * now.t2;

				var miterPointA = now.p1 - mid_dir.normalized * outset * now.t1 * sign + mid_dir.normalized * offset;
				var miterPointB = now.p1 + mid_dir.normalized * inset * now.t2 * sign + mid_dir.normalized * offset;

				var bevel = false;

				if (miterDistance < to_next.magnitude / 2 &&
				    miterDistance < to_prev.magnitude / 2 &&
				    angleBetweenSegments > 15 * Mathf.Deg2Rad)
				{
					prev.v3 = miterPointA;
					prev.v4 = miterPointB;
					now.v1  = miterPointB;
					now.v2  = miterPointA;
				}
				else
				{
					bevel = true;
				}

				if (bevel)
				{
					if (miterDistance < to_next.magnitude / 2 &&
					    miterDistance < to_prev.magnitude / 2 &&
					    angleBetweenSegments > 30 * Mathf.Deg2Rad)
					{
						if (sign < 0)
						{
							prev.v3 = miterPointA;
							now.v2  = miterPointA;
						}
						else
						{
							prev.v4 = miterPointB;
							now.v1  = miterPointB;
						}
					}

					AddQuad(outVerts, outTris, prev.v3, prev.v4, now.v1, now.v2, color);
				}

				_segments[iprev] = prev;
				_segments[i]     = now;
			}

			for (int i = 0; i < _segments.Count; i++)
			{
				Segment s = _segments[i];
				AddQuad(outVerts, outTris, s.v1, s.v4, s.v3, s.v2, color);
			}
		}


		public static Segment MakeLineSegment(
			Vector2  p1,
			Vector2  p2,
			float    t1,
			float    t2,
			Segment? prevSegment,
			float    thick1 = 1,
			float    thick2 = 1,
			float    thick3 = 1,
			float    thick4 = 1)
		{
			Vector2 offset = new Vector2(p1.y - p2.y, p2.x - p1.x).normalized;

			Segment s = new Segment
			{
				p1 = p1,
				p2 = p2,
				t1 = t1,
				t2 = t2
			};

			if (prevSegment != null)
			{
				Segment p = prevSegment.Value;
				s.v1 = new Vector2(p.v4.x, p.v4.y);
				s.v2 = new Vector2(p.v3.x, p.v3.y);
			}
			else
			{
				s.v1 = p1 - offset * thick1;
				s.v2 = p1 + offset * thick2;
			}

			s.v3 = p2 + offset * thick3;
			s.v4 = p2 - offset * thick4;

			return s;
		}

		public static Vector3 GetMidpointAngle(Vector3 point, Vector3 prev, Vector3 next)
		{
			Vector3 to_next = point - next;
			Vector3 to_prev = point - prev;

			// We need to find the proper direction between the next and previous point
			Vector3 smaller = to_next, larger = to_prev;

			if (to_next.magnitude > to_prev.magnitude)
			{
				smaller = to_prev;
				larger  = to_next;
			}

			float t = smaller.magnitude / larger.magnitude;

			Vector3 samePointOnLarger = point - Vector3.Lerp(Vector3.zero, larger, t);
			Vector3 p1                = samePointOnLarger;
			Vector3 p2                = point - smaller;


			Vector3 midpoint = p1 + (p2 - p1) / 2;
			Vector3 mid_dir  = point - midpoint;

			return mid_dir.normalized;
		}
	}
}
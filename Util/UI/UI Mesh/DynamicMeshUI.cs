using System;
using System.Collections.Generic;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Util.Procedural;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

[RequireComponent(typeof(CanvasRenderer))]
public class DynamicMeshUI : MaskableGraphic
{
	public enum LayerType
	{
		Solid,
		Line
	}

	public enum PositionMode
	{
		Normalized,
		Offset
	}

	[Serializable]
	public struct Layer
	{
		public LayerType type;
		public Color     color;
		public float     scale;

		public float offset;

		public bool  split_scale;
		public float inset;
		public float outset;

		public bool  manual_miter;
		public float miter_angle;

		public Layer(LayerType type)
		{
			this.type = type;
			color     = Color.white;
			scale     = 1;
			offset    = 0;

			split_scale = false;
			inset       = 1;
			outset      = 1;

			manual_miter = false;
			miter_angle  = 0;
		}
	}

	[Serializable]
	public struct Vert
	{
		public Vector2 anchor;
		public Vector2 offset;

		public Vector2 GetLocalPosition(RectTransform parent)
		{
			float x = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, anchor.x);
			float y = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, anchor.y);
			return new Vector2(x, y) + offset;
		}
	}

	public Texture tex;

	public override Texture mainTexture
	{
		get
		{
			if (tex == null)
			{
				if (material != null && material.mainTexture != null)
				{
					return material.mainTexture;
				}

				return s_WhiteTexture;
			}

			return tex;
		}
	}

	private RectTransform rt;

	public bool UseRectSize;

	public List<Vert> Verts = new List<Vert>();

	public List<Layer> Layers = new List<Layer>
	{
		new Layer(LayerType.Solid) { color = Color.white }
	};

	private List<int> _tris;
	private List<int> _tempTriangulation;

	private List<UIVertex> _uiVerts;

	private static List<Vector2> _scratchPoints = new List<Vector2>();

	[Button]
	public void Redraw()
	{
		SetVerticesDirty();
		SetMaterialDirty();
	}

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		SetVerticesDirty();
		SetMaterialDirty();
	}

	private void AddQuad(VertexHelper vh, Vector2 corner1, Vector2 corner2, Vector2 uvCorner1, Vector2 uvCorner2)
	{
		var i = vh.currentVertCount;

		UIVertex vert = new UIVertex();
		vert.color = color; // Do not forget to set this, otherwise

		vert.position = corner1;
		vert.uv0      = uvCorner1;
		vh.AddVert(vert);

		vert.position = new Vector2(corner2.x, corner1.y);
		vert.uv0      = new Vector2(uvCorner2.x, uvCorner1.y);
		vh.AddVert(vert);

		vert.position = corner2;
		vert.uv0      = uvCorner2;
		vh.AddVert(vert);

		vert.position = new Vector2(corner1.x, corner2.y);
		vert.uv0      = new Vector2(uvCorner1.x, uvCorner2.y);
		vh.AddVert(vert);

		vh.AddTriangle(i + 0, i + 2, i + 1);
		vh.AddTriangle(i + 3, i + 2, i + 0);
	}

	// actually update our mesh
	protected override void OnPopulateMesh(VertexHelper vh)
	{
		// Clear vertex helper to reset vertices, indices etc.
		vh.Clear();

		rt = GetComponent<RectTransform>();
		if (rt == null) return;

		if (_tris == null) _tris                           = new List<int>();
		if (_uiVerts == null) _uiVerts                     = new List<UIVertex>();
		if (_tempTriangulation == null) _tempTriangulation = new List<int>();
		if (_segments == null) _segments                   = new List<Segment>();

		_tris.Clear();
		_uiVerts.Clear();

		if (Verts.Count <= 2) return;

		_scratchPoints.Clear();
		for (int i = 0; i < Verts.Count; i++)
		{
			_scratchPoints.Add(Verts[i].GetLocalPosition(rt));
		}

		for (int i = 0; i < Layers.Count; i++)
		{
			var layer = Layers[i];
			switch (layer.type)
			{
				case LayerType.Solid:
					AddPolygon(layer.color * color, layer.scale);
					break;

				case LayerType.Line:
					if (layer.split_scale)
						AddPolyLine(layer.color * color, layer.offset, layer.inset, layer.outset);
					else
						AddPolyLine(layer.color * color, layer.offset, layer.scale / 2, layer.scale / 5);
					break;
			}
		}

		vh.AddUIVertexStream(_uiVerts, _tris);
	}

	private struct Segment
	{
		public Vector3 p1;
		public Vector3 p2;

		public Vector3 v1;
		public Vector3 v2;
		public Vector3 v3;
		public Vector3 v4;
	}

	private List<Segment> _segments;

	private void AddQuad(Vector3 bottom_left, Vector3 top_left, Vector3 top_right, Vector3 bottom_right, Color color)
	{
		int index = _uiVerts.Count;

		_uiVerts.Add(new UIVertex { color = color, position = bottom_left, uv0  = new Vector2(0, 0) });
		_uiVerts.Add(new UIVertex { color = color, position = top_left, uv0     = new Vector2(0, 1) });
		_uiVerts.Add(new UIVertex { color = color, position = top_right, uv0    = new Vector2(1, 1) });
		_uiVerts.Add(new UIVertex { color = color, position = bottom_right, uv0 = new Vector2(1, 0) });

		_tris.Add(index + 0);
		_tris.Add(index + 2);
		_tris.Add(index + 1);

		_tris.Add(index + 3);
		_tris.Add(index + 2);
		_tris.Add(index + 0);
	}

	private void AddPolygon(Color _color, float scale)
	{
		//_triangulation.Clear();
		Triangulator.Triangulate(_scratchPoints, _tempTriangulation);

		int count = _uiVerts.Count;
		foreach (int i in _tempTriangulation)
		{
			_tris.Add(i + count);
		}

		for (int i = 0; i < _scratchPoints.Count; i++)
		{
			Vector3 point = _scratchPoints[i];
			Vector3 prev  = i <= 0 ? _scratchPoints[_scratchPoints.Count - 1] : _scratchPoints[i - 1];
			Vector3 next  = i < _scratchPoints.Count - 1 ? _scratchPoints[i + 1] : _scratchPoints[0];

			Vector3 dir = GetMidpointAngle(point, prev, next);

			/*Vector3 p1 = _scratchPoints[i];
			Vector3 p2 = i < _scratchPoints.Count - 1 ? _scratchPoints[i + 1] : _scratchPoints[0];*/

			//Vector2 offset = new Vector2((p1.y - p2.y), p2.x - p1.x).normalized;

			_uiVerts.Add(new UIVertex { color = _color, position = point + dir * scale });
		}
	}

	private void AddPolyLine(Color _color, float offset, float inset, float outset)
	{
		_segments.Clear();

		for (int i = 0; i < _scratchPoints.Count; i++)
		{
			Segment? prev = null;
			if (i > 0)
			{
				prev = i > 0 ? _segments[i - 1] : _segments[_segments.Count - 1];
			}

			Segment s = MakeLineSegment(_scratchPoints[i], i < _scratchPoints.Count - 1 ? _scratchPoints[i + 1] : _scratchPoints[0], prev,
				inset, outset, inset, outset);

			_segments.Add(s);
		}

		float thickness = Mathf.Abs(inset) + Mathf.Abs(outset);

		using (Draw.WithMatrix(transform.localToWorldMatrix))
		{
			using (Draw.WithDuration(4))
			{
				for (int i = 0; i < _segments.Count; i++)
				{
					int prev_ind = i > 0 ? i - 1 : _segments.Count - 1;
					//int next_ind = i < _segments.Count - 1 ? i + 1 : 0;

					Segment prev    = _segments[prev_ind];
					Segment segment = _segments[i];


					Vector3 to_next = segment.p1 - segment.p2;
					Vector3 to_prev = prev.p2 - prev.p1;

					// We need to find the proper direction between the next and previous point
					Vector3 mid_dir = GetMidpointAngle(segment.p1, prev.p1, segment.p2);
					/*Vector3 smaller = to_next, larger = to_prev;

					if (to_next.magnitude > to_prev.magnitude) {
						smaller = to_prev;
						larger  = to_next;
					}

					float t = smaller.magnitude / larger.magnitude;

					Vector3 samePointOnLarger = segment.p1 - Vector3.Lerp(Vector3.zero, larger, t);
					Vector3 p1 = samePointOnLarger;
					Vector3 p2 = segment.p1 - smaller;


					Vector3 midpoint = p1 + (p2 - p1) / 2;
					Vector3 mid_dir  = segment.p1 - midpoint;*/

					float angleBetweenSegments = Vector2.Angle(to_prev, to_next) * Mathf.Deg2Rad;

					// Positive sign means the line is turning in a 'clockwise' direction
					float sign = Mathf.Sign(Vector3.Cross(to_next.normalized, to_prev.normalized).z);

					//TODO: Make this an option:
					//float miterDistance = thickness / (2 * Mathf.Tan (angleBetweenSegments / 2));
					float miterDistance = thickness;

					var miterPointA = segment.p1 - mid_dir.normalized * miterDistance * sign + mid_dir.normalized * offset;
					var miterPointB = segment.p1 + mid_dir.normalized * miterDistance * sign + mid_dir.normalized * offset;

					var bevel = false;

					if (miterDistance < to_next.magnitude / 2 && miterDistance < to_prev.magnitude / 2 && angleBetweenSegments > 15 * Mathf.Deg2Rad)
					{
						prev.v3    = miterPointA;
						prev.v4    = miterPointB;
						segment.v1 = miterPointB;
						segment.v2 = miterPointA;
					}
					else
					{
						bevel = true;
					}

					if (bevel)
					{
						if (miterDistance < to_next.magnitude / 2 && miterDistance < to_prev.magnitude / 2 && angleBetweenSegments > 30 * Mathf.Deg2Rad)
						{
							if (sign < 0)
							{
								prev.v3    = miterPointA;
								segment.v2 = miterPointA;
							}
							else
							{
								prev.v4    = miterPointB;
								segment.v1 = miterPointB;
							}
						}

						AddQuad(prev.v3, prev.v4, segment.v1, segment.v2, _color);
					}

					_segments[prev_ind] = prev;
					_segments[i]        = segment;
				}
			}

			for (int i = 0; i < _segments.Count; i++)
			{
				Segment s = _segments[i];
				AddQuad(s.v1, s.v4, s.v3, s.v2, _color);
			}
		}
	}

	private Vector3 GetMidpointAngle(Vector3 point, Vector3 prev, Vector3 next)
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


	private Segment MakeLineSegment(Vector2 p1, Vector2 p2, Segment? prevSegment, float thick1 = 1, float thick2 = 1, float thick3 = 1, float thick4 = 1)
	{
		Vector2 offset = new Vector2(p1.y - p2.y, p2.x - p1.x).normalized;

		Segment s = new Segment
		{
			p1 = p1,
			p2 = p2
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

#if UNITY_EDITOR

	[CustomEditor(typeof(DynamicMeshUI))]
	private class DynamicMeshUIEditor : OdinEditor
	{
		private void OnSceneGUI()
		{
			DynamicMeshUI mesh = target as DynamicMeshUI;

			Handles.matrix = mesh.transform.localToWorldMatrix;

			if (Event.current.OnRepaint())
			{
				for (int i = 0; i < mesh.Verts.Count; i++)
				{
					using (Draw.WithMatrix(mesh.transform.localToWorldMatrix))
					{
						Draw.WireSphere((Vector3)mesh.Verts[i].GetLocalPosition(mesh.rectTransform), 2, Color.red);
					}

					/*using (var check = new EditorGUI.ChangeCheckScope()) {
						var p = Handles.PositionHandle(mesh.Verts[i], Quaternion.identity);

						if (check.changed) {
							mesh.Verts[i] = p;
							mesh.transform.gameObject.SetActive(false);
							mesh.transform.gameObject.SetActive(true);
						}
					}*/
				}
			}

			Handles.matrix = Matrix4x4.identity;
		}
	}

	/*private void OnDrawGizmosSelected()
	{

	}*/
#endif
}
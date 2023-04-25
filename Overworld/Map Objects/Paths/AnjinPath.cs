using System;
using System.Collections.Generic;
using Anjin.Editor;
using PathCreation.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Extensions;

namespace Anjin.Nanokin.Map
{
	public class AnjinPath
	{
		public enum CalculationSpace { Relative = 0, Absolute = 1, AbsoluteInPlaymode = 2 }
		public enum HandleMode { Aligned = 0, Mirrored = 1, Free = 2, Automatic = 3, Linear = 4 }

		public class Point // TODO make this a struct
		{
			public Vector3 position;
			public Vector3 left_handle;
			public Vector3 right_handle;


			[SerializeField] private HandleMode _handle_mode_left;
			[SerializeField] private HandleMode _handle_mode_right;

			[SerializeField] private float      _auto_scale  = 0.3f;


			[SerializeField]
			[Obsolete]
			public HandleMode _handle_mode		= HandleMode.Aligned;
			/*public HandleMode handle_mode
			{
				get => _handle_mode;
				set {
					if (_handle_mode != value) WasModified = true;
					_handle_mode = value;
				}
			}*/

			public HandleMode handle_mode_left
			{
				get => _handle_mode_left;
				set {
					if (_handle_mode_left != value) WasModified = true;
					_handle_mode_left = value;
				}
			}

			public HandleMode handle_mode_right
			{
				get => _handle_mode_right;
				set {
					if (_handle_mode_right != value) WasModified = true;
					_handle_mode_right = value;
				}
			}

			public HandleMode handle_mode_both {
				set {
					handle_mode_left  = value;
					handle_mode_right = value;
				}
			}

			public float auto_scale
			{
				get => _auto_scale;
				set
				{
					if (_auto_scale != value) WasModified = true;
					_auto_scale = value;
				}
			}

			[ShowInInspector]
			public bool WasModified = false; // TODO move this to main AnjinPath so we dont have to iterate all points every frame (EnsureVertsUpToDate)


			public Point() : this(new Vector3(1, 0, 0)) { }

			public Point(Vector3 position)
			{
				this.position = position;
				left_handle   = Vector3.left * 3f;
				right_handle  = Vector3.right * 3f;
			}
		}

		public class Vertex // TODO make this a struct
		{
			public Vector3 position;
			public Vector3 tangent;
			public float   time;
			public float   cumulativeLength;
		}


		public bool IsRelative => (CalcSpace == CalculationSpace.Relative || !Application.isPlaying && CalcSpace == CalculationSpace.AbsoluteInPlaymode);
		public bool IsAbsolute => (CalcSpace == CalculationSpace.Absolute || Application.isPlaying && CalcSpace == CalculationSpace.AbsoluteInPlaymode);

		[ShowInInspector, EnumToggleButtons, HideLabel]
		public CalculationSpace CalcSpace {
			get => _calcSpace;
			set {
				if (_calcSpace != value) {
					_prevCalcSpace = _calcSpace;
					_calcSpace     = value;
					OnPathModify();
				}

			}
		}

		[ShowInInspector]
		public bool Closed
		{
			get => _closed;
			set
			{
				if (_closed != value) OnPathModify();
				_closed = value;
			}
		}

		[ShowInInspector]
		public bool UseHolderScale
		{
			get => _useHolderScale;
			set
			{
				if (_useHolderScale != value) OnPathModify();
				_useHolderScale = value;
			}
		}

		[ShowInInspector, PropertyRange(0.05f, 5f)]
		public float VertexSpacing
		{
			get => _vertexSpacing;
			set
			{
				if (_vertexSpacing != value) OnPathModify();
				_vertexSpacing = value;
			}
		}

		[ShowInInspector, PropertyRange(0.01f, 4f)]
		public float VertexAccuracy
		{
			get => _vertexAccuracy;
			set
			{
				if (_vertexAccuracy != value) OnPathModify();
				_vertexAccuracy = value;
			}
		}

		[FoldoutGroup("Debug")]
		[ShowInInspector, ReadOnly, LabelText("Length")]
		public float VertexPathLength;


		[FoldoutGroup("Debug", 1)]
		[ShowInInspector, ReadOnly]
		public readonly string ID = DataUtil.MakeShortID(8);


		[FoldoutGroup("Debug")]
		public Matrix4x4 BaseMatrix {
			get {
				if (Application.isPlaying && CalcSpace == CalculationSpace.AbsoluteInPlaymode)
					return _runtimeBaseMatrix;

				if (Holder != null && (CalcSpace == CalculationSpace.Relative || CalcSpace == CalculationSpace.AbsoluteInPlaymode)) {
					return GetHolderMatrix();
				}

				return Matrix4x4.identity;
			}
		}

		Matrix4x4 GetHolderMatrix()
		{
			if (!UseHolderScale) {
				return Holder.Matrix.TR();
			}

			return Holder.Matrix;
		}

		[NonSerialized]
		[FoldoutGroup("Debug")]
		private Matrix4x4 _runtimeBaseMatrix;

		[FoldoutGroup("Debug")]
		public IAnjinPathHolder Holder;

		[FoldoutGroup("Debug")]
		[NonSerialized]
		public Action OnModified;

		[FoldoutGroup("Debug"), NonSerialized, ShowInInspector]
		public bool ShowPointsInInspector;

		[FoldoutGroup("Debug"), ShowIf("ShowPointsInInspector")]
		public List<Point> Points = new List<Point>();

		public List<AnjinPathMetadataPoint> Metadata = new List<AnjinPathMetadataPoint>();

		[FoldoutGroup("Debug"), HideInInspector, NonSerialized]
		public List<Vertex> _vertices = new List<Vertex>();

		[FoldoutGroup("Debug"), NonSerialized, ShowInInspector]
		public bool ShowVertsInInspector;


		[FoldoutGroup("Debug"), ShowIf("ShowVertsInInspector")]
		public List<Vertex> Vertices
		{
			get
			{
				EnsureVertsUpToDate();
				return _vertices;
			}
		}

		[SerializeField, HideInInspector] private CalculationSpace _calcSpace      = CalculationSpace.Relative;
		[SerializeField, HideInInspector] private bool             _useHolderScale     = true;
		[SerializeField, HideInInspector] private bool             _closed         = false;
		[SerializeField, HideInInspector] private float            _vertexSpacing  = 0.5f;
		[SerializeField, HideInInspector] private float            _vertexAccuracy = 3.5f;

		[NonSerialized, HideInInspector] private CalculationSpace _prevCalcSpace;

		[FoldoutGroup("Debug")]
		[ShowInInspector, NonSerialized]
		private bool _vertsNeedUpdating = false;

		public AnjinPath(IAnjinPathHolder holder)
		{
			Holder = holder;

			AddPoint(new Vector3(-1, 0, 0));
			AddPoint(new Vector3(1, 0, 0));
		}

		public void OnEnterPlaymode()
		{
			if (Holder != null)
				_runtimeBaseMatrix = GetHolderMatrix();
		}

		public void AddPoint(Vector3 position)
		{
			Points.Add(new Point(position));
			OnPathModify();
		}

		public void DeletePointAtIndex(int i)
		{
			if (i < 0 || i >= Points.Count) return;
			Points.RemoveAt(i);
			OnPathModify();
		}

		public void SetPointFromWorldPos(int i, Vector3 worldPos)
		{
			if (i < 0 || i >= Points.Count) return;

			Point p = Points[i];
			if (Holder != null && IsRelative)
				p.position = BaseMatrix.inverse.MultiplyPoint3x4(worldPos);
			else
				p.position = worldPos;

			TryAutoSetHandlesForIndex(i);

			OnPathModify();
		}

		public void SetRightHandleFromWorldPos(int i, Vector3 worldPos)
		{
			if (i < 0 || i >= Points.Count) return;

			Point p = Points[i];

			if (p.handle_mode_right == HandleMode.Automatic) return;

			if (p.handle_mode_right == HandleMode.Linear) {
				p.right_handle = Vector3.zero;
				return;
			}

			if (Holder != null)
				p.right_handle = BaseMatrix.inverse.MultiplyPoint3x4(worldPos) - p.position;
			else
				p.right_handle = worldPos - p.position;

			if (p.handle_mode_right != HandleMode.Free)
			{
				Vector3 dir = p.right_handle.normalized;

				if (p.handle_mode_right == HandleMode.Aligned)
				{
					p.left_handle = -dir * p.left_handle.magnitude;
				}
				else if (p.handle_mode_right == HandleMode.Mirrored)
				{
					p.left_handle = -p.right_handle;
				}
			}

			OnPathModify();
		}

		public void SetLeftHandleFromWorldPos(int i, Vector3 worldPos)
		{
			if (i < 0 || i >= Points.Count) return;

			Point p = Points[i];

			if (p.handle_mode_left == HandleMode.Automatic) return;

			if (p.handle_mode_left == HandleMode.Linear) {
				p.left_handle = Vector3.zero;
				return;
			}

			if (Holder != null)
				p.left_handle = BaseMatrix.inverse.MultiplyPoint3x4(worldPos) - p.position;
			else
				p.left_handle = worldPos - p.position;

			if (p.handle_mode_left != HandleMode.Free)
			{
				Vector3 dir = p.left_handle.normalized;

				if (p.handle_mode_left == HandleMode.Aligned)
				{
					p.right_handle = -dir * p.right_handle.magnitude;
				}
				else if (p.handle_mode_left == HandleMode.Mirrored)
				{
					p.right_handle = -p.left_handle;
				}
			}

			OnPathModify();
		}

		public bool GetPositionAndRotationAtDistance(out Vector3 point, out Quaternion rotation, float distance)
		{
			point    = Vector3.zero;
			rotation = Quaternion.identity;

			if (Vertices == null || Vertices.Count <= 0 || VertexPathLength <= 0) return false;

			float t = distance / VertexPathLength;

			// TODO: Different end of path behaviours
			if (t < 0) t += Mathf.CeilToInt(Mathf.Abs(t));
			t %= 1;

			/*if (Closed) {
				if (t < 0) t += Mathf.CeilToInt (Mathf.Abs (t));
				t %= 1;
			} else {
				t = Mathf.Clamp01 (t);
			}*/

			// Constrain t based on the end of path instruction
			/*switch (endOfPathInstruction) {
				case EndOfPathInstruction.Loop:
					// If t is negative, make it the equivalent value between 0 and 1
					if (t < 0) {
						t += Mathf.CeilToInt (Mathf.Abs (t));
					}
					t %= 1;
					break;
				case EndOfPathInstruction.Reverse:
					t = Mathf.PingPong (t, 1);
					break;
				case EndOfPathInstruction.Stop:
					t = Mathf.Clamp01 (t);
					break;
			}*/

			int prevIndex = 0;
			int nextIndex = Vertices.Count - 1;
			int i         = Mathf.RoundToInt(t * (Vertices.Count - 1)); // starting guess

			Vertex v = Vertices[i];

			// Starts by looking at middle vertex and determines if t lies to the left or to the right of that vertex.
			// Continues dividing in half until closest surrounding vertices have been found.
			while (true)
			{
				// t lies to left
				if (t <= v.time)
				{
					nextIndex = i;
				}
				// t lies to right
				else
				{
					prevIndex = i;
				}

				i = (nextIndex + prevIndex) / 2;

				if (nextIndex - prevIndex <= 1)
				{
					break;
				}

				v = Vertices[i];
			}

			float abPercent = Mathf.InverseLerp(Vertices[prevIndex].time, Vertices[nextIndex].time, t);

			point = Vector3.Lerp(Vertices[prevIndex].position, Vertices[nextIndex].position, abPercent);

			if (Holder != null)
				point = BaseMatrix.MultiplyPoint3x4(point);

			Vector3 tangent = Vector3.forward;
			if (prevIndex == 0 && nextIndex == 0 && VertexPathLength > 1)
			{
				tangent = Vertices[1].tangent;
			}
			else
			{
				tangent = Vector3.Lerp(Vertices[prevIndex].tangent, Vertices[nextIndex].tangent, abPercent);
			}

			rotation = Quaternion.LookRotation(tangent, Vector3.up);

			if (Holder != null && IsRelative)
				rotation = rotation * Holder.transform.rotation;

			return true;
			//return new TimeOnPathData (prevIndex, nextIndex, abPercent);
		}

		[ShowInInspector]
		[FoldoutGroup("Debug")]
		public int NumSegments => Mathf.Max(Points.Count - 1 + (Closed ? 1 : 0), 0);


		public bool GetSegmentPoints(int i, out Point p1, out Point p2)
		{
			p1 = null;
			p2 = null;

			if (!Closed && i > Points.Count || i >= Points.Count)
				return false;

			p1 = Points[i];

			if (!Closed || i < Points.Count - 1)
			{
				p2 = Points[i + 1];
			}
			else
			{
				p2 = Points[0];
			}

			return true;
		}

		public void AddPointToEnd(Point point)
		{
			point.WasModified = true;
			Points.Add(point);
			OnPathModify();
		}

		/// Insert new anchor point at given position. Automatically place control points around it so as to keep shape of curve the same
		public void SplitSegment(int segment, Vector3 position, float splitTime)
		{
			segment   = LoopIndex(segment);
			splitTime = Mathf.Clamp01(splitTime);

			Point first  = Points[segment];
			Point second = Points[LoopIndex(segment + 1)];

			Points.Insert(segment + 1, new Point
			{
				position			= position,
				handle_mode_left	= HandleMode.Automatic,
				handle_mode_right	= HandleMode.Automatic,
			});

			UpdateAllAutoHandles();


			/*if (controlMode == ControlMode.Automatic) {
			    points.InsertRange (segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
			    AutoSetAllAffectedControlPoints (segmentIndex * 3 + 3);
			} else {
			    // Split the curve to find where control points can be inserted to least affect shape of curve
			    // Curve will probably be deformed slightly since splitTime is only an estimate (for performance reasons, and so doesn't correspond exactly with anchorPos)
			    Vector3[][] splitSegment = CubicBezierUtility.SplitCurve (GetPointsInSegment (segmentIndex), splitTime);
			    points.InsertRange (segmentIndex * 3 + 2, new Vector3[] { splitSegment[0][2], splitSegment[1][0], splitSegment[1][1] });
			    int newAnchorIndex = segmentIndex * 3 + 3;
			    MovePoint (newAnchorIndex - 2, splitSegment[0][1], true);
			    MovePoint (newAnchorIndex + 2, splitSegment[1][2], true);
			    MovePoint (newAnchorIndex, anchorPos, true);

			    if (controlMode == ControlMode.Mirrored) {
			        float avgDst = ((splitSegment[0][2] - anchorPos).magnitude + (splitSegment[1][1] - anchorPos).magnitude) / 2;
			        MovePoint (newAnchorIndex + 1, anchorPos + (splitSegment[1][1] - anchorPos).normalized * avgDst, true);
			    }
			}

			// Insert angle for new anchor (value should be set inbetween neighbour anchor angles)
			int newAnchorAngleIndex = (segmentIndex + 1) % perAnchorNormalsAngle.Count;
			int numAngles = perAnchorNormalsAngle.Count;
			float anglePrev = perAnchorNormalsAngle[segmentIndex];
			float angleNext = perAnchorNormalsAngle[newAnchorAngleIndex];
			float splitAngle = Mathf.LerpAngle (anglePrev, angleNext, splitTime);
			perAnchorNormalsAngle.Insert (newAnchorAngleIndex, splitAngle);

			NotifyPathModified ();*/
		}

		public void UpdateAllAutoHandles()
		{
			for (int i = 0; i < Points.Count; i++)
				TryAutoSetHandlesForIndex(i);
		}

		public void InsureLinearHandles()
		{
			for (int i = 0; i < Points.Count; i++) {
				Point p = Points[i];
				if(p.handle_mode_left == HandleMode.Linear)
					p.left_handle = Vector3.zero;

				if(p.handle_mode_right == HandleMode.Linear)
					p.right_handle = Vector3.zero;
			}
		}

		public void TryAutoSetHandlesForIndex(int index)
		{
			if (Points.Count < 1)
				return;

			Point point = Points[index];

			if (point.handle_mode_left != HandleMode.Automatic && point.handle_mode_right == HandleMode.Automatic)
				return;

			Point prev = null;
			if (index > 0 || Closed)
				prev = Points[LoopIndex(index - 1)];

			Point next = null;
			if (index < Points.Count - 1 || Closed)
				next = Points[LoopIndex(index + 1)];

			AutoSetHandles(point, prev, next, point.handle_mode_left == HandleMode.Automatic, point.handle_mode_right == HandleMode.Automatic);
		}

		//private static float[] _neighbourDistances = new float[2];

		/// Calculates good positions (to result in smooth path) for the controls around specified anchor
		public static void AutoSetHandles(Point point, Point prev, Point next, bool left, bool right)
		{
			if (!left && !right || prev == null && next == null) return;

			// Calculate a vector that is perpendicular to the vector bisecting the angle between this anchor and its two immediate neighbours
			// The control points will be placed along that vector
			//Vector3 anchorPos          = point.position;
			Vector3 dir = Vector3.zero;

			float neighbour_dist_1 = 0;
			float neighbour_dist_2 = 0;

			if (prev != null)
			{
				Vector3 offset = prev.position - point.position;
				dir              += offset.normalized;
				neighbour_dist_1 =  offset.magnitude;
			}

			if (next != null)
			{
				Vector3 offset = next.position - point.position;
				dir              -= offset.normalized;
				neighbour_dist_2 =  -offset.magnitude;
			}

			dir.Normalize();

			// Set the control points along the calculated direction, with a distance proportional to the distance to the neighbouring control point
			if(left)
				point.left_handle  = dir * neighbour_dist_1 * point.auto_scale;

			if(right)
				point.right_handle = dir * neighbour_dist_2 * point.auto_scale;

			/*for (int i = 0; i < 2; i++) {
				int controlIndex = anchorIndex + i * 2 - 1;
				if (controlIndex >= 0 && controlIndex < points.Count || isClosed) {
					points[LoopIndex (controlIndex)] = anchorPos + dir * neighbourDistances[i] * autoControlLength;
				}
			}*/
		}

		public void OnPathModify()
		{
			_vertsNeedUpdating = true;

			if (_prevCalcSpace != _calcSpace) {

				var hmat = GetHolderMatrix();

				if ((_prevCalcSpace == CalculationSpace.Absolute && (_calcSpace == CalculationSpace.Relative || _calcSpace == CalculationSpace.AbsoluteInPlaymode))) {
					// Absolute to relative

					for (int i = 0; i < Points.Count; i++) {
						Point p = Points[i];
						p.position = hmat.inverse.MultiplyPoint3x4(p.position);
					}

				} else if((_prevCalcSpace == CalculationSpace.Relative || _prevCalcSpace == CalculationSpace.AbsoluteInPlaymode) && _calcSpace == CalculationSpace.Absolute) {
					// Relative to absolute

					if(Holder != null) {
						for (int i = 0; i < Points.Count; i++) {
							Point p = Points[i];
							p.position = hmat.MultiplyPoint3x4(p.position);
						}
					}
				}

				_prevCalcSpace = _calcSpace;
			}

			UpdateAllAutoHandles();
			OnModified?.Invoke();
		}

		public void EnsureVertsUpToDate()
		{
			HandleVersionUpgrade();

			if (_vertices == null || _vertsNeedUpdating) {

				InsureLinearHandles();
				UpdateAllAutoHandles();
				GenerateVerts(VertexSpacing, VertexAccuracy);
				return;
			}

			bool hasChange = false;
			for (var i = 0; i < Points.Count; i++)
			{
				if (!Points[i].WasModified) continue;

				InsureLinearHandles();
				UpdateAllAutoHandles();
				GenerateVerts(VertexSpacing, VertexAccuracy);
				for (var j = 0; j < Points.Count; j++)
				{
					Points[j].WasModified = false;
				}

				return;
			}
		}

		public void GenerateVerts(float spacing = 0.5f, float accuracy = 0.1f)
		{
			if (spacing <= 0.001f) return;

			if (_vertices == null)
			{
				_vertices = new List<Vertex>();
			}

			_vertices.Clear();

			var     segments               = NumSegments;
			float   distanceSinceLastPoint = 0;
			Vector3 prevPointOnpath        = Vector3.zero;
			Vector3 lastAddedPoint         = Vector3.zero;
			float   currentLength          = 0;


			for (int segment = 0; segment < segments; segment++)
			{
				if (!GetSegmentPoints(segment, out Point p1, out Point p2))
					break;

				if (segment == 0)
				{
					_vertices.Add(new Vertex {position = p1.position});
					prevPointOnpath = p1.position;
					lastAddedPoint  = p1.position;
				}

				//float estimatedLength = CubicBezierUtility.CurveLength(p1.position, p1.position + p1.right_handle, p2.position + p2.left_handle, p2.position);
				float estimatedLength = CubicBezierUtility.BezierSingleLength(new Vector3[] {p1.position, p1.position + p1.right_handle, p2.position + p2.left_handle, p2.position});
				int   divisions       = Mathf.CeilToInt(estimatedLength * accuracy);
				float increment       = 1f / divisions;

				for (float t = increment; t <= 1; t += increment)
				{
					bool isLastPointOnPath   = (t + increment > 1 && segment == segments - 1);
					if (isLastPointOnPath) t = 1;

					Vector3 pointOnPath = CubicBezierUtility.EvaluateCurve(p1.position, p1.position + p1.right_handle, p2.position + p2.left_handle, p2.position, t);
					distanceSinceLastPoint += Vector3.Distance(prevPointOnpath, pointOnPath);

					if (distanceSinceLastPoint >= spacing)
					{
						float overshootDst = distanceSinceLastPoint - spacing;
						pointOnPath += (prevPointOnpath - pointOnPath).normalized * overshootDst;
						t           -= increment;
					}

					if (distanceSinceLastPoint >= spacing || isLastPointOnPath)
					{
						Vertex v = new Vertex();
						currentLength      += (lastAddedPoint - pointOnPath).magnitude;
						v.position         =  pointOnPath;
						v.cumulativeLength =  currentLength;
						v.tangent          =  CubicBezierUtility.EvaluateCurveDerivative(p1.position, p1.position + p1.right_handle, p2.position + p2.left_handle, p2.position, t).normalized;
						_vertices.Add(v);
						distanceSinceLastPoint = 0;
						lastAddedPoint         = pointOnPath;
					}

					prevPointOnpath = pointOnPath;
				}
			}

			VertexPathLength = currentLength;

			for (int i = 0; i < _vertices.Count; i++)
			{
				var v = _vertices[i];
				v.time = v.cumulativeLength / currentLength;
			}

			_vertsNeedUpdating = false;
		}

		public void Reverse()
		{
			throw new NotImplementedException();
		}

		/// Loop index around to start/end of points array if out of bounds (useful when working with closed paths)
		public int LoopIndex(int i)
		{
			return (i + Points.Count) % Points.Count;
		}

		public bool Valid() => Points != null;

		// Versioning
		private int VERSION = 0;

		public void HandleVersionUpgrade()
		{
			// 0 -> 1: Split handle mode variable into two variables.
			if (VERSION == 0) {
				foreach (Point point in Points) {
					point.handle_mode_left  = point._handle_mode;
					point.handle_mode_right = point._handle_mode;
				}
				VERSION = 1;
			}
		}
	}

	public interface IAnjinPathHolder
	{
		AnjinPath Path      { get; }
		Matrix4x4 Matrix    { get; }
		Transform transform { get; }
	}
}
using System.Collections.Generic;
using PathCreation;
using PathCreation.Utility;
using UnityEditor;
using UnityEngine;
using Util.Extensions;

namespace Anjin.Nanokin.Map {
	public class AnjinPathScreenspace
    {
        const int accuracyMultiplier = 10;

		// dont allow vertices to be spaced too far apart, as screenspace-worldspace conversion can then be noticeably off
        const float intermediaryThreshold = .2f;

        public readonly List<Vector3> verticesWorld;

		// For each point in the polyline, says which bezier segment it belongs to
        readonly List<int> vertexToPathSegmentMap;

        // Stores the index in the vertices list where the start point of each segment is
        readonly int[] segmentStartIndices;

        readonly float pathLengthWorld;
        readonly float[] cumululativeLengthWorld;

        Vector2[] points;

        Vector3 prevCamPos;
        Quaternion prevCamRot;
        bool premCamIsOrtho;

		public bool Valid;

		public readonly AnjinPath path;

        readonly Transform transform;
        readonly Vector3 transformPosition;
        readonly Quaternion transformRotation;
        readonly Vector3 transformScale;

        public AnjinPathScreenspace(AnjinPath path, Transform transform, float maxAngleError, float minVertexDst, float accuracy = 1)
		{
			Valid = false;
			if (path.Points.Count <= 0) return;
			Valid = true;

			this.path = path;

            this.transform 		= transform;
            transformPosition 	= transform.position;
            transformRotation 	= transform.rotation;
            transformScale 		= path.UseHolderScale ? transform.localScale : Vector3.one;

            // Split path in vertices based on angle error
            verticesWorld          = new List<Vector3>();
            vertexToPathSegmentMap = new List<int>();
            segmentStartIndices    = new int[path.NumSegments +1];

            verticesWorld.Add(path.Points[0].position);
            vertexToPathSegmentMap.Add(0);

            Vector3 prevPointOnPath = path.Points[0].position;
			Vector3 lastAddedPoint  = prevPointOnPath;

			float dstSinceLastVertex       = 0;
			float dstSinceLastIntermediary = 0;

            for (int segmentIndex = 0; segmentIndex < path.NumSegments /*(path.Closed ? path.NumSegments : path.NumSegments - 1)*/; segmentIndex++)
			{
				AnjinPath.Point point      = path.Points[segmentIndex];
				AnjinPath.Point next_point = path.Points[(segmentIndex + 1) % path.Points.Count];

                //Vector3[]       segmentPoints = path.GetPointsInSegment(segmentIndex);

                verticesWorld.Add(point.position);
                vertexToPathSegmentMap.Add(segmentIndex);
                segmentStartIndices[segmentIndex] = verticesWorld.Count-1;

                prevPointOnPath          = point.position;
                lastAddedPoint           = prevPointOnPath;
                dstSinceLastVertex       = 0;
                dstSinceLastIntermediary = 0;

				Vector3 p1_pos = point.position;
				Vector3 p1_rh  = point.position      + point.right_handle;
				Vector3 p2_lh  = next_point.position + next_point.left_handle;
				Vector3 p2_pos = next_point.position;

                float estimatedSegmentLength = CubicBezierUtility.EstimateCurveLength( p1_pos, p1_rh, p2_lh, p2_pos );

                int divisions = Mathf.CeilToInt(estimatedSegmentLength * accuracy * accuracyMultiplier);
                float increment = 1f / divisions;

                for (float t = increment; t <= 1; t += increment)
                {
                    Vector3 pointOnPath     = CubicBezierUtility.EvaluateCurve( p1_pos, p1_rh, p2_lh,  p2_pos , t);
                    Vector3 nextPointOnPath = CubicBezierUtility.EvaluateCurve( p1_pos, p1_rh, p2_lh, p2_pos ,  t + increment);

                    // angle at current point on path
                    float localAngle = 180 - MathUtility.MinAngle(prevPointOnPath, pointOnPath, nextPointOnPath);
                    // angle between the last added vertex, the current point on the path, and the next point on the path
                    float angleFromPrevVertex = 180 - MathUtility.MinAngle(lastAddedPoint, pointOnPath, nextPointOnPath);
                    float angleError = Mathf.Max(localAngle, angleFromPrevVertex);


                    if (angleError > maxAngleError && dstSinceLastVertex >= minVertexDst)
                    {
                        dstSinceLastVertex = 0;
                        dstSinceLastIntermediary = 0;
                        verticesWorld.Add(pointOnPath);
                        vertexToPathSegmentMap.Add(segmentIndex);
                        lastAddedPoint = pointOnPath;
                    }
                    else
                    {
                        if (dstSinceLastIntermediary > intermediaryThreshold)
                        {
                            verticesWorld.Add(pointOnPath);
                            vertexToPathSegmentMap.Add(segmentIndex);
                            dstSinceLastIntermediary = 0;
                        }
                        else
                        {
                            dstSinceLastIntermediary += (pointOnPath - prevPointOnPath).magnitude;
                        }
                        dstSinceLastVertex += (pointOnPath - prevPointOnPath).magnitude;
                    }
                    prevPointOnPath = pointOnPath;
                }
            }

            segmentStartIndices[path.NumSegments] = verticesWorld.Count;

            // ensure final point gets added (unless path is closed loop)
            if (!path.Closed)
            {
                verticesWorld.Add(path.Points[path.Points.Count - 1].position);
            }
            else
            {
                verticesWorld.Add(path.Points[0].position);
            }

			Matrix4x4 mat = path.BaseMatrix;

			// Calculate length
            cumululativeLengthWorld = new float[verticesWorld.Count];
            for (int i = 0; i < verticesWorld.Count; i++) {
				verticesWorld[i] = mat.MultiplyPoint3x4(verticesWorld[i]);
				if (i > 0) {
                    pathLengthWorld += (verticesWorld[i - 1] - verticesWorld[i]).magnitude;
                    cumululativeLengthWorld[i] = pathLengthWorld;
                }
            }

        }

        void ComputeScreenSpace()
        {
            if (Camera.current.transform.position != prevCamPos || Camera.current.transform.rotation != prevCamRot || Camera.current.orthographic != premCamIsOrtho)
            {
                points = new Vector2[verticesWorld.Count];
                for (int i = 0; i < verticesWorld.Count; i++)
                {
                    points[i] = HandleUtility.WorldToGUIPoint(verticesWorld[i]);
                }

                prevCamPos = Camera.current.transform.position;
                prevCamRot = Camera.current.transform.rotation;
                premCamIsOrtho = Camera.current.orthographic;
            }
        }

        public MouseInfo CalculateMouseInfo()
        {
            ComputeScreenSpace();

            Vector2 mousePos = Event.current.mousePosition;
            float minDst = float.MaxValue;
            int closestPolyLineSegmentIndex = 0;
            int closestBezierSegmentIndex = 0;

            for (int i = 0; i < points.Length - 1; i++)
            {
                float dst = HandleUtility.DistancePointToLineSegment(mousePos, points[i], points[i + 1]);

                if (dst < minDst)
                {
                    minDst = dst;
                    closestPolyLineSegmentIndex = i;
                    closestBezierSegmentIndex = vertexToPathSegmentMap[i];
                }
            }

            Vector2 closestPointOnLine = MathUtility.ClosestPointOnLineSegment(mousePos, points[closestPolyLineSegmentIndex], points[closestPolyLineSegmentIndex + 1]);
            float dstToPointOnLine = (points[closestPolyLineSegmentIndex] - closestPointOnLine).magnitude;
            float percentBetweenVertices = dstToPointOnLine / (points[closestPolyLineSegmentIndex] - points[closestPolyLineSegmentIndex + 1]).magnitude;
            Vector3 closestPoint3D = Vector3.Lerp(verticesWorld[closestPolyLineSegmentIndex], verticesWorld[closestPolyLineSegmentIndex + 1], percentBetweenVertices);

            float distanceAlongPathWorld = cumululativeLengthWorld[closestPolyLineSegmentIndex] + Vector3.Distance(verticesWorld[closestPolyLineSegmentIndex], closestPoint3D);
            float timeAlongPath = distanceAlongPathWorld / pathLengthWorld;

            // Calculate how far between the current bezier segment the closest point on the line is

            int bezierSegmentStartIndex = segmentStartIndices[closestBezierSegmentIndex];
            int bezierSegmentEndIndex = segmentStartIndices[closestBezierSegmentIndex+1];
            float bezierSegmentLength = cumululativeLengthWorld[bezierSegmentEndIndex] - cumululativeLengthWorld[bezierSegmentStartIndex];
            float distanceAlongBezierSegment = distanceAlongPathWorld - cumululativeLengthWorld[bezierSegmentStartIndex];
            float timeAlongBezierSegment = distanceAlongBezierSegment/bezierSegmentLength;

            return new MouseInfo(minDst, closestPoint3D, distanceAlongPathWorld, timeAlongPath, timeAlongBezierSegment, closestBezierSegmentIndex);
        }

        public bool TransformIsOutOfDate() {
            return transform.position != transformPosition || transform.rotation != transformRotation || transform.localScale != transformScale;
        }


        public struct MouseInfo
        {
            public readonly float mouseDstToLine;
            public readonly Vector3 closestWorldPointToMouse;
            public readonly float distanceAlongPathWorld;
            public readonly float timeOnPath;
            public readonly float timeOnBezierSegment;
            public readonly int closestSegmentIndex;


            public MouseInfo(float mouseDstToLine, Vector3 closestWorldPointToMouse, float distanceAlongPathWorld, float timeOnPath, float timeOnBezierSegment, int closestSegmentIndex)
            {
                this.mouseDstToLine = mouseDstToLine;
                this.closestWorldPointToMouse = closestWorldPointToMouse;
                this.distanceAlongPathWorld = distanceAlongPathWorld;
                this.timeOnPath = timeOnPath;
                this.timeOnBezierSegment = timeOnBezierSegment;
                this.closestSegmentIndex = closestSegmentIndex;
            }
        }
    }
}
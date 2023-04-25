﻿using System.Collections.Generic;
using UnityEngine;

namespace Assets.Drawing
{
	public class BezierCurve : MonoBehaviour
	{
		private static readonly float[] Factorial = new float[]
		{
			1.0f,
			1.0f,
			2.0f,
			6.0f,
			24.0f,
			120.0f,
			720.0f,
			5040.0f,
			40320.0f,
			362880.0f,
			3628800.0f,
			39916800.0f,
			479001600.0f,
			6227020800.0f,
			87178291200.0f,
			1307674368000.0f,
			20922789888000.0f,
		};

		private static float Binom(int n, int i)
		{
			float ni;
			float a1 = Factorial[n];
			float a2 = Factorial[i];
			float a3 = Factorial[n - i];
			ni = a1 / (a2 * a3);
			return ni;
		}

		private static float Bernstein(int n, int i, float t)
		{
			float t_i = Mathf.Pow(t, i);
			float t_n_minus_i = Mathf.Pow((1 - t), (n - i));

			float basis = Binom(n, i) * t_i * t_n_minus_i;
			return basis;
		}

		public static Vector3 GetPoint(float t, List<Vector3> controlPoints)
		{
			int N = controlPoints.Count - 1;
			if (N > 16)
			{
				DebugLogger.Log("Too many control points.", LogContext.Combat | LogContext.Graphics, LogPriority.Low);
				controlPoints.RemoveRange(16, controlPoints.Count - 16);
			}

			if (t <= 0) return controlPoints[0];
			if (t >= 1) return controlPoints[controlPoints.Count - 1];

			Vector3 p = new Vector3();

			for (int i = 0; i < controlPoints.Count; ++i)
			{
				Vector3 bn = Bernstein(N, i, t) * controlPoints[i];
				p += bn;
			}

			return p;
		}

		public static List<Vector3> PointList3(
			List<Vector3> controlPoints,
			float interval = 0.01f)
		{
			int N = controlPoints.Count - 1;
			if (N > 16)
			{
				DebugLogger.Log("Too many control points.", LogContext.Combat | LogContext.Graphics, LogPriority.Low);
				controlPoints.RemoveRange(16, controlPoints.Count - 16);
			}

			List<Vector3> points = new List<Vector3>();
			for (float t = 0.0f; t <= 1.0f + interval - 0.0001f; t += interval)
			{
				Vector3 p = new Vector3();
				for (int i = 0; i < controlPoints.Count; ++i)
				{
					Vector3 bn = Bernstein(N, i, t) * controlPoints[i];
					p += bn;
				}
				points.Add(p);
			}

			return points;
		}
	}
}
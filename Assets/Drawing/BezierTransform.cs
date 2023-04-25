using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Anjin.Scripting;
using Anjin.Util;
using Assets.Scripts;
using Combat;
using JetBrains.Annotations;
using UnityEngine;
using Util.UniTween.Value;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;

namespace Assets.Drawing
{
	[ExecuteAlways]
	public class BezierTransform : MonoBehaviour
	{
		[SerializeField] public BezierEditor fromCurve;
		[SerializeField] public BezierEditor toCurve;
		[SerializeField] private bool dontDrawfromCurve;

		public Vector3 OriginAnchor;
		public Vector3 DestinationAnchor;
		public bool Render;

		public Vector3 Up = Vector3.up;


		//can theoretically leave ref keyword out, but this is probably more readable?
		public static void Remap(List<Vector3> fromPoints, ref List<Vector3> toPoints, Vector3 oAnchor, Vector3 dAnchor, Vector3 upInput)
		{
			toPoints.Clear();
			float magnitude = (dAnchor - oAnchor).magnitude;
			Vector3 up = upInput.normalized;
			Vector3 forward = (dAnchor - oAnchor).normalized;
			Vector3 right = Vector3.Cross(forward,  up);
			foreach (var p in fromPoints)
			{
				Vector3 transformedPoint = Vector3.LerpUnclamped(oAnchor, dAnchor, p.z);
				transformedPoint += up * magnitude * p.y;
				transformedPoint -= right * magnitude * p.x;

				toPoints.Add(transformedPoint);
			}
		}

		private void Update()
		{
			fromCurve.Render = !dontDrawfromCurve;

			toCurve.Render = Render;
			Remap(fromCurve.ControlPoints, ref toCurve.SetPoints, OriginAnchor, DestinationAnchor, Up);
		}

		[Button("Set Control Points")]
		private void ButtonSetPoints()
		{
			fromCurve.ProcessPoints();

			toCurve.ResetPoints();
			for (int i = 0; i < fromCurve.PointsCount; i++)
			{
				toCurve.SpawnPoint();
			}
		}

	}
}

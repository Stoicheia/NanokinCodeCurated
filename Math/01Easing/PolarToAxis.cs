using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Util
{
	public static class PolarToAxis // \\convert two polar functions into two axes functions
	{
		public enum EvalAxis{X, Y}


		public static float Evaluate(float t, IEvaluable rFunc, IEvaluable thetaFunc, EvalAxis axis, float rOff, float tOff)
		{
			switch (axis)
			{
				case EvalAxis.X:
					return (rOff + rFunc.Evaluate(t)) * Mathf.Cos((tOff + thetaFunc.Evaluate(t)) * Mathf.PI * 2);
					break;
				default:
					return (rOff + rFunc.Evaluate(t)) * Mathf.Sin((tOff + thetaFunc.Evaluate(t)) * Mathf.PI * 2);
			}
		}

		public static EaseFunction DummyEvaluate(IEvaluable rFunc, IEvaluable thetaFunc, EvalAxis axis, float rOff, float tOff)
		{
			return (f, f1, arg3, arg4) => Evaluate(f/f1, rFunc, thetaFunc, axis, rOff, tOff);
		}
	}
}
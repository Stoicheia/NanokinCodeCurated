using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public class PullbackEase : EasingFunction // \\ax^n + (1-a)x
	{
		public float power;

		public override float Evaluate(float t)
		{
			return _customParameter * Mathf.Pow(t, power) + (1 - _customParameter) * t;
		}
	}
}
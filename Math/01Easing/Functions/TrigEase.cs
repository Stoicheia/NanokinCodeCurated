using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public class TrigEase : EasingFunction // a\sin(2\pi b(t-k))
	{
		public float amplitude;
		public float frequency;
		public float phase;
		public bool absoluteValue;

		public override float Evaluate(float t)
		{
			float res = Mathf.Sin(2 * Mathf.PI * frequency * (t - phase));
			return absoluteValue ? amplitude * Mathf.Abs(res) : amplitude * res;
		}
	}
}
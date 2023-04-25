using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public class LashEase : EasingFunction // a\sin(c \pi t)^n
	{
		public float amplitude;
		public float intensity;
		public int count;

		public override float Evaluate(float t)
		{
			float frequency = 2 * count - 1;
			return amplitude * Mathf.Pow(Mathf.Sin(frequency * Mathf.PI * t), intensity * 12);
		}
	}
}
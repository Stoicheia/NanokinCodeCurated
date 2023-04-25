using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public class PolynomialEase : EasingFunction // \sum_{i=0}^N a_i t^i
	{
		public List<float> coefficients;

		public override float Evaluate(float t)
		{
			float res = 0;
			for (int i = 0; i < coefficients.Count; i++)
			{
				res += coefficients[i] * Mathf.Pow(t, i);
			}

			return res;
		}
	}
}
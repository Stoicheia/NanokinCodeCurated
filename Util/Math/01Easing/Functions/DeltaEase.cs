using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public class DeltaEase : EasingFunction // \\x^n or 1-(1-x)^n
	{
		public enum DeltaEaseType { In, Out, Sigmoid }

		public DeltaEaseType EaseType;

		public override float Evaluate(float t)
		{
			switch (EaseType)
			{
				case DeltaEaseType.In:
					return Mathf.Pow(t, _customParameter);
					break;
				case DeltaEaseType.Out:
					return 1-Mathf.Pow(1 - t, _customParameter);
					break;
				default:
					return (1 - t) * Mathf.Pow(t, _customParameter) + t * (1 - Mathf.Pow(1 - t, _customParameter));
			}
		}
	}
}
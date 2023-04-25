using System;
using Anjin.Actors;
using UnityEngine;

namespace Util
{
	[Serializable]
	public class JumpHanger
	{
		[SerializeField] private float          Range;
		[SerializeField] private AnimationCurve GravityScale = AnimationCurve.Linear(0, 1, 1, 0.8f);

		public float GetGravityScale(AirMetrics metrics)
		{
			// Hang time feature.
			float yDeltaAbs = Mathf.Abs(metrics.yDelta);

			bool inHangTimeWindow = yDeltaAbs < Range;
			if (inHangTimeWindow)
			{
				// NOTE: Updated to use the delta for the current frame only, it seems to be more robust this way.
				// Gravity naturally curves yDelta so that positive=rising, 0=apex, negative=falling.

				float t = (Range - yDeltaAbs) / Range;
				return GravityScale.Evaluate(t);
			}

			return 1;
		}
	}
}
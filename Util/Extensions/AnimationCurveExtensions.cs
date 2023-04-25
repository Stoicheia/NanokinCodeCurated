using Anjin.Util;
using JetBrains.Annotations;
using UnityEngine;

namespace Util.Extensions
{
	public static class AnimationCurveExtensions
	{
		public static AnimationCurve GetInverse([NotNull] this AnimationCurve initialCurve)
		{
			var inverseCurve = new AnimationCurve();

			for (var i = 0; i < initialCurve.length; i++)
			{
				float inWeight  = initialCurve.keys[i].inTangent * initialCurve.keys[i].inWeight / 1;
				float outWeight = initialCurve.keys[i].outTangent * initialCurve.keys[i].outWeight / 1;

				var inverseKey = new Keyframe(initialCurve.keys[i].value, initialCurve.keys[i].time, 1 / initialCurve.keys[i].inTangent, 1 / initialCurve.keys[i].outTangent, inWeight, outWeight);

				inverseCurve.AddKey(inverseKey);
			}

			return inverseCurve;
		}

		public static float EvaluateSafe([NotNull] this AnimationCurve curve, float time)
		{
			float? min = null;
			float? max = null;

			for (int i = 0; i < curve.length; i++) {
				float t = curve.keys[i].time;

				if (min == null || t < min) min = t;
				if (max == null || t > max) max = t;
			}

			if (min == null || max == null) return 0;

			return curve.Evaluate(time.Clamp(min.Value, max.Value));
		}
	}
}
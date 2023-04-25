using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	public class ActorPlaybackCurves : ScriptableObject
	{
		[ListDrawerSettings(ShowIndexLabels = true)]
		public AnimationCurve[] InterpolationCurves;

		public int LinearCurve = 0;

		private float[] _tmpCurves;

		private void Awake()
		{
			_tmpCurves = new float[InterpolationCurves.Length * 2];
		}

		public int GetApproximateCurve([NotNull] List<float> set)
		{
			if (set.Count == 0) return -1;
			if (set.Count == 1) return -1;
			if (set.Count <= 3) return LinearCurve; // Not enough samples for meaningful interpolation

			Array.Resize(ref _tmpCurves, InterpolationCurves.Length * 2);

			// Reset curve sums
			for (var i = 0; i < _tmpCurves.Length; i++)
			{
				_tmpCurves[i] = 0;
			}

			// Find the window
			float max = float.MinValue;
			float min = float.MaxValue;
			for (var i = 0; i < set.Count; i++)
			{
				float sample = set[i];

				max = Mathf.Max(max, sample);
				min = Mathf.Min(min, sample);
			}

			if (Mathf.Abs(max - min) < 0.1f)
				return LinearCurve; // Too small for a specific curve to be noticeable

			// Take the samples and compare distance to
			float segspan = 1 / (float) (set.Count - 1);

			for (var i = 0; i < set.Count; i++)
			{
				float sample = set[i];

				float x = i * segspan;
				float y = Mathf.InverseLerp(min, max, sample);

				for (var j = 0; j < InterpolationCurves.Length; j++)
				{
					float cy    = InterpolationCurves[j].Evaluate(x);
					float invcy = 1 - InterpolationCurves[j].Evaluate(x);

					float dist    = Mathf.Abs(cy - y);
					float invdist = Mathf.Abs(invcy - y);

					_tmpCurves[j]                              += dist;
					_tmpCurves[j + InterpolationCurves.Length] += invdist;
				}
			}

			// Find the curve which best describes the set
			int best = LinearCurve;
			for (var i = 0; i < _tmpCurves.Length; i++)
			{
				_tmpCurves[i] /= set.Count;

				float distanceAvg = _tmpCurves[i];
				if (distanceAvg < _tmpCurves[best])
				{
					best = i;
				}
			}

			if (best >= InterpolationCurves.Length)
				best -= InterpolationCurves.Length;

			return best;
		}
	}
}
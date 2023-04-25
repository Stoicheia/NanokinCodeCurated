using System.Collections.Generic;
using System.Text;

namespace Util.Collections
{
	/// <summary>
	/// A weight map to allow weighted random retrieval.
	/// Usage:
	///		fill with weighted values and then call Choose()
	///		weight is any arbitrary values.
	///		e.g. for 1A 1B 2C: A and B have 25% chance each, C has 50%.
	///		the values are all relative to one another and always add up to 100% together.
	///
	/// </summary>
	/// <typeparam name="TValue"></typeparam>
	public class WeightMap<TValue>
	{
		private readonly Dictionary<TValue, int> _indices  = new Dictionary<TValue, int>();
		private readonly List<WeightSegment>     _segments = new List<WeightSegment>();

		public WeightMap()
		{ }

		public WeightMap(IEnumerable<TValue> values, float startWeight = 0)
		{
			foreach (TValue value in values)
			{
				SetWeight(value, startWeight);
			}
		}

		public float TotalLength { get; private set; }

		public void Clear()
		{
			TotalLength = 0;
			_indices.Clear();
			_segments.Clear();
		}

		private float GetWeight(TValue value)
		{
			if (_indices.TryGetValue(value, out int idx))
				return _segments[idx].Length;
			else
				return 0;
		}

		private void RegisterWeight(TValue value, float weight)
		{
			float start = TotalLength;
			float end   = weight + start;

			_indices.Add(value, _segments.Count);
			_segments.Add(new WeightSegment(value, start, end));

			TotalLength += weight;
		}

		public void SetWeight(TValue value, float weight)
		{
			if (!_indices.TryGetValue(value, out int idx))
			{
				RegisterWeight(value, weight);
				return;
			}

			WeightSegment range = _segments[idx];

			range.Length = weight;
			UpdateSegmentsAfter(idx);
		}

		public void Add(TValue value, float weight)
		{
			if (!_indices.ContainsKey(value))
			{
				RegisterWeight(value, weight);
				return;
			}

			float currentWeight = GetWeight(value);
			SetWeight(value, currentWeight + weight);
		}

		public void ScaleWeight(TValue value, float multiplier)
		{
			if (!_indices.ContainsKey(value))
			{
				RegisterWeight(value, multiplier);
				return;
			}

			float currentWeight = GetWeight(value);
			SetWeight(value, currentWeight * multiplier);
		}

		private void UpdateSegmentsAfter(int idx)
		{
			float ptr = _segments[idx].Max;

			for (int i = idx + 1; i < _segments.Count; i++)
			{
				WeightSegment segment = _segments[idx];

				segment.Start = ptr;

				ptr += segment.Length;
			}

			TotalLength = ptr;
		}

		public TValue Choose()
		{
			float weight = RNG.Range(0, TotalLength);

			return GetSegment(weight).Value;
		}

		private WeightSegment GetSegment(float goalWeight)
		{
			if (goalWeight < 0 || goalWeight > TotalLength)
				return default;

			// This retrieval has a best case of O(1) and worst case of O(N)
			foreach (WeightSegment segment in _segments)
			{
				if (segment.Contains(goalWeight))
				{
					// This is the goal segment.
					return segment;
				}
			}

			// If we ever need top performance we can reduce to O(log n) with a binary search:
			// 0. Initialize LR bounds as a range of index [0, len[
			// 1. Pick the center segment of the LR index range.
			// 2. If this is the goal, exit and return the value of the goal segment.
			// 3. Check which side the goal would find itself in (weight < center.start == left side, weight > center.end == right side)
			// 4. Update the LR bounds: making sure to update the L/R bounds.
			// 	  a) If we will search to the left, R bound = index of current center segment
			//    b) If we will search to the right, L bound = index of current center segment
			// 5. Start over at 1..

			return default;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder($"Total={TotalLength}, Count={_indices.Count}\n{{\n");

			foreach (WeightSegment range in _segments)
			{
				sb.Append($"\t:{range}\n");
			}

			sb.Append("}");
			return sb.ToString();
		}


		private class WeightSegment
		{
			private FloatRange _range;

			public WeightSegment(TValue value, float start, float end)
			{
				Value  = value;
				_range = new FloatRange(start, end);
			}

			public TValue Value { get; }

			public float Length
			{
				get => _range.Span;
				set => _range.max = _range.min + value;
			}

			public float Start
			{
				get => _range.min;
				set
				{
					float length = Length;
					_range.min = value;
					_range.max = _range.min + length;
				}
			}

			public float Max => _range.max;

			public bool Contains(float weight)
			{
				return _range.Contains(weight);
			}
		}
	}
}
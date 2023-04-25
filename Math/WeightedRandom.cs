using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuestsOld;
using UnityEngine;

public static class WeightedRandom<T>
{
	public static T Choose(List<(T, float)> objectsWithWeights, float norm = 1, float bias = 0)
	{
		List<T> objects = objectsWithWeights.Select(x => x.Item1).ToList();
		List<float> weights = objectsWithWeights.Select(x => x.Item2).ToList();
		return Choose(objects, weights, norm, bias);
	}

	// <summary> Pick a weighted random object in a certain norm. For negative weights, use bias. </summary>
	public static T Choose(List<T> objects, List<float> weights, float norm = 1, float bias = 0)
	{
		if (float.IsPositiveInfinity(norm))
		{
			float max = Single.NegativeInfinity;
			T maxObject = objects[0];
			for(int i = 0; i < Math.Min(objects.Count, weights.Count); i++)
			{
				if (weights[i] > max)
				{
					max = weights[i];
					maxObject = objects[i];
				}
			}

			return maxObject;
		}

		List<float> positiveWeights = weights.Select(x => x + bias - Mathf.Min(0,weights.Min())).ToList();
		List<float> normedWeights = positiveWeights.Select(x => Mathf.Pow(x, norm)).ToList();
		float randomPosition = UnityEngine.Random.Range(0.0f, normedWeights.Sum());
		float cumulativeSum = 0;
		for(int i = 0; i < Math.Min(objects.Count, normedWeights.Count); i++)
		{
			cumulativeSum += normedWeights[i];
			if (cumulativeSum >= randomPosition)
			{
				return objects[i];
			}
		}

		return objects.Last();
	}

	// <summary> Pick a weighted random object in a certain norm. For negative weights, use bias. </summary>

	public static T Choose(Dictionary<T, float> objects, float norm = 1, float bias = 0)
	{
		return Choose(objects.Keys.ToList(), objects.Values.ToList(), norm, bias);
	}

	// <summary>Pick a weighted random object. Use this for precalculated cumulative weights. Sort weights ascending with first index equal to weight instead of zero. </summary>
	public static T ChooseFast(List<T> objects, List<float> cumulativeWeights)
	{
		float randomPosition = UnityEngine.Random.Range(0.0f, cumulativeWeights.Last());
		int ptr = -1;
		int size = Math.Min(objects.Count, cumulativeWeights.Count);
		for (int k = size/2; k > 0; k /= 2)
		{
			while (ptr + k < size - 1 && cumulativeWeights[ptr + k] < randomPosition)
			{
				ptr += k;
			}
		}

		return objects[ptr+1];
	}

	// <summary>Pick a weighted random object. Use this for precalculated cumulative weights. Sort weights ascending with first index equal to weight instead of zero. </summary>
	public static T ChooseFast(Dictionary<T, float> objects)
	{
		return ChooseFast(objects.Keys.ToList(), objects.Values.ToList());
	}

}

using UnityEngine;

public class FrequencyConfig : ScriptableObject
{
	public float low = 1, high = 2;

	public float Next()
	{
		return RNG.Range(low, high);
	}
}
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Util
{
	[Serializable]
	public class RangeVector
	{
		public Vector3 min, max;

		public Vector3 Evaluate => new Vector3(
			Random.Range(min.x, max.x),
			Random.Range(min.y, max.y),
			Random.Range(min.z, max.z)
		);
	}
}


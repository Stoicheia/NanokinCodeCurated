using Anjin.Util;
using UnityEngine;

namespace Util
{
	public struct Centroid
	{
		private Vector3 _sum;
		private int     _count;

		public void add(Vector3 p)
		{
			_sum += p;
			_count++;
		}

		public Vector3 get() => _sum / _count.Minimum(1);

		public static implicit operator Vector2(Centroid ctr)
		{
			return ctr.get();
		}
	}
}
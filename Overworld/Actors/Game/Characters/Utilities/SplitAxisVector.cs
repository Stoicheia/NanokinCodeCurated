using UnityEngine;

namespace Anjin.Actors
{
	/// <summary>
	/// A vector that has been split in two to facilitate working on each individual axis.
	/// </summary>
	public struct SplitAxisVector
	{
		public Vector3 horizontal;
		public Vector3 vertical;

		public SplitAxisVector(Vector3 horizontal, Vector3 vertical)
		{
			this.horizontal = horizontal;
			this.vertical   = vertical;
		}

		public SplitAxisVector(Vector3 singleAxisVector)
		{
			horizontal = new Vector3(singleAxisVector.x, 0, singleAxisVector.z);
			vertical   = new Vector3(0, singleAxisVector.y, 0);
		}

		public static implicit operator Vector3(SplitAxisVector splitAxis)
		{
			return splitAxis.horizontal + splitAxis.vertical;
		}
	}
}
using UnityEngine;

namespace Util.Extensions {
	public static partial class Extensions {

		private static Vector3[] _scratchPoints = new Vector3[4];

		/// <summary>
		/// Calls RectTransform.GetWorldCorners, and returns the four supplied vectors, without an extra array allocation.
		/// </summary>
		/// <param name="rt">The RectTransform to get the corners of.</param>
		/// <param name="p1">The bottom-left corner.</param>
		/// <param name="p2">The top-left corner.</param>
		/// <param name="p3">The top-right corner.</param>
		/// <param name="p4">The bottom-right corner.</param>
		public static void GetWorldCorners(this RectTransform rt, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4)
		{
			rt.GetWorldCorners(_scratchPoints);
			p1 = _scratchPoints[0];
			p2 = _scratchPoints[1];
			p3 = _scratchPoints[2];
			p4 = _scratchPoints[3];
		}


		public static void TransformTo(this RectTransform rt, RectTransform other) {

			GetWorldCorners(rt, out var p1, out var p2, out var p3, out var p4);
			GetWorldCorners(rt, out var op1, out var op2, out var op3, out var op4);


			rt.position = op1;
		}
	}
}
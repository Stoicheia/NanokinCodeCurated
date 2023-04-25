using UnityEngine;

namespace Util.Extensions {
	public static partial class Extensions {

		public static Matrix4x4 TR(this Matrix4x4 mat)                => mat.TR(Vector3.one);
		public static Matrix4x4 TR(this Matrix4x4 mat, Vector3 scale) => Matrix4x4.TRS(mat.GetColumn(3), mat.rotation, scale);

	}
}
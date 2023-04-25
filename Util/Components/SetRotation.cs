using UnityEngine;

namespace Anjin.Utils
{
	[AddComponentMenu("Anjin: Transform/Rotation: Hard set")]
	public class SetRotation : MonoBehaviour
	{
		public bool SetX;
		public bool SetY;
		public bool SetZ;

		public float X;
		public float Y;
		public float Z;

		private void Update()
		{
			Vector3 euler = transform.rotation.eulerAngles;
			transform.rotation = Quaternion.Euler(
				(SetX) ? X : euler.x,
				(SetY) ? Y : euler.y,
				(SetZ) ? Z : euler.z);
		}
	}
}
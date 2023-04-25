using UnityEngine;
using Random = UnityEngine.Random;

namespace Util.Components
{
	[AddComponentMenu("Anjin: Transform/Randomize")]
	public class RandomizeTransform : MonoBehaviour
	{
		public bool RandomizeOnAwake = true;

		public Bounds PositionBounds = new Bounds(Vector3.zero, Vector3.one * 20);

		public bool RandomizePosition;
		public bool RandomizeRotation;

		public bool RotX;
		public bool RotY;
		public bool RotZ;

		void Awake()
		{
			if (RandomizePosition) {
				transform.position = new Vector3(
					Random.Range(PositionBounds.min.x, PositionBounds.max.x),
					Random.Range(PositionBounds.min.y, PositionBounds.max.y),
					Random.Range(PositionBounds.min.z, PositionBounds.max.z));
			}

			if (RandomizeRotation) {
				Vector3 euler     = transform.rotation.eulerAngles;
				if (RotX) euler.x = Random.Range(0, 359);
				if (RotY) euler.y = Random.Range(0, 359);
				if (RotZ) euler.z = Random.Range(0, 359);
			}
		}
	}
}
using API.Spritesheet.Indexing.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Utils
{
	[AddComponentMenu("Anjin: Transform/Animation/Rotate: over time")]
	public class RotateOverTime : MonoBehaviour
	{
		[FormerlySerializedAs("deltaPerSecond")]
		public Vector3 DeltaPerSecond;

		public Quaternion baseRotation;

		private void OnEnable()
		{
			SimpleAnimationsSystem.rotateOverTime.Add(this);
			SimpleAnimationsSystem.dirty = true;
		}

		private void OnDisable()
		{
			SimpleAnimationsSystem.rotateOverTime.Remove(this);
			SimpleAnimationsSystem.dirty = true;
		}
	}
}
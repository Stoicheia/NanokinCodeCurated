using API.Spritesheet.Indexing.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Utils
{
	public class MoveOverTime : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("movementPerSecond")]
		public Vector3 UnitPerSecond;

		private void OnEnable()
		{
			SimpleAnimationsSystem.moveOverTime.Add(this);
			SimpleAnimationsSystem.dirty = true;
		}

		private void OnDisable()
		{
			SimpleAnimationsSystem.moveOverTime.Remove(this);
			SimpleAnimationsSystem.dirty = true;
		}
	}
}
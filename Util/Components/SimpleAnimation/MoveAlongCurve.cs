using System;
using API.Spritesheet.Indexing.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Utils
{
	public class MoveAlongCurve : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("_amplitude"), SerializeField]
		public Vector3 Amplitude;

		[FormerlySerializedAs("_x"), SerializeField]
		public AnimationCurve X = AnimationCurve.Constant(0, 1, 1);

		[FormerlySerializedAs("_y"), SerializeField]
		public AnimationCurve Y = AnimationCurve.Constant(0, 1, 1);

		[FormerlySerializedAs("_z"), SerializeField]
		public AnimationCurve Z = AnimationCurve.Constant(0, 1, 1);

		[NonSerialized] public float   elapsed;
		[NonSerialized] public Vector3 startPosition;

		private void OnEnable()
		{
			SimpleAnimationsSystem.moveAlongCurve.Add(this);
		}

		private void OnDisable()
		{
			SimpleAnimationsSystem.moveAlongCurve.Remove(this);
		}

		private void Start()
		{
			startPosition = transform.localPosition;
		}
	}
}
using System;
using API.Spritesheet.Indexing.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Utils
{
	[AddComponentMenu("Anjin: Transform/Scale: over curve")]
	public class ScaleAlongCurve : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("_amplitude"),SerializeField]
		public Vector3 Amplitude;

		public bool UseSetScale;

		[FormerlySerializedAs("_x"),SerializeField] public AnimationCurve X = AnimationCurve.Constant(0, 1, 1);
		[FormerlySerializedAs("_y"),SerializeField] public AnimationCurve Y = AnimationCurve.Constant(0, 1, 1);
		[FormerlySerializedAs("_z"),SerializeField] public AnimationCurve Z = AnimationCurve.Constant(0, 1, 1);


		[NonSerialized]
		public float elapsed;

		[NonSerialized, HideInInspector]
		public Vector3 _startingScale;

		private void Awake()
		{
			_startingScale = transform.localScale;
		}

		private void OnEnable()
		{
			SimpleAnimationsSystem.scaleAlongCurves.Add(this);
		}

		private void OnDisable()
		{
			SimpleAnimationsSystem.scaleAlongCurves.Remove(this);
		}
	}
}
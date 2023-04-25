using System;
using API.Spritesheet.Indexing.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Anjin.Utils
{
	public class OscillateOverTime : MonoBehaviour
	{
		[FormerlySerializedAs("amplitude")]
		public Vector3 Amplitude;

		[FormerlySerializedAs("duration")]
		public float Duration;

		public bool RandomizeOnStartup;

		[NonSerialized] public Vector3 basePosition;
		[NonSerialized] public float   t;

		private void OnEnable()
		{
			if (RandomizeOnStartup)
				t = RNG.Float * Duration;

			SimpleAnimationsSystem.oscillateOverTime.Add(this);
			SimpleAnimationsSystem.dirty = true;
		}

		private void OnDisable()
		{
			SimpleAnimationsSystem.oscillateOverTime.Remove(this);
			SimpleAnimationsSystem.dirty = true;
		}

		private void Start()
		{
			basePosition = transform.localPosition;
		}
	}
}
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Utils
{
	public class FreezeFrameVolume : TimeScaleVolumeBase
	{
		[SerializeField]
		[FormerlySerializedAs("duration"), SuffixLabel("frames")]
		public int DurationFrames;

		[NonSerialized]
		public Action onEnded;

		private float _elapsed;

		public override float Scaling => 0;

		protected override void Update()
		{
			base.Update();

			_elapsed += Time.deltaTime;

			if (_elapsed >= DurationFrames * 1 / 60f)
			{
				onEnded?.Invoke();
				onEnded = null;

				Destroy(gameObject);
			}
		}
	}
}
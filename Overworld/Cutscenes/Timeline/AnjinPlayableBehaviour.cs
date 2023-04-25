using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Overworld.Cutscenes.Timeline {

	public class AnjinPlayableBehaviour : PlayableBehaviour {

		[HideInInspector] public double clipStart;

		public virtual bool CanPlay => true;

		public AnjinPlayableBehaviour() : base() { }

		protected struct ProcessInfo {
			public double currentTimelineTime;
			public double globalClipTime;
			public bool   insideClip;
			public float  weight;
		}

		public void MixerProcessFrameBase(Playable playable, FrameData frameData, object playerData, double timelineCurrentTime, float weight)
		{
			if (!CanPlay) return;

			var glbTIme = timelineCurrentTime - clipStart;
			ProcessInfo info = new ProcessInfo {
				currentTimelineTime = timelineCurrentTime,
				globalClipTime      = glbTIme,
				insideClip          = glbTIme >= 0 && glbTIme < playable.GetDuration(),
				weight				= weight
			};

			MixerProcessFrame(playable, frameData, playerData, info);
		}

		protected virtual void MixerProcessFrame(Playable playable, FrameData frameData, object playerData, ProcessInfo info) { }


		protected float  scaleByWeight(float  val, float weight, float weight_effect) => val - (1 - weight) * weight_effect;
		protected double scaleByWeight(double val, float weight, float weight_effect) => val - (1 - weight) * weight_effect;

		public virtual void Cleanup() { }

		public override void OnPlayableDestroy(Playable playable) => Cleanup();
		public override void OnGraphStop(Playable       playable) => Cleanup();

	}
}
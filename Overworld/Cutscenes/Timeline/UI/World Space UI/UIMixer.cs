using Anjin.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace Overworld.Cutscenes.Timeline {
	public class UIMixer : PlayableBehaviour {

		private bool  firstFrameHappened;
		private float defaultWorldDistance;

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			double timelineCurrentTime = (playable.GetGraph().GetResolver() as PlayableDirector).time;

			int inputCount = playable.GetInputCount();

			if (!firstFrameHappened) {
				defaultWorldDistance = 0;

				/*if(CutsceneHUD.Exists)
					defaultWorldDistance = CutsceneHUD.Live.WorldSpaceCanvas.planeDistance;*/

				firstFrameHappened   = true;
			}

			//float blendedColor    = Color.clear;
			float blendedDistance		= 0;
			float totalDistanceWeight	= 0f;

			/*float blendedAlpha     = 0;
			float totalAlphaWeight = 0f;*/

			for (int i = 0; i < inputCount; i++) {

				float inputWeight = playable.GetInputWeight(i);

				ScriptPlayable<AnjinPlayableBehaviour> inputPlayable = (ScriptPlayable<AnjinPlayableBehaviour>)playable.GetInput(i);
				AnjinPlayableBehaviour                 input         = inputPlayable.GetBehaviour();
				input.MixerProcessFrameBase(inputPlayable, info, playerData, timelineCurrentTime, inputWeight);

				if(input is WorldSpaceUIBehaviour worldSpace) {
					if (worldSpace.WeightEffectsDistance > Mathf.Epsilon) {
						blendedDistance     += worldSpace.WorldDistance - (1 - inputWeight) * worldSpace.WeightEffectsDistance;
						totalDistanceWeight += inputWeight * worldSpace.WeightEffectsDistance;
					}
				}

				/*if(input.WeightEffectsAlpha > Mathf.Epsilon) {
					blendedAlpha     += input.Alpha * inputWeight * input.WeightEffectsAlpha;
					totalAlphaWeight += inputWeight * input.WeightEffectsAlpha;
				}*/

			}

			if (!CutsceneHUD.Exists) return;

			//CutsceneHUD.Live.WorldSpaceCanvas.planeDistance = blendedDistance + defaultWorldDistance * (1f - totalDistanceWeight);
		}

		public override void OnPlayableDestroy(Playable playable)
		{
			if(firstFrameHappened) {
				firstFrameHappened = false;

				/*if(CutsceneHUD.Exists)
					CutsceneHUD.Live.WorldSpaceCanvas.planeDistance = defaultWorldDistance;*/
			}
		}
	}
}
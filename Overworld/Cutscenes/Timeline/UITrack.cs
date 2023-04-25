using Overworld.Cutscenes.Timeline.UI;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Overworld.Cutscenes.Timeline {

	[
		TrackClipType(typeof(WorldSpaceUIClip)),
		TrackClipType(typeof(UIClip)),
	]
	public class UITrack : TrackAsset {

		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var playable = ScriptPlayable<UIMixer>.Create(graph, inputCount);

			foreach (var clip in m_Clips)
			{
				if(clip.asset is AnjinPlayableAssetBase anjinAsset) {
					anjinAsset.clipStart = clip.start;
				}
			}

			return playable;
		}

	}
}
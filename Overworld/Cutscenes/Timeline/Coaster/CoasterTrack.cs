using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Overworld.Cutscenes.Timeline.Coaster
{
	public class CoasterTrack : TrackAsset
	{
		protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
		{
			return base.CreatePlayable(graph, gameObject, clip);
		}
	}
}
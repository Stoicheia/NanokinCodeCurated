using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.UnityTimeline;

namespace Combat.Toolkit.Timeline
{
	[
		TrackColor(0.8f, 0.25f, 0.05f), // Bloody orange!
		TrackClipType(typeof(ActiveEntityPosClip)),
		TrackClipType(typeof(ArenaPosClip)),
		TrackClipType(typeof(CastPosClip)),
		TrackClipType(typeof(LuaPosClip))
	]
	public class LookPosTrack : TrackAsset
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var playableMixer = ScriptPlayable<PositionMixer>.Create(graph, inputCount);
			return playableMixer;
		}

		public override void GatherProperties(PlayableDirector director, [NotNull] IPropertyCollector driver)
		{
			base.GatherProperties(director, driver);

			driver.AddFromName<Transform>("position");
		}
	}
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


[
	TrackColor(0.7366781f, 0.3261246f, 0.8529412f),
	TrackClipType(typeof(TimeClip))
]
public class TimeTrack : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		ScriptPlayable<TimeTrackBehaviour> scriptPlayable = ScriptPlayable<TimeTrackBehaviour>.Create(graph, inputCount);

		TimeTrackBehaviour mixer = scriptPlayable.GetBehaviour();

		// This foreach will rename clips based on what they do, and collect the markers and put them into the mixer.
		// Since this happens when you enter Preview or Play mode,
		// the object holding the Timeline must be enabled or you won't see any change in names

		foreach (TimelineClip clip in GetClips())
		{
			var data = (TimeClip) clip.asset;
			clip.displayName = GetClipName(data, clip);
			mixer.Add(clip, data);
		}

		return scriptPlayable;
	}

	private static string GetClipName([NotNull] TimeClip clip, TimelineClip c)
	{
		switch (clip.EndAction)
		{
			case TimeClipBehaviour.EndActions.Loop:    return $"[{clip.Name}] ∞";
			case TimeClipBehaviour.EndActions.Nothing: return $"[{clip.Name}]";
			case TimeClipBehaviour.EndActions.Pause:   return $"[{clip.Name}] ■";
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
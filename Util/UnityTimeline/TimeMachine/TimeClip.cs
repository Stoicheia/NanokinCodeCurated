using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeClip : PlayableAsset, ITimelineClipAsset
{
	[HideInInspector] public TimeClipBehaviour Template = new TimeClipBehaviour();

	public string Name = "";

	[EnumToggleButtons, HideLabel]
	public TimeClipBehaviour.EndActions EndAction;

	public ClipCaps clipCaps => ClipCaps.None;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<TimeClipBehaviour> playable = ScriptPlayable<TimeClipBehaviour>.Create(graph, Template);

		TimeClipBehaviour data = playable.GetBehaviour();
		data.EndAction = EndAction;
		return playable;
	}
}
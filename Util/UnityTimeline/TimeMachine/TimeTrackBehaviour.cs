using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


public class TimeTrackBehaviour : PlayableBehaviour
{
	public readonly List<ClipEntry> clips = new List<ClipEntry>();

	public struct ClipEntry
	{
		public TimelineClip clip;
		public TimeClip     asset;
	}

	private PlayableDirector _director;
	private int              _current = -1;

	public override void OnPlayableCreate(Playable playable)
	{
		_director = playable.GetGraph().GetResolver() as PlayableDirector;
	}

	public void Add(TimelineClip clip, TimeClip data)
	{
		clips.Add(new ClipEntry
		{
			asset = data,
			clip  = clip
		});
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (!Application.isPlaying)
			return;

		int inputCount = playable.GetInputCount();

		for (int i = 0; i < inputCount; i++)
		{
			var               input = (ScriptPlayable<TimeClipBehaviour>) playable.GetInput(i);
			TimeClipBehaviour asset = input.GetBehaviour();

			float weight = playable.GetInputWeight(i);
			bool  active = weight > 0.01f;


			if (i == _current)
			{
				ClipEntry entry = clips[_current];
				double    start = entry.clip.start;
				double    end   = entry.clip.end;

				if (_director.time >= end || _director.time >= _director.duration)
				{
					switch (asset.EndAction)
					{
						case TimeClipBehaviour.EndActions.Nothing:
							break;

						case TimeClipBehaviour.EndActions.Pause:
							_director.Pause();
							_director.time = end;
							break;

						case TimeClipBehaviour.EndActions.Loop:
							_director.Play();
							_director.time = start;
							return;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			if (active)
			{
				_current = i;
			}
		}
	}
}
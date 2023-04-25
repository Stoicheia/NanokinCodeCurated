using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Cameras;
using Cysharp.Threading.Tasks;
using Overworld.Cutscenes.Timeline;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

public class TimeMarkerSystem : SerializedMonoBehaviour, INotificationReceiver
{
	public enum PauseMode {
		Stop,
		SetSpeedToZero
	}

	public PauseMode pauseMode;

	private PlayableDirector _director;
	private TimeMarker       _currentMarker;
	private TimeMarker       _pauseMarker;

	/// <summary>
	/// The target marker has been reached at least once.
	/// </summary>
	[Title("Debug")]
	[ShowInPlay]
	[NonSerialized] public bool reachedMarker;

	/// <summary>
	/// The target marker has looped at least once.
	/// </summary>
	[ShowInPlay]
	[NonSerialized] public bool hasLooped;

	[ShowInPlay]
	[NonSerialized] public bool reachedEnd;

	/// <summary>
	/// Which marker to play up to.
	/// Markers will be skipped until we reach the corresponding marker index.
	/// </summary>
	[ShowInPlay, NonSerialized]
	private int _targetIndex = -1;

	private List<TimeMarker> _markers;

	private bool _insideSkip = false;
	private bool _softPause  = false;

	private void Awake()
	{
		_director = GetComponent<PlayableDirector>();
		_director.stopped += director => {
			if (_insideSkip) return;
			_currentMarker = null;
			_pauseMarker   = null;
			_targetIndex   = -1;
			reachedMarker  = false;
			hasLooped      = false;
			reachedEnd     = true;
			_softPause     = false;
		};

		_director.played += director =>
		{
			if (_insideSkip) return;
			if (_currentMarker != null && _currentMarker.loops)
			{
				// Loop back to currentMarker.
				hasLooped      = true;
				_softPause     = false;
				_director.time = _currentMarker.time;
				HandlePause(_currentMarker);
			}
		};

		TimelineAsset timeline = _director.playableAsset as TimelineAsset;

		if(timeline && timeline.markerTrack != null) {
			_markers = timeline.markerTrack
							   .GetMarkers()
							   .OfType<TimeMarker>()
							   .OrderBy(m => m.time) // Yes, the markers are not already sorted by time. Yes, the playable api is a fucking clusterfuck.
							   .ToList();
		}
	}

	public void Reset()
	{
		_currentMarker = null;
		_pauseMarker   = null;
		_targetIndex   = -1;
		reachedMarker  = false;
		reachedEnd     = false;
		hasLooped      = false;
		_softPause     = false;
	}

	public void SetTargetMarker(int value)
	{
		reachedMarker = value <= _targetIndex; // If value > target, we'll have to play for a bit before it's reached
		_targetIndex  = value;
	}

	public void StepTargetMarker(int n = 1)
	{
		SetTargetMarker(_targetIndex + n);
	}

	public void SetTargetMarker(string name)
	{
		int marker = _markers.FindIndex(m => m.name == name);
		SetTargetMarker(marker);
	}

	public void OnNotify(Playable origin, INotification notification, object context)
	{
		if (!(notification is TimeMarker newMarker))
			return;

		int index = _markers.IndexOf(newMarker);
		if (index == _targetIndex)
		{
			// Reach the next marker.
			EnterNextMarker(newMarker);
			HandlePause(newMarker);
			reachedMarker = true;
		}
		else if (_currentMarker != null && _currentMarker.loops)
		{
			// Loop back to currentMarker.
			hasLooped      = true;
			_director.time = _currentMarker.time;
			HandlePause(_currentMarker);
		}
		else
		{
			// Simply pass this marker and continue on.
			EnterNextMarker(newMarker);
		}
	}

	private void EnterNextMarker(TimeMarker newMarker)
	{
		hasLooped      = false;
		reachedMarker  = false;
		_currentMarker = newMarker;
	}

	private void HandlePause(TimeMarker newMarker)
	{
		// Note: we have to set pauseMarker, because it will be triggered once more
		// once we resume. (which would pause endlessly and never allowing resuming!)
		if(newMarker.pauses) {
			if (_currentMarker != _pauseMarker) {
				if (pauseMode == PauseMode.Stop)
					_director.Pause();
				else {
					_director.time = newMarker.time;
					//_director.Evaluate();
					SpeedBasedPause();
				}

				_pauseMarker = _currentMarker;
			}
		} else {
			Resume();
			_pauseMarker = null;
		}
	}

	[Button]
	public void Resume(bool notSoft = false)
	{
		if(!notSoft && (_softPause || pauseMode == PauseMode.SetSpeedToZero))
			SpeedBasedResume();
		else
			_director.Resume();
	}

	void SpeedBasedPause()
	{
		_softPause = true;
		_director.playableGraph.GetRootPlayable(0).SetSpeed(0);
	}

	void SpeedBasedResume() => _director.playableGraph.GetRootPlayable(0).SetSpeed(1);

	[Button]
	public async UniTask SkipToEnd()
	{
		var asset = _director.playableAsset as TimelineAsset;
		if (asset == null) return;

		IEnumerable<TrackAsset> root_tracks = asset.GetRootTracks();
		List<IMarker>           all_markers = ListPool<IMarker>.Claim();

		foreach (var track in root_tracks) {
			all_markers.AddRange(track.GetMarkers());
		}

		_director.time = 0;

		_insideSkip = true;
		_director.Play();
		await UniTask.DelayFrame(1);
		_director.Stop();

		_insideSkip = false;

		_director.time = asset.duration - 1;
		_director.Play();
		await UniTask.DelayFrame(1);
		_director.Pause();

		await UniTask.DelayFrame(30);

		GameCams.Live.MessyHackToResetCinemachineBrainStack();

		ListPool<IMarker>.Release(all_markers);
	}
}
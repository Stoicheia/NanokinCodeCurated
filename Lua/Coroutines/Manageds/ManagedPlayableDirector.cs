using Anjin.Util;
using Overworld.Cutscenes;
using UnityEngine.Playables;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedPlayableDirector : CoroutineManaged
	{
		private readonly PlayableDirector _director;
		private readonly TimeMarkerSystem _timeMarkerSystem;

		private bool _looped;

		public ManagedPlayableDirector(PlayableDirector director)
		{
			_director = director;

			_timeMarkerSystem = _director.GetOrAddComponent<TimeMarkerSystem>();
		}

		// Note to self: DO NOT RELY ON PlayableDirector.state FOR ANYTHING!!!!!!!!!!
		// PlayableDirector.state changes to paused if you pause the editor's playmode.
		// Pinch yourself all you want, you ain't waking up from this one.
		public override bool Active => !_timeMarkerSystem.reachedMarker && !_timeMarkerSystem.hasLooped && !_timeMarkerSystem.reachedEnd;

		public override void OnEnd(bool forceStopped, bool wasSkipped = false)
		{
			if (wasSkipped) {
				BeginSkip();
				OnSkip();
			}
		}

		async void OnSkip()
		{
			await _timeMarkerSystem.SkipToEnd();
			EndSkip();
		}
	}
}
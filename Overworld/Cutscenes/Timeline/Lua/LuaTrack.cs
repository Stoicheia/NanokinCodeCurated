using Anjin.Util;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using UnityEditor.Timeline;

#endif

namespace Overworld.Cutscenes.Timeline
{
#if UNITY_EDITOR
	[CustomTimelineEditor(typeof(LuaTrack))]
	public class LuaTrackEditor : TrackEditor
	{
		public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
		{
			track.CreateCurves("FakeCurves");
			track.curves.SetCurve(string.Empty, typeof(GameObject), "m_FakeCurve", AnimationCurve.Linear(0, 1, 1, 1));
			base.OnCreate(track, copiedFrom);
		}
	}
#endif

	[
		TrackClipType(typeof(LuaCallClip)),
		TrackClipType(typeof(LuaCallMarker)),
		TrackBindingType(typeof(CoplayerTimelineSystem))
	]
	public class LuaTrack : MarkerTrack
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var director = go.GetComponent<PlayableDirector>();
			if (director)
			{
				var proxy = go.GetOrAddComponent<CoplayerTimelineSystem>();
				director.SetGenericBinding(this, proxy);
			}

			return base.CreateTrackMixer(graph, go, inputCount);
		}
	}
}
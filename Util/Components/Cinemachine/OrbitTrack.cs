using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Util.Components.Cinemachine
{
	[
		TrackColor(0.88627f, 0.64706f, 0), // nice warm yellow! None of that cursed disgusting soulless mustard yellow shit
		TrackBindingType(typeof(CinemachineOrbit)),
		TrackClipType(typeof(OrbitValueClip)),
		TrackClipType(typeof(OrbitExtensionPresetClip))
	]
	public class OrbitTrack : TrackAsset
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			return ScriptPlayable<OrbitExtensionMixer>.Create(graph, inputCount);
		}

		public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
		{
			base.GatherProperties(director, driver);

			driver.AddFromName<CinemachineOrbit>("coordinates.azimuth");
			driver.AddFromName<CinemachineOrbit>("coordinates.elevation");
			driver.AddFromName<CinemachineOrbit>("coordinates.distance");
		}
	}
}
using UnityEngine;
using UnityEngine.Playables;

namespace Util.Extensions {
	public static class Timeline {

		public static void SetNormalizedTime(this PlayableDirector director, float ntime)
		{
			if (director.state == PlayState.Paused) {
				director.Play();
				director.SetSpeedTo0();
			}

			director.time = Mathf.Clamp01(ntime) * (float)director.duration;
		}

		public static void SetTime(this PlayableDirector director, float time)
		{
			if (director.state == PlayState.Paused) {
				director.Play();
				director.SetSpeedTo0();
			}

			director.time = Mathf.Clamp(time, 0, (float)director.duration);
		}

		public static void SetSpeedTo0(this PlayableDirector director) => director.playableGraph.GetRootPlayable(0).SetSpeed(0);
		public static void SetSpeedTo1(this PlayableDirector director) => director.playableGraph.GetRootPlayable(0).SetSpeed(1);
	}
}
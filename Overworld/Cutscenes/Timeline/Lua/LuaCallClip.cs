using UnityEngine;
using UnityEngine.Playables;

namespace Overworld.Cutscenes.Timeline {

	public class LuaCallClip : PlayableAsset {

		public override double duration => 0;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			Debug.Log("Owner: " + owner);
			var playable = Playable.Create(graph, 0);
			return playable;
		}
	}
}
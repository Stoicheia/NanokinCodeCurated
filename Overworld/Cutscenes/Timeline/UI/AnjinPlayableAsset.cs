using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using Util.Odin.Attributes;

namespace Overworld.Cutscenes.Timeline.UI {
	public class AnjinPlayableAsset<T> : AnjinPlayableAssetBase where T: AnjinPlayableBehaviour, new()
	{
		[Inline]
		[Title("@$property.NiceName")]
		[SerializeField]
		public T Template = new T();

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			Template.clipStart = clipStart;
			return ScriptPlayable<T>.Create(graph, Template);
		}
	}
}
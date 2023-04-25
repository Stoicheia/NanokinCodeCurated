using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.Odin.Attributes;

namespace Util.Components.Cinemachine
{
	[Serializable]
	public class OrbitValueClip : PlayableAsset, ITimelineClipAsset
	{
		[SerializeField, Inline] private OrbitExtensionData template;

		public ClipCaps clipCaps { get; } = ClipCaps.Blending;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<OrbitExtensionData>.Create(graph, template);
		}
	}
}
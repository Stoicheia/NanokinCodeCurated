using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace Util.Components.Cinemachine
{
	[Serializable]
	public class OrbitExtensionPresetClip : PlayableAsset, ITimelineClipAsset
	{
		[FormerlySerializedAs("presetAsset"),SerializeField, Required] private OrbitExtensionAsset asset;
		[FormerlySerializedAs("offset"),SerializeField, Required]      private SphereCoordinate    value;

		public ClipCaps clipCaps { get; } = ClipCaps.Blending;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			ScriptPlayable<PresetData> playable = ScriptPlayable<PresetData>.Create(graph);

			if (asset == null)
			{
				return playable;
			}

			PresetData data = playable.GetBehaviour();
			data.asset = asset;
			data.value = value;

			return playable;
		}


		[Serializable]
		public class PresetData : OrbitExtensionData
		{
			[FormerlySerializedAs("presetAsset")]
			public OrbitExtensionAsset asset;
			[FormerlySerializedAs("offset")]
			public SphereCoordinate value;

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				SphereCoordinate v = value;

				if (asset != null)
					v += asset.Value;

				azimuth   = v.azimuth;
				elevation = v.elevation;
				distance  = v.distance;
			}
		}
	}
}
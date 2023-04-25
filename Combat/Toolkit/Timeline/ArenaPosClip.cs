using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.UnityTimeline;

namespace Combat.Toolkit.Timeline
{
	public class ArenaPosClip : PlayableAsset, ITimelineClipAsset, IArenaHolder
	{
		public ClipCaps clipCaps { get; } = ClipCaps.Blending;
		public Arena    Arena    { get; set; }

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var positionData = ScriptPlayable<Data>.Create(graph);

			positionData.GetBehaviour().Arena = Arena;

			return positionData;
		}

		public class Data : PositionData
		{
			private Arena     _arena;
			private Transform _arenaTransform;

			public Arena Arena
			{
				set
				{
					_arena          = value;
					_arenaTransform = _arena.transform;
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				if (_arena != null)
				{
					position = _arenaTransform.position;
				}
			}
		}
	}
}
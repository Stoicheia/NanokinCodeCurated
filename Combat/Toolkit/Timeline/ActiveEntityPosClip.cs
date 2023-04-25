using Combat.Features.TurnOrder.Events;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.UnityTimeline;

namespace Combat.Toolkit.Timeline
{
	public class ActiveEntityPosClip : PlayableAsset, ITimelineClipAsset
	{
		public BattleRunner runner;

		public ClipCaps clipCaps { get; } = ClipCaps.Blending;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var positionData = ScriptPlayable<Data>.Create(graph);

			positionData.GetBehaviour().runner = runner;

			// if (Application.isPlaying)
			// 	positionData.GetBehaviour().core = _composite.Resolve(graph.GetResolver());
			// else
			// 	positionData.GetBehaviour().placeholder = _placeholder.Resolve(graph.GetResolver());

			return positionData;
		}

		public class Data : PositionData
		{
			public BattleRunner runner;

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				if (runner == null)
					return;

				ITurnActer currentEvent = runner.battle.ActiveActer;

				if (currentEvent is Fighter fighter)
				{
					position = fighter.actor.transform.position;
				}
			}
		}
	}
}
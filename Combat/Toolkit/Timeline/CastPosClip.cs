using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.UnityTimeline;

namespace Combat.Toolkit.Timeline
{
	/// <summary>
	/// A clip which retrieves the centroid of entities involved in a skill cast.
	/// </summary>
	public class CastPosClip : PlayableAsset, ITimelineClipAsset, IBattleClip
	{
		[SerializeField] private ExposedReference<Transform> _placeholder;
		[SerializeField] private Data.Targets                _target;
		[SerializeField] private Data.TargetSelectors        _selector = Data.TargetSelectors.Centroid;

		public ClipCaps clipCaps { get; } = ClipCaps.Blending;

		public BattleRunner Runner { get; set; }

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var positionData = ScriptPlayable<Data>.Create(graph);

			if (Application.isPlaying)
			{
				positionData.GetBehaviour().target   = _target;
				positionData.GetBehaviour().selector = _selector;
				positionData.GetBehaviour().runner     = Runner;
			}
			else
			{
				positionData.GetBehaviour().placeholder = _placeholder.Resolve(graph.GetResolver());
			}

			return positionData;
		}

		public class Data : PositionData
		{
			public Transform  placeholder;
			public BattleRunner runner;

			public Targets         target = Targets.Caster;
			public TargetSelectors selector;

			private List<Vector3> _scratchPositions = new List<Vector3>();

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				if (runner == null)
				{
					// Use a placeholder for testing in editor.
					position = placeholder.position;
					return;
				}

				List<Vector3> entities = EvaluatePositions();
				position = EvaluateSelector(entities);
			}

			[NotNull]
			private List<Vector3> EvaluatePositions()
			{
				_scratchPositions.Clear();

				// CastInformation castInformation = composite.battle.ActiveSkillCast;
				//
				//
				// if ((target & Targets.Caster) != 0)
				// {
				// 	_scratchPositions.Add(castInformation.caster.view.transform.position);
				// }
				//
				// if ((target & Targets.CastTargets) != 0)
				// {
				// 	foreach (Target targetPick in castInformation.targeting.picks)
				// 	{
				// 		foreach (Fighter e in targetPick.fighters) _scratchPositions.Add(e.view.Center);
				// 		foreach (Battle.Slot e in targetPick.slots) _scratchPositions.Add(e.position);
				// 	}
				// }
				//
				// if ((target & Targets.CasterTeam) != 0)
				// {
				// 	Team team = composite.battle.GetTeam(castInformation.caster);
				//
				// 	if (team != null)
				// 	{
				// 		foreach (Fighter fighter in team.fighters)
				// 		{
				// 			_scratchPositions.Add(fighter.view.transform.position);
				// 		}
				// 	}
				// }
				//
				// if ((target & Targets.CastEffects) != 0)
				// {
				// 	// TODO Needs optimization..?
				// 	CombatEffect[] combatEffects = FindObjectsOfType<CombatEffect>();
				// 	foreach (CombatEffect combatEffect in combatEffects)
				// 	{
				// 		_scratchPositions.Add(combatEffect.transform.position);
				// 	}
				// }


				return _scratchPositions;
			}

			private Vector3 EvaluateSelector([NotNull] List<Vector3> positions)
			{
				switch (selector)
				{
					case TargetSelectors.First: return positions.First();
					case TargetSelectors.Last:  return positions.Last();
					case TargetSelectors.Centroid:
					{
						Vector3 centroid = Vector3.zero;

						if (positions.Count == 0)
							return centroid;

						foreach (Vector3 pos in positions)
						{
							centroid += pos;
						}

						return centroid / positions.Count;
					}

					default: throw new ArgumentOutOfRangeException();
				}
			}

			[Flags]
			public enum Targets
			{
				Caster      = 1 << 1,
				CasterTeam  = 1 << 2,
				CastTargets = 1 << 3,
				CastEffects = 1 << 4
			}

			public enum TargetSelectors
			{
				First,
				Last,
				Centroid
			}
		}
	}
}
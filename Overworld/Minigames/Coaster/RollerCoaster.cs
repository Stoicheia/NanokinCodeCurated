using System;
using System.Collections.Generic;
using Anjin.Util;
using Dreamteck.Splines;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	/// <summary>
	/// A set of coaster tracks.
	/// </summary>
	public class RollerCoaster : MonoBehaviour
	{
		public const string TRIGGER_FINISH = "FINISH";

		public bool               AutoDiscoverController = true;
		public ICoasterController Controller;

		public  List<CoasterTrack> AllTracks;
		private TrackJumpPoint[]   _jumpPoints;

		[NonSerialized, ShowInPlay]
		public Dictionary<CoasterTrack, List<TrackJumpPoint>> JumpPointsPerTrack;

		private void Awake()
		{
			if (AutoDiscoverController && Controller != null)
				Controller = GetComponent<ICoasterController>();

			_jumpPoints = GetComponentsInChildren<TrackJumpPoint>();

			JumpPointsPerTrack = new Dictionary<CoasterTrack, List<TrackJumpPoint>>();
			foreach (TrackJumpPoint point in _jumpPoints)
			{
				if (point.Track1 == null || point.Track2 == null) continue;

				JumpPointsPerTrack.AddToDictionaryContainingList(point.Track1, point);
			}

			foreach (CoasterTrack track in GetComponentsInChildren<CoasterTrack>()) {
				foreach(TriggerGroup group in track.Spline.triggerGroups) {
					foreach (SplineTrigger trigger in group.triggers) {
						trigger.AddListener((user) => {
							Controller?.OnCoasterTrigger(trigger.name, user);
						});
					}
				}
			}
		}
	}
}
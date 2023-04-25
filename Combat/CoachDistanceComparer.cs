using System.Collections.Generic;
using Anjin.Cameras;
using UnityEngine;

namespace Combat
{
	public class CoachDistanceComparer : IComparer<Coach>
	{
		public static CoachDistanceComparer instance => Singleton<CoachDistanceComparer>.Instance;

		public int Compare(Coach x, Coach y)
		{
			Vector3 cam = GameCams.Live.UnityCam.transform.position;

			float d1 = Vector3.Distance(cam, x.fighter.position);
			float d2 = Vector3.Distance(cam, y.fighter.position);

			return d1.CompareTo(d2);
		}
	}
}
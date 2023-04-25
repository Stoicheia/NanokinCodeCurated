using Drawing;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Components;

namespace Anjin.Minigames
{
	public class TrackJumpPoint : AnjinBehaviour
	{
		public enum Directions {None, Left, Right,}

		[EnumToggleButtons]
		public Directions Direction = Directions.None;

		public CoasterTrack Track1;
		public CoasterTrack Track2;

		public float Track1_Dist1;
		public float Track1_Dist2;

		public float Track2_Dist1;
		public float Track2_Dist2;

		//#if UNITY_EDITOR
		public override void DrawGizmos()
		{
			if (gameObject != UnityEditor.Selection.activeObject) return;

			if (Track1 == null || Track2 == null) return;

			var spline1 = Track1.Spline;
			var spline2 = Track2.Spline;

			Vector3 t1_a = spline1.EvaluatePosition(spline1.Travel(0, Track1_Dist1));
			Vector3 t1_b = spline1.EvaluatePosition(spline1.Travel(0, Track1_Dist2));
			Vector3 t2_a = spline2.EvaluatePosition(spline2.Travel(0, Track2_Dist1));
			Vector3 t2_b = spline2.EvaluatePosition(spline2.Travel(0, Track2_Dist2));

			using(Draw.WithColor(Color.red))
			{
				Draw.WireSphere(t1_a, 0.25f);
				Draw.WireSphere(t1_b, 0.25f);
				Draw.WireSphere(t2_a, 0.25f);
				Draw.WireSphere(t2_b, 0.25f);
			}

			for (int i = 0; i <= 10; i++)
			{
				float   t   = i / (float)10 * (Mathf.Abs(Track1_Dist1 - Track1_Dist2));
				Vector3 pos = spline1.EvaluatePosition(spline1.Travel(0, Track1_Dist1 + t));

				float   t2   = i / (float)10 * (Mathf.Abs(Track2_Dist1 - Track2_Dist2));
				Vector3 pos2 = spline2.EvaluatePosition(spline2.Travel(0, Track2_Dist1 + t2));

				//SplineSample sample = Track2.Project(pos);
				//Vector3      pos2   = Track2.EvaluatePosition(Track2.Travel(sample.percent, 2));
				//Vector3 pos2 = sample.position;

				Draw.Arrow(pos, pos2, Color.magenta);
			}
		}
		//#endif
	}
}
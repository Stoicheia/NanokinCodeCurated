using Drawing;
using Unity.Mathematics;
using UnityEngine;

namespace Util.Extensions {
	public static class ALINEExtensions {

		public static void Parabola(this CommandBuilder cb, Vector3 p0, Vector3 p1, float height) => cb.Parabola(p0, p1, height, Color.white);
		public static void Parabola(this CommandBuilder cb, Vector3 p0, Vector3 p1, float height, Color color, float t_start = 0, float t_end = 1, int segments = 20)
		{
			float3 prev = p0;
			bool   alt  = false;

			for (int i = 1; i <= segments; i++) {
				float t = Mathf.Lerp(t_start, t_end, i/(float)segments);

				float3 p = MathUtil.EvaluateParabola(p0, p1, height, t);
				cb.Line(prev, p, color);
				prev = p;
				alt  = !alt;
			}
		}

	}
}
using Anjin.Util;
using Drawing;
using UnityEngine;

namespace Util
{
	public class Draw2
	{
		public static void DrawTwoToneSphere(Vector3 position, float radius, Color foreground, Color? background = null)
		{
			if (background == null)
				background = Color.black;

			Gizmos.color = Color.black;
			Gizmos.DrawSphere(position, radius);

			Gizmos.color = foreground;
			Gizmos.DrawWireSphere(position, radius);
		}

		public static void DrawAngle(Vector3 position, float angle, Color lineColor, float length = 1f)
		{
			Gizmos.color = lineColor;
			Gizmos.DrawLine(position, position + Vector3.forward.Rotate(y: angle) * length);
		}

		public static void DrawLine(Vector3 a, Vector3 b, Color lineColor)
		{
			Gizmos.color = lineColor;
			Gizmos.DrawLine(a, b);
		}

		/// <summary>
		/// Draw a line from point A towards a direction B.
		/// The length of the line is the magnitude of B.
		/// </summary>
		public static void DrawLineTowards(Vector3 a, Vector3 towards, Color lineColor)
		{
			Gizmos.color = lineColor;
			Gizmos.DrawLine(a, a + towards);
		}

		public static void DrawBounds(Vector3 position, Bounds bounds, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawWireCube(position + bounds.center, bounds.size);
		}

		public static void DrawWireSphere(Vector3 pos, float radius, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawWireSphere(pos, radius);
		}

		public static void DrawSphere(Vector3 pos, float radius, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawSphere(pos, radius);
		}

		public static void Asterisk(Vector3 pos, float size)
		{
			float hsize = size * 0.875f;

			Vector3 up    = pos + Vector3.up * 0.75f;
			Vector3 down  = pos + Vector3.down * 0.75f;
			Vector3 left  = pos + Vector3.left;
			Vector3 right = pos + Vector3.right;
			Vector3 back  = pos + Vector3.back;
			Vector3 fwd   = pos + Vector3.forward;


			Draw.Line(up.normalized * size, down.normalized * size);
			Draw.Line((up + fwd).normalized * hsize, (down + back).normalized * hsize);
			Draw.Line((down + fwd).normalized * hsize, (up + back).normalized * hsize);
			Draw.Line((up + left).normalized * hsize, (down + right).normalized * hsize);
			Draw.Line((down + left).normalized * hsize, (up + right).normalized * hsize);
		}
	}
}
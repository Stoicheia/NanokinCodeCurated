using UnityEngine;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
#if UNITY_EDITOR
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;
#endif

namespace Anjin.Editor
{
	public class SimpleLayout
	{
		public Vector2 DrawPos;
		public Rect    rect;

		public void DoLabelWidth(string s, float width) => DoLabelWidth(s, width, GUI.skin.label);
		public void DoLabelWidth(string s, float width, GUIStyle style)
		{
			Vector2 size = style.CalcSize(new GUIContent(s));

			g.Label(this.GetRect(width, size.y), s, style);
		}

		public void DoLabel(string s) => DoLabel(s, GUI.skin.label);
		public void DoLabel(string s, GUIStyle style)
		{
			Vector2 size = style.CalcSize(new GUIContent(s));

			g.Label(this.GetRect(size.x, size.y), s, style);
		}
	}

	public static class SimpleLayoutExtentions
	{
		public static void SetupDrawRect(this SimpleLayout layout, float height)
		{
			layout.rect = GUILayoutUtility.GetRect(0, 10000, height, height);
		}

		public static void Begin(this SimpleLayout layout, float StartX, float StartY)
		{
			layout.DrawPos = new Vector2(StartX, StartY);
		}

		public static Rect GetRect(this SimpleLayout layout, float w, float h = 16)
		{
			Rect r = new Rect(layout.rect.x + layout.DrawPos.x, layout.rect.y + layout.DrawPos.y, w, h);
			layout.DrawPos.x += w;

			return r;
		}

		public static Rect GetRectStretch(this SimpleLayout layout, float h = 16)
		{
			float width = layout.rect.width - layout.DrawPos.x - 4;
			Rect  r     = layout.GetRect(width, h);
			return r;
		}

		public static void NewLine(this SimpleLayout layout, float height = 16)
		{
			layout.DrawPos.Set(4, layout.DrawPos.y + height);
		}

		public static void HSpace(this SimpleLayout layout, float size)
		{
			layout.DrawPos.x += size;
		}


	}

}
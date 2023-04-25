using System;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;

#endif

/// <summary>
/// Draw the properties with a darker background and
/// borders, optionally.
/// </summary>
public class DarkBox : Attribute
{
	/// <summary>
	/// Dark
	/// </summary>
	public readonly bool withBorders;

	public DarkBox() { }

	public DarkBox(bool withBorders)
	{
		this.withBorders = withBorders;
	}
}

public class ColorBox : Attribute
{
	public readonly float opacity;

	public ColorBox(float opacity = 0.15f)
	{
		this.opacity = opacity;
	}
}

namespace OdinExtensions
{
#if UNITY_EDITOR
	[UsedImplicitly]
	[DrawerPriority(0, 99)]
	public class ColorBoxDrawer : OdinAttributeDrawer<ColorBox>
	{
		private const float GOLDEN_RATIO = 0.618033988749895f;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			int namehash = Property.ValueEntry.TypeOfValue.Name.GetHashCode();

			var hue = (float) ((namehash + (double) int.MaxValue) / uint.MaxValue);

			Color col = Color.Lerp(
				DarkBoxDrawer.Color,
				Color.HSVToRGB(hue, 0.95f, 0.75f),
				Attribute.opacity);

			BoxGUI.BeginBox(col);
			CallNextDrawer(label);
			BoxGUI.EndBox();
		}
	}

	[UsedImplicitly]
	[DrawerPriority(0, 99)]
	public class DarkBoxDrawer : OdinAttributeDrawer<DarkBox>
	{
		public static readonly Color Color = EditorGUIUtility.isProSkin
			? Color.Lerp(Color.black, Color.white, 0.1f)
			: Color.gray;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			BoxGUI.BeginBox(new Color(0, 0, 0, 0.15f));
			CallNextDrawer(label);

			// ReSharper disable once RedundantCast
			BoxGUI.EndBox(Attribute.withBorders ? Color : (Color?) null);
		}
	}

	internal static class BoxGUI
	{
		private static Rect currentLayoutRect;

		public static void BeginBox(Color color)
		{
			currentLayoutRect = EditorGUILayout.BeginVertical(SirenixGUIStyles.None);

			// Rect currentLayoutRect = GUIHelper.GetCurrentLayoutRect();
			if (Event.current.type == EventType.Repaint)
			{
				SirenixEditorGUI.DrawSolidRect(currentLayoutRect, color);
			}
		}

		public static void EndBox(Color? borders = null)
		{
			EditorGUILayout.EndVertical();

			if (Event.current.type == EventType.Repaint && borders != null)
			{
				SirenixEditorGUI.DrawBorders(currentLayoutRect, 1, 1, 1, 1, borders.Value);
			}

			GUILayout.Space(1);
		}
	}
#endif
}
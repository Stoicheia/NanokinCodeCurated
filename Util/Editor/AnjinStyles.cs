using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Anjin.Editor
{
	public static class AnjinStyles
	{
		private static Font _liberationMono;
		public static Font LiberationMono
		{
			get
			{
				if (_liberationMono == null)
				{
					_liberationMono = Resources.Load<Font>("UI/Font/LIBERATIONMONO-BOLD");
				}
				return _liberationMono;
			}
		}

		private static GUISkin _nanokinPixelSkin;
		public static GUISkin NanokinPixelSkin
		{
			get
			{
				if (_nanokinPixelSkin == null)
					_nanokinPixelSkin = Resources.Load<GUISkin>("UI/Debug GUI Skin");
				return _nanokinPixelSkin;
			}
		}

		private static GUIStyle _boxContainer;
		public static GUIStyle BoxContainer
		{
			get
			{
				if (_boxContainer == null)
					_boxContainer = new GUIStyle(EditorStyles.helpBox)
					{
						margin = new RectOffset(0, 0, 0, 0)
					};
				return _boxContainer;
			}
		}

		private static GUIStyle _boxContainerNoPadding;

		public static GUIStyle BoxContainerNoPadding
		{
			get
			{
				if (_boxContainerNoPadding == null)
					_boxContainerNoPadding = new GUIStyle(BoxContainer)
					{
						padding = new RectOffset(-4, -4, -4, -4),
						clipping = TextClipping.Overflow,

					};
				return _boxContainerNoPadding;
			}
		}

		static GUIStyle _miniLabelWordWrap;

		public static GUIStyle MiniLabelWordWrap
		{
			get
			{
				if (_miniLabelWordWrap == null)
					_miniLabelWordWrap = new GUIStyle(EditorStyles.whiteMiniLabel)
					{
						wordWrap = true,
						clipping = TextClipping.Overflow

					};
				return _miniLabelWordWrap;
			}
		}

		static GUIStyle _boldButton;

		public static GUIStyle BoldButton
		{
			get
			{
				if (_boldButton == null)
					_boldButton = new GUIStyle(SirenixGUIStyles.Button)
					{
						fontStyle = FontStyle.Bold,

					};
				return _boldButton;
			}
		}

	}
}
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Anjin.EditorUtility
{
	public static class AnjinEditorIcons
	{
		static Texture2D _ActorTagOff;
		static Texture2D _ActorTagOn;
		static Texture2D _ActorTagHover;

		static Texture2D _ExpandArrowOff;
		static Texture2D _ExpandArrowOn;

		static Texture2D _BubbleIcon;
		static Texture2D _CancelBubbleIcon;
		static Texture2D _RefreshIcon;
		static Texture2D _PlusIcon;


		public static Texture2D ActorTagOff { get { if (_ActorTagOff == null) init(); return _ActorTagOff; } }
		public static Texture2D ActorTagOn { get { if (_ActorTagOn == null) init(); return _ActorTagOn; } }
		public static Texture2D ActorTagHover { get { if (_ActorTagHover == null) init(); return _ActorTagHover; } }

		public static Texture2D ExpandArrowOff
		{
			get
			{
				if (_ExpandArrowOff == null) init();
				return _ExpandArrowOff;
			}
		}

		public static Texture2D ExpandArrowOn
		{
			get
			{
				if (_ExpandArrowOn == null) init();
				return _ExpandArrowOn;
			}
		}

		public static Texture2D BubbleIcon 			{ get { if (_BubbleIcon 		== null) init(); return _BubbleIcon; 	   } }
		public static Texture2D CancelBubbleIcon 	{ get { if (_CancelBubbleIcon 	== null) init(); return _CancelBubbleIcon; } }
		public static Texture2D RefreshIcon 		{ get { if (_RefreshIcon 		== null) init(); return _RefreshIcon; 	   } }
		public static Texture2D PlusIcon 			{ get { if (_PlusIcon 			== null) init(); return _PlusIcon; 		   } }

		private static GUIStyle _ActorTagButtonStyle;
		public static GUIStyle ActorTagButtonStyle
		{ get { if (_ActorTagButtonStyle == null) init(); return _ActorTagButtonStyle; } }

		private static GUIStyle _ExpandArrowOffStyle;
		public static GUIStyle ExpandArrowOffStyle
		{ get { if (_ExpandArrowOffStyle == null) init(); return _ExpandArrowOffStyle; } }

		private static GUIStyle _ExpandArrowOnStyle;
		public static GUIStyle ExpandArrowOnStyle
		{ get { if (_ExpandArrowOnStyle == null) init(); return _ExpandArrowOnStyle; } }

		static void init()
		{
			_ActorTagOff 		= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/General/tag_button_off.png");
			_ActorTagOn			= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/General/tag_button_on.png");
			_ActorTagHover 		= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/General/tag_button_hover.png");

			_ExpandArrowOff 	= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/General/expand_arrow_off.png");
			_ExpandArrowOn 		= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/General/expand_arrow_on.png");


			_BubbleIcon 		= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/Textboxes/bubble_icon.png");
			_CancelBubbleIcon 	= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/Textboxes/bubble_icon_cancel.png");
			_RefreshIcon 		= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/Textboxes/refresh_icon.png");
			_PlusIcon 			= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/Textboxes/plus_icon.png");

			_ActorTagButtonStyle = new GUIStyle(GUI.skin.button);

			_ExpandArrowOffStyle = new GUIStyle();
			_ExpandArrowOffStyle.normal.background = _ExpandArrowOff;

			_ExpandArrowOnStyle                   = new GUIStyle();
			_ExpandArrowOnStyle.normal.background = _ExpandArrowOn;

			ActorTagButtonStyle.normal.background 	= _ActorTagOff;
			ActorTagButtonStyle.active.background 	= _ActorTagOn;
			ActorTagButtonStyle.hover.background 	= _ActorTagHover;

			ActorTagButtonStyle.stretchWidth  = true;
			ActorTagButtonStyle.stretchHeight = false;
		}
	}
}
#endif
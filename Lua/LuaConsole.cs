using System;
using System.Text;
using Anjin.Nanokin;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;

namespace Anjin.Scripting
{
	public class LuaConsole : StaticBoy<LuaConsole>
	{
		const int MAX_INPUT_LENGTH = 1024;

		public bool Enabled = false;

		[HideInEditorMode, ShowInInspector]
		public static bool Open;
		public GUISkin Skin;
		public StringBuilder sb;

		public int textSize = 48;

		public int caretPos;

		//[HideInEditorMode, ShowInInspector]
		//public string current => sb.ToString();

		public GUIStyle textStyle;

		void Awake()
		{
			sb = new StringBuilder();
			Open = false;
			Keyboard.current.onTextInput += OnKeyboardInput;

			textStyle = new GUIStyle(Skin != null ? Skin.GetStyle("RegularLabel") : GUI.skin.label);
			textStyle.fontSize = textSize;
			caretPos = 0;
		}

		void OnDestroy()
		{
			Keyboard.current.onTextInput -= OnKeyboardInput;
		}

		void Update()
		{
			if (!Enabled) return;

			if (GameInputs.IsPressed(Key.Backquote)) {
				Open = !Open;
			}

			if (Open) {
				if(GameInputs.IsPressed(Key.Enter))
					ExecuteBuffer();
				else if (GameInputs.IsPressed(Key.RightArrow))
					caretPos++;
				else if (GameInputs.IsPressed(Key.LeftArrow))
					caretPos--;
			}

			caretPos = Mathf.Clamp(caretPos, 0, sb.Length);
		}

		void ExecuteBuffer()
		{
			if (!enabled || sb.Length == 0) return;

			try {
				Lua.envScript.DoString(sb.ToString());
			} catch (Exception e) {
				Console.WriteLine(e);
			}

			sb.Clear();
		}

		void OnKeyboardInput(char c)
		{
			if (!Open) return;

			var ascii = Convert.ToInt32(c);
			//Valid ascii chars
			if (ascii >= 32 && ascii < 126 && sb.Length < MAX_INPUT_LENGTH) {
				sb.Append(c);
				caretPos++;
			}

			if (ascii == 8) //Backspace
			{
				sb.Remove(sb.Length - 1, 1);
				caretPos--;
			}
			//Debug.Log(c + " " + Convert.ToInt32(c));
		}

		void OnGUI()
		{
			if (!Enabled || !Open) return;

			var prevSkin = g.skin; g.skin = Skin;

			glo.BeginArea(new Rect(0,0,Screen.width, Screen.height));
			{
				g.Box(new Rect(0, 0, Screen.width, 64), GUIContent.none);
				g.Label(new Rect(8, 16, Screen.width, 24), sb.ToString(), textStyle);
				GUIDrawRect(new Rect(8 + caretPos * 18, 48, 16, 4), Color.black);
			}
			glo.EndArea();

			g.skin = prevSkin;
		}


		private static Texture2D _staticRectTexture;
		private static GUIStyle  _staticRectStyle;

		// Note that this function is only meant to be called from OnGUI() functions.
		public static void GUIDrawRect( Rect position, Color color )
		{
			if ( _staticRectTexture == null ) _staticRectTexture = new Texture2D( 1, 1 );

			if ( _staticRectStyle == null ) _staticRectStyle = new GUIStyle();

			_staticRectTexture.SetPixel( 0, 0, color );
			_staticRectTexture.Apply();

			_staticRectStyle.normal.background = _staticRectTexture;

			GUI.Box( position, GUIContent.none, _staticRectStyle );
		}
	}

}

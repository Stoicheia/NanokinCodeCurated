using System;
using System.Collections.Generic;
using Anjin.EventSystemNS;
using Anjin.Scripting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;

#endif

public class LuaAsset : ScriptableObject
{
	/// <summary>
	/// Raw text, with custom Lua syntax
	/// </summary>
	[SerializeField]
	private string text;

	public string Text => text;

	/// <summary>
	/// Cached transpiled Lua
	/// text is very light so we can afford
	/// to waste a bit of memory so we don't
	/// have to transpile in real-time
	/// </summary>
	[SerializeField]
	private string transpiledText;

	public string TranspiledText => transpiledText;

	/// <summary>
	/// Path relative to Assets/
	/// </summary>
	public string Path;

	public bool   ErrorOnImport;
	public string ErrorMessage;

	[NonSerialized]
	public List<string> functions = new List<string>();

	public void UpdateText(string text)
	{
		this.text = text;

		/*#if UNITY_EDITOR
		Debug.Log($"{name}: UpdateText(): \n {text}");
		#endif
		*/

		// This is still imperceptibly fast when running on a script or two, but it is becoming non-trivial
		// and as the number of scripts and imported libraries keep growing,
		// performances will only worsen without caching like this.
		transpiledText = LuaUtil.TranspileToLua(text);
	}


	// public static readonly Regex ColonObjectRegex      = new Regex(@": ", RegexOptions.Multiline | RegexOptions.Compiled);
	// public static readonly Regex LambdaRegex           = new Regex(@"\|\w+\|\s.+", RegexOptions.Multiline | RegexOptions.Compiled);

#if UNITY_EDITOR

	[MenuItem("Assets/Create/Anjin/Lua Script")]
	public static void CreateNewLuaFile()
	{
		/*string 	path 		= "Assets/Resources/Scripts";
			var 	selected 	= Selection.activeObject;

			if (selected != null)
				path = AssetDatabase.GetAssetPath(selected);

			path = path + "/script.lua";

			File.CreateText(path);

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, null, path, null, "");
			*/

		//ProjectWindowUtil.StartNameEditingIfProjectWindowExists();

		//Debug.Log(Selection.activeObject);
		//Debug.Log(AssetDatabase.GetAssetPath(Selection.activeObject));
	}

	public override string ToString()
	{
		return $"LuaAsset({name}): {text}";
	}

#endif
}

namespace Anjin.Scripting
{
#if UNITY_EDITOR

	[CustomEditor(typeof(LuaAsset))]
	[CanEditMultipleObjects]
	public class LuaAssetEditor : OdinEditor
	{
		private const int CODE_MAX_CHARS = 30000;

		private CodeDisplays _codeDisplay = CodeDisplays.Anjin;

		[NonSerialized]
		private GUIStyle TextStyle;

		public override void OnInspectorGUI()
		{
			if (TextStyle == null)
				TextStyle = "ScriptText";

			bool tmp_enable = GUI.enabled;
			GUI.enabled = true;

			LuaAsset asset = target as LuaAsset;
			if (asset == null)
				return;

			// PATH
			// ----------------------------------------
			GUILayout.Label(asset.Path);

			// ERRORS
			// ----------------------------------------
			if (asset.ErrorOnImport)
			{
				EditorGUILayout.Space();
				GUIStyle style = EventStyles.GetLabelWithColor(Color.red);
				GUILayout.Label("Script failed to run:", style);
				GUILayout.Label(asset.ErrorMessage, style);
				EditorGUILayout.Space();
			}
			else
			{
				// FUNCTIONS
				// ----------------------------------------
				EditorGUILayout.Space();

				if (asset.functions != null)
				{
					GUILayout.Label("Functions:", EditorStyles.boldLabel);

					if (asset.functions.Count == 0)
						GUILayout.Label("none");
					else
						for (int i = 0; i < asset.functions.Count; i++)
						{
							GUILayout.Label($"{i + 1}. {asset.functions[i]}");
						}

					EditorGUILayout.Space();
				}
			}

			// CODE
			// ----------------------------------------
			EditorGUILayout.BeginHorizontal();
			if (EditorGUILayout.ToggleLeft("Anjin", _codeDisplay == CodeDisplays.Anjin)) _codeDisplay           = CodeDisplays.Anjin;
			if (EditorGUILayout.ToggleLeft("Transpiled", _codeDisplay == CodeDisplays.Transpiled)) _codeDisplay = CodeDisplays.Transpiled;

			EditorGUILayout.EndHorizontal();

			var code = "";

			switch (_codeDisplay)
			{
				case CodeDisplays.Anjin:
					code = GetLimitedCodeString(asset.Text);
					break;

				case CodeDisplays.Transpiled:
					code = GetLimitedCodeString(asset.TranspiledText);
					break;
			}

			Rect rect = GUILayoutUtility.GetRect(new GUIContent(code), TextStyle);
			rect.x     = 0.0f;
			rect.width = rect.width + 16;
			GUI.Box(rect, code, TextStyle);

			GUI.enabled = tmp_enable;
		}

		private string GetLimitedCodeString(string code)
		{
			string str = "";
			if (targets.Length == 1)
			{
				str = code;
				if (str.Length > CODE_MAX_CHARS)
					str = str.Substring(0, CODE_MAX_CHARS) + "...\n\n<...etc...>";
			}

			return str;
		}

		private enum CodeDisplays
		{
			Anjin,
			Transpiled,
		}
	}

#endif
}
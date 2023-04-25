using System;
using System.Collections.Generic;
using Anjin.Nanokin.ParkAI;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif
using UnityEngine;

namespace Overworld.Rendering
{
	[CreateAssetMenu(fileName = "Anjin/Color Replacement Profile")]
	public class ColorReplacementProfile : SerializedScriptableObject
	{
		public string   ScriptID;
		public PeepRace Race = PeepRace.Generic;

		[ReadOnly]
		public string ID = DataUtil.MakeShortID(9); // Used for hash comparisons, DO NOT REMOVE

		[ListDrawerSettings(CustomAddFunction = "AddCol", Expanded = true, OnTitleBarGUI = "OnTitleGUI")]
		public List<ColReplace> colArray = new List<ColReplace>();

		public void AddCol()
		{
			if (colArray.Count < ColorReplacementSetter.MAX_REPLACEMENT_COLORS)
				colArray.Add(new ColReplace(Color.white, Color.white, 1));
		}

		void OnTitleGUI()
		{
#if UNITY_EDITOR

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(SirenixGUIStyles.BoxContainer);
			GUILayout.FlexibleSpace();
			GUILayout.Label("Original");
			GUILayout.FlexibleSpace();
			GUILayout.Label("Replacement");
			GUILayout.FlexibleSpace();
			GUILayout.Label("Blend");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
#endif
		}

		private void OnValidate()
		{
			if(ID == null) ID = DataUtil.MakeShortID(9);
		}
	}


	public struct ColReplace
	{
		[HorizontalGroup("Colors"), HideLabel]
		public Color from;

		[HorizontalGroup("Colors"), HideLabel]
		public Color to;

		[HorizontalGroup("Colors"), HideLabel]
		public float blend;

		public ColReplace(Color from, Color to, float blend)
		{
			this.from = from;
			this.to   = to;
			this.blend = blend;
		}
	}
}
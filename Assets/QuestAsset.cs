using System;
using System.Collections.Generic;
using Anjin.Editor;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Assets
{
	[Serializable, LuaUserdata]
	public class QuestAsset : SerializedScriptableObject, IAddressable
	{
		[Title("Config")]
		public string 		ID;
		public LuaAsset 	QuestScript;

		/*[HideLabel]
		[BoxGroupExt("Starting Phase", 0.0f, 1f, 1f)]
		public Phase StartingPhase 	= new Phase();

		[Space(16)]
		[Title("Main Phases")]*/
		[Space(16)]
		public List<Phase> 	Phases			= new List<Phase>();

		[Space(16)]
		[Title("Display")]
		[FormerlySerializedAs("Name")]
		public string DisplayName;
		public AssetReferenceSprite Sprite;

		[Space]
		public string Description;
		public List<Objective> Objectives;

		[Space]
		public LevelManifest Level;

		public string Address { get; set; }

		[Serializable]
		public class Phase
		{
			//[Title("@$property.Parent.NiceName")]
			public string 			ID;

			[HorizontalGroup("Show",Title = "Show:"), ToggleButton, LabelText("Name")]
			public bool 			ShowName;
			[HorizontalGroup("Show"), ToggleButton, LabelText("Description")]
			public bool 			ShowDescription;

			[ShowIf("$ShowName")]
			public GameText 		Name;

			[GameTextMultiLine][ShowIf("$ShowDescription")]
			public GameText 		Description;

			public List<Objective> 	Objectives 		= new List<Objective>();
		}

		[Serializable, InlineProperty, HideLabel]
		public class Objective
		{
			public string 	ID;
			public GameText Name;
			public GameText Description;
			[MinValue(1)]
			public int 		Quantity = 1;
		}
	}
}
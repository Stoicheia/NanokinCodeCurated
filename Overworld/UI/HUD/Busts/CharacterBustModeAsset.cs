using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.UI {
	public class CharacterBustModeAsset : SerializedScriptableObject {

		[Title("States")]
		public BustState Neutral;

		[Space(24)]
		public List<BustState> States = new List<BustState>();
	}
}
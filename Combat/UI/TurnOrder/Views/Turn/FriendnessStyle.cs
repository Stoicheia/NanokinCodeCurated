using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.UI.TurnOrder
{
	[Serializable]
	public struct FriendnessStyle
	{
		public                                       Color lightColor;
		[FormerlySerializedAs("spriteColor")] public Color contentColor;
		public                                       Color frameColor;
		public                                       Color textColor;
		public                                       Color effectColor;
	}
}
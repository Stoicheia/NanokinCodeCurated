using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Data.Combat
{
	[Serializable]
	public class GrowingPoints
	{
		[FormerlySerializedAs("_hp"), SerializeField, Inline(true), GrowingCurvedValue(StatConstants.MAX_HP), LabelWidth(72f)]
		public GrowingCurvedValue HP = new GrowingCurvedValue();

		[FormerlySerializedAs("_sp"), SerializeField, Inline(true), GrowingCurvedValue(StatConstants.MAX_SP), LabelWidth(72f)]
		public GrowingCurvedValue SP = new GrowingCurvedValue();

		public void Randomise()
		{
			SP.Randomise();
			HP.Randomise();
		}
	}
}
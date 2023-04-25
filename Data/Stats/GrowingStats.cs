using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Data.Combat
{
	[Serializable]
	public class GrowingStats
	{
		[FormerlySerializedAs("_power"), SerializeField, Inline(true), GrowingCurvedValue(StatConstants.MAX_STAT), LabelWidth(72f)]
		public GrowingCurvedValue Power = new GrowingCurvedValue();

		[FormerlySerializedAs("_speed"), SerializeField, Inline(true), GrowingCurvedValue(StatConstants.MAX_STAT), LabelWidth(72f)]
		public GrowingCurvedValue Speed = new GrowingCurvedValue();

		[FormerlySerializedAs("_willpower"), SerializeField, Inline(true), GrowingCurvedValue(StatConstants.MAX_STAT), LabelWidth(72f)]
		public GrowingCurvedValue Will = new GrowingCurvedValue();

		[SerializeField]
		public int[] AP = {1, 1}; // 2 ap at level 1

		public void Randomise()
		{
			Power.Randomise();
			Speed.Randomise();
			Will.Randomise();
		}
	}
}
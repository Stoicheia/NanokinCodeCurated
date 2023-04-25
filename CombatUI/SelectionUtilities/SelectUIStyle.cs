using System;
using UnityEngine;

namespace Combat.Components
{
	[Serializable]
	public struct SelectUIStyle
	{
		[SerializeField] public float NormalBrightness;
		[SerializeField] public float HighlightBrightness;
		[SerializeField] public float DimBrightness;

		public static SelectUIStyle Default => new SelectUIStyle
		{
			NormalBrightness    = 1,
			HighlightBrightness = 1
		};
	}
}
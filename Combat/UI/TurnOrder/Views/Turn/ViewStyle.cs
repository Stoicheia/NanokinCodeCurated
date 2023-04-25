using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Combat.UI.TurnOrder
{
	[Serializable]
	public struct ViewStyle
	{
		public static ViewStyle Default => new ViewStyle
		{
			scale   = 1,
			opacity = 1
		};

		public Vector2 offset;
		public float   rotation;

		[Range(0, 2)]
		public float   scale;

		[Range(0, 1), FormerlySerializedAs("contentShading")]
		public float shading;

		[Range(0, 1)]
		public float opacity;

		public bool condense;

		public FloatRange scaling_rng;
		public FloatRange rotation_rng;

		public FloatRange offset_x_rng;
		public FloatRange offset_y_rng;
	}
}
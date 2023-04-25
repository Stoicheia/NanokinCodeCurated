using System;
using UnityEngine;
using UnityUtilities;

namespace Util.UnityEditor.Odin.Attributes {

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class ColoredBackAttribute : Attribute {

		public Color Color;
		public string GetColor;

		/// <summary>Sets the GUI color for the property.</summary>
		public ColoredBackAttribute(float h, float s, float v, float a = 1f) => Color = Color.HSVToRGB(h, s, v).Alpha(a);

		/// <summary>Sets the GUI color for the property.</summary>
		/// <param name="getColor">Specify the name of a local field, member or property that returns a Color.</param>
		public ColoredBackAttribute(string getColor) => GetColor = getColor;
	}
}
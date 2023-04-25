using System;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Util.Odin.Attributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	public class BoxGroupExtAttribute : BoxGroupAttribute
	{
		public Color Tint;
		public bool  Foldable;

		public BoxGroupExtAttribute(string group, bool showLabel = true, bool centerLabel = false, int order = 0, bool foldable = true) : base(
			group, showLabel, centerLabel, order)
		{
			Tint     = Color.white;
			Foldable = foldable;
		}

		public BoxGroupExtAttribute(string group, float h, float s, float v, bool showLabel = true, bool centerLabel = false, int order = 0, bool foldable = true) : base(group, showLabel,centerLabel, order)
		{
			Tint     = Color.HSVToRGB(h, s, v);
			Foldable = foldable;
		}

		public BoxGroupExtAttribute() : this("_DefaultBoxGroup") { }
	}
}

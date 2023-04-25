using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Editor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ToggleButtonAttribute : Attribute
	{
		public int ButtonHeight;

		public Color color;
		public Color active_color;

		public ToggleButtonAttribute()
		{
			ButtonHeight = 0;
		}

		public ToggleButtonAttribute(float h = -1, float s = -1, float v = -1, float active_h = -1, float active_s = -1, float active_v = -1) : this()
		{
			color        = Color.HSVToRGB(h > 0 ? h : 1, s > 0 ? s : 0, v > 0 ? v : 1);
			active_color = Color.HSVToRGB(active_h > 0 ? active_h : 1, active_s > 0 ? active_s : 0, active_v > 0 ? active_v : 1);
		}

		public ToggleButtonAttribute(ButtonSizes size)
		{
			ButtonHeight = (int)size;
		}

		public ToggleButtonAttribute(int buttonSize)
		{
			ButtonHeight = buttonSize;
		}
	}
}
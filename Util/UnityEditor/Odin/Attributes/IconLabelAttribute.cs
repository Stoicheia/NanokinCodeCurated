using System;

namespace Util.Odin.Attributes {
	[AttributeUsage(AttributeTargets.All)]
	public class IconLabelAttribute : Attribute
	{
		public readonly Icons icon;
		public readonly bool  keepLabel;

		public IconLabelAttribute(Icons icon, bool keepLabel = true)
		{
			this.icon      = icon;
			this.keepLabel = keepLabel;
		}
	}
}
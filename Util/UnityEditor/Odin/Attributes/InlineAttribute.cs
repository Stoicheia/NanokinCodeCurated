using System;

namespace Util.Odin.Attributes
{
	[AttributeUsage(AttributeTargets.All)]
	public class InlineAttribute : Attribute
	{
		public readonly  bool keepLabel;
		public readonly  bool asGroup;
		private readonly bool parentLabel;

		public InlineAttribute(
			bool keepLabel   = false,
			bool asGroup     = false,
			bool parentLabel = false)
		{
			this.keepLabel   = keepLabel;
			this.asGroup     = asGroup;
			this.parentLabel = parentLabel;
		}
	}
}
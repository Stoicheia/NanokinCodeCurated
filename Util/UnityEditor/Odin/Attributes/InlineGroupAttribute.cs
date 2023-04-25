using System;

namespace Util.Odin.Attributes
{
	[AttributeUsage(AttributeTargets.All)]
	public class InlineGroupAttribute : InlineAttribute
	{
		public InlineGroupAttribute() : base(asGroup: true)
		{

		}
	}
}
using System;

namespace Util.Odin.Attributes
{
	public class BasePathAttribute : Attribute
	{
		public readonly string basePath;

		public BasePathAttribute(string basePath)
		{
			this.basePath = basePath;
		}
	}
}
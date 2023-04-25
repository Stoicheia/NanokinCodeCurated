using System;

namespace Util
{
	/// <summary>
	/// Remove a prefix from the label string
	/// </summary>
	public class HidePrefix : Attribute
	{
		public string Prefix { get; }

		public HidePrefix(string prefix)
		{
			Prefix = $"{prefix} ";
		}
	}
}
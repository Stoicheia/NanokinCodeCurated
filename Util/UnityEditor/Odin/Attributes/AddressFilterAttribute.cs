using System;

namespace Util.Odin.Attributes
{
	public class AddressFilterAttribute : Attribute
	{
		public string Prefix   { get; }
		public string Suffix   { get; }
		public string Exclude  { get; }
		public string Contains { get; }

		public AddressFilterAttribute(string prefix   = null,
			string                           suffix   = null,
			string                           exclude  = null,
			string                           contains = null
		)
		{
			Prefix   = prefix;
			Suffix   = suffix;
			Exclude  = exclude;
			Contains = contains;
		}

		public bool AddressMatches(string address)
		{
			if (Prefix != null && !address.StartsWith(Prefix)) return false;
			if (Suffix != null && !address.EndsWith(Suffix)) return false;
			if (Exclude != null && address.Contains(Exclude)) return false;
			if (Contains != null && !address.Contains(Contains)) return false;

			return true;
		}
	}
}
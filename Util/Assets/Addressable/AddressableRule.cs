using System;

namespace Util.Addressable
{
	public struct AddressableRule
	{
		public Type     type;
		public string   pattern;
		public string   group;
		public string   label;
		public string[] labels;

		/// <summary>
		/// Only assign an auto-address if the asset has no address yet.
		/// </summary>
		public bool init_only;

		/// <summary>
		/// A regex test on the asset path to omit
		/// it from auto-addressing.
		/// </summary>
		public string except_path;

		/// <summary>
		/// A regex test on the asset path to include
		/// it into auto-addressing.
		/// </summary>
		public string matches_path;
		public string remove_suffix;


		// public AddressableRule(string pattern, string group)
		// {
		// 	this.pattern = pattern;
		// 	this.@group  = @group;
		// 	labels       = null;
		// }
		//
		// public AddressableRule(string pattern, string group, params string[] labels)
		// {
		// 	this.pattern = pattern;
		// 	this.@group  = @group;
		// 	this.labels  = labels;
		// }
	}
}
using System.Collections.Generic;

namespace Anjin.Regions
{
	/// <summary>
	/// Defines a sequence of region objects. Does not have any special consideration for links.
	/// </summary>
	public class RegionObjectSequence : RegionObject
	{
		public List<RegionObject> Objects;

		public RegionObjectSequence() : this(null) { }
		public RegionObjectSequence(RegionGraph parentGraph) : base(parentGraph)
		{
			Objects = new List<RegionObject>();
		}
	}

















}
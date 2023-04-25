using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Regions {
	public class SceneRegionObject : SceneRegionObjectBase {
		public RegionObject Region;

		public override bool AddObjectToList(List<RegionObject> Objects)
		{
			if (Region == null) return false;

			Objects.Add(Region);

			return true;
		}
	}
}
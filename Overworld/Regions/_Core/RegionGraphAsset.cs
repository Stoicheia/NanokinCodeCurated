using Anjin.Scripting;
using Sirenix.OdinInspector;

namespace Anjin.Regions
{
	[LuaUserdata]
	public class RegionGraphAsset : SerializedScriptableObject
	{
		public RegionGraph Graph;

		public void OnCreation()
		{
			Graph = new RegionGraph();
		}
	}
}
using Anjin.Regions;
using Anjin.Scripting;

namespace Overworld.Controllers
{
	[LuaUserdata(StaticName = "Regions")]
	public class RegionsLua
	{
		public static void load_graph(string asset)
		{
			RegionController.Live.LoadGraph(asset);
		}
	}
}
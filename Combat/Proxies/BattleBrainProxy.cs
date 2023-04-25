using Anjin.Scripting;

namespace Combat
{
	[LuaProxyTypes(typeof(BattleBrain), Descendants = true)]
	public class BattleBrainProxy : LuaProxy<BattleBrain>
	{
		public Team team => proxy.team;
	}
}
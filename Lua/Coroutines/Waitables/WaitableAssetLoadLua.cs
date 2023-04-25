using MoonSharp.Interpreter;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableAssetLoadLua : ICoroutineWaitable
	{
		public Table  AssetTable;
		public object Result => AssetTable;

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			if(AssetTable.TryGet("is_loaded", out bool val))
			{
				return val;
			}

			return true;
		}

		public WaitableAssetLoadLua(Table assetTable)
		{
			AssetTable = assetTable;
		}
	}
}
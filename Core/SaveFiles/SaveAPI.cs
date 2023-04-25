using Anjin.Scripting;
using Data.Shops;
using MoonSharp.Interpreter;
using SaveFiles;

public static class SaveAPI
{
	[LuaGlobalFunc]
	public static bool has_item(string search)
	{
		return SaveManager.current.HasItem(search);
	}

	[LuaGlobalFunc]
	public static bool lose_item(string addr)
	{
		return SaveManager.current.LoseItem(addr);
	}

	[LuaGlobalFunc]
	public static void gain_item(string addr, Table conf)
	{
		LootEntry entry = new LootEntry(LootType.Item, addr);
		entry.ReadConf(conf);
		entry.AwardAll();
	}
}
namespace Anjin.Scripting
{
	/// <summary>
	/// Allows accessing of a component through a LuaComponentBase.
	/// N.B.: Must be marked with [LuaUserData] as well.
	/// </summary>
	public interface ILuaAddon
	{
		string NameInTable { get; }
	}
}
using Combat.Scripting;
using JetBrains.Annotations;

namespace Anjin.Scripting
{
	public interface ILuaObject
	{
		LuaAsset Script { get; }

		[CanBeNull]
		ScriptStore LuaStore { get; set; }

		[CanBeNull]
		string[] Requires { get; }
	}
}
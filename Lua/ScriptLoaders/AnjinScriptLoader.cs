using System.Collections.Generic;
using MoonSharp.Interpreter.Loaders;

namespace Anjin.Scripting
{
	public abstract class AnjinScriptLoader : ScriptLoaderBase
	{
		public abstract List<string> GetAllNames();
	}
}
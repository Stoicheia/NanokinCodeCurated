using MoonSharp.Interpreter;
using Pathfinding;
using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace Anjin.Scripting
{

	[LuaProxyTypes(typeof(RichAI))]
	public class AStarAIProxy : LuaProxy<IAstarAI>
	{
		public Vector3 destination { get => proxy.destination; set => proxy.destination = value; }

		public void Teleport(Vector3 newPosition, bool clearPath = true) => proxy.Teleport(newPosition, clearPath);
	}

	[LuaProxyTypes(typeof(LuaScriptComponent))]
	public class LuaComponentBaseProxy<C> : MonoLuaProxy<C> where C: LuaComponentBase
	{
		public Closure mono_update { get => proxy.mono_update; set => proxy.mono_update   = value; }
		public Closure on_reset    { get => proxy.on_reset;    set => proxy.on_reset = value; }
	}

	public class LuaComponentProxy : LuaComponentBaseProxy<LuaComponentBase> { }

}
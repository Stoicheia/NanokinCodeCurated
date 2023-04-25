using Anjin.Actors;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.Scripting
{
	[LuaUserdata]
	public class LuaOnSwordHit : MonoBehaviour, ILuaAddon, IHitHandler<SwordHit>
	{
		public string Function;
		public string NameInTable => "SwordHit";

		[HideInInspector] public Closure on_hit;

		public void OnHit(SwordHit hit)
		{
			Lua.RunGlobal(Function);
			on_hit?.Call(hit);
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
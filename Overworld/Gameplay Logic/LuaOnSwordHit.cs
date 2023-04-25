using Anjin.Actors;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Lua Events/Lua on Sword Hit")]
	public class LuaOnSwordHit : SerializedMonoBehaviour, IHitHandler<SwordHit>
	{
		public string   Function;
		public LuaAsset Script;

		public void OnHit(SwordHit info)
		{
			if (info is SwordHit swordHit)
			{
				Lua.RunScriptOrGlobal(Function, Script);
			}
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
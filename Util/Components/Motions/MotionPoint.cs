using Anjin.Scripting;
using UnityEngine;

namespace Anjin.Utils
{
	/// <summary>
	/// A point in a MPath
	/// </summary>
	[LuaUserdata]
	public struct MotionPoint
	{
		public MotionDef?  motion;
		public WorldPoint2 target;

		public override string ToString()
		{
			if (target.P1.gameobject == null || target.P2.gameobject == null) return $"Type: {motion?.Type}";
			return $"From: {target.P1.gameobject.name}; To: {target.P2.gameobject.name} ; Type: {motion?.Type}";
		}
	}
}
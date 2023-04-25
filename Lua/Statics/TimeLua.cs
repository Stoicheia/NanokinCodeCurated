using Anjin.Scripting;
using UnityEngine;

namespace Scripting
{
	[LuaUserdata(StaticName = "Time")]
	public class TimeLua
	{
		public static float delta_time { get => Time.deltaTime; }

	}
}
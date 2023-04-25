using System;
using Anjin.Scripting;

namespace Anjin.Audio
{
	[Flags]
	[LuaEnum]
	public enum AudioLayer
	{
		None     = 0b00,
		Music    = 0b01,
		Ambience = 0b11,
		All      = 0xffffff
	}
}
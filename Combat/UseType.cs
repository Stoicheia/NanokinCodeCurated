using System;
using Anjin.Scripting;

namespace Combat
{
	[LuaEnum("usetype")]
	[Flags]
	public enum UseType
	{
		None    = 0b00,
		Skill   = 0b01,
		Sticker = 0b10,
		Any     = 0b11
	}
}
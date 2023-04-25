using System;
using Anjin.Scripting;

namespace Data.Nanokin
{
	[LuaEnum("limbtype")]
	public enum LimbType
	{
		None,
		Body,
		Head,
		Arm1,
		Arm2,
	}

	public static class NanokinLimbKindExtensions
	{
		public static string ToGameName(this LimbType type)
		{
			switch (type)
			{
				case LimbType.Body: return "Body";
				case LimbType.Head: return "Head";
				case LimbType.Arm1: return "Main Arm";
				case LimbType.Arm2: return "Off Arm";
				case LimbType.None: return "<none>";
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}
	}
}
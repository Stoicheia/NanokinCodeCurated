using System;
using Anjin.Scripting;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Util
{
	[Serializable, Inline(true)]
	[LuaUserdata]
	public struct RangeOrInt
	{
		public Modes mode;
		[FormerlySerializedAs("value")]
		public int min;
		public int max;

		public bool IsRange => mode == Modes.Range;


		public RangeOrInt(int min)
		{
			mode     = Modes.Constant;
			this.min = min;
			max      = min;
		}

		public RangeOrInt(int min, int max)
		{
			mode     = Modes.Range;
			this.min = min;
			this.max = max;
		}

		public override string ToString()
		{
#if UNITY_EDITOR
			if (IsRange)
				return $"Range [{min}, {max}]";
			else
#endif
				return $"Constant '{min}'";
		}

		public static implicit operator RangeOrInt(int v)
		{
			return new RangeOrInt(v);
		}

		public static implicit operator int(RangeOrInt roi)
		{
			switch (roi.mode)
			{
				case Modes.Range:    return RNG.Range(roi.min, roi.max);
				case Modes.Constant: return roi.min;
				default:             throw new ArgumentOutOfRangeException();
			}
		}


		public enum Modes
		{
			Range,
			Constant,
		}
	}
}
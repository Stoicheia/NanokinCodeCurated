using System;
using Anjin.Scripting;
using Util.Odin.Attributes;

namespace Util
{
	[Serializable, Inline(true)]
	[LuaUserdata]
	public struct RangeOrFloat
	{
		public Modes mode;
		public float min, max;
		public float value;

		public bool IsRange => mode == Modes.Range;

		public RangeOrFloat(float value)
		{
			mode       = Modes.Constant;
			this.value = value;
			min        = value;
			max        = value;
		}

		public RangeOrFloat(float min, float max)
		{
			mode     = Modes.Range;
			value    = min;
			this.min = min;
			this.max = max;
		}

		public float Evaluate()
		{
			switch (mode)
			{
				case Modes.Range:    return RNG.Range(min, max);
				case Modes.Constant: return value;
				default:             throw new ArgumentOutOfRangeException();
			}
		}

		public override string ToString()
		{
#if UNITY_EDITOR
			if (IsRange)
				return $"Range [{min}, {max}]";
			else
#endif
				return $"Constant '{value}'";
		}


		public enum Modes
		{
			Range,
			Constant,
		}

		public static implicit operator RangeOrFloat(float duration)
		{
			return new RangeOrFloat(duration);
		}

		public static implicit operator RangeOrFloat((float min, float max) duration)
		{
			return new RangeOrFloat(duration.min, duration.max);
		}
	}
}
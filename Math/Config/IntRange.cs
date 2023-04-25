using Anjin.Util;
using Sirenix.OdinInspector;

namespace Util
{
	public struct IntRange
	{
		[ShowInInspector] public int start;
		[ShowInInspector] public int end;

		public int Span => end - start;

		public IntRange(int start, int end)
		{
			this.start = start;
			this.end   = end;
		}

		public bool Contains(int value)
		{
			return value.Between(start, end);
		}

		public bool CollectionContains(int value)
		{
			return value.Between(start, end - 1);
		}

		public float GetPercent(int frame)
		{
			int span = end   - start;
			int pos  = frame - start;

			return pos / (float) span;
		}
	}
}
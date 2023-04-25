using Data.Combat;

namespace Combat.Data
{
	public struct PointChange
	{
		public Pointf   value;
		public bool     critical, miss;
		public Elements element;
		public bool     noNumbers;

		public PointChange(Pointf value, bool noNumbers = false) : this()
		{
			this.value     = value;
			this.noNumbers = noNumbers;
			miss = value.hp == 0 &&
			       value.sp == 0 &&
			       value.op == 0;
		}

		public static implicit operator PointChange(Pointf value) => new PointChange(value);
	}

	public static class PointChangeExtensions
	{
		public static PointChange ToChange(this Pointf pt, bool silent = false) => new PointChange(pt, silent);
	}
}
namespace Combat.Data
{
	// public static class StatOpExtensions
	// {
	// 	public static bool IsApplicable(this StatOp op)
	// 	{
	// 		int i = (int) op;
	// 		return i >= (int) StatOp.set && i <= (int) StatOp.randomize;
	// 	}
	//
	// 	public static bool IsTransient(this StatOp op)
	// 	{
	// 		int i = (int) op;
	// 		return i >= (int) StatOp.up && i <= (int) StatOp.scale;
	// 	}
	// }

	public enum StatOp
	{
		// Cached
		// These operations are cached flat/scale properties.
		up,
		low,
		scale,
		flag,

		// Transient
		// These operations can only be applied on transient values (at the time they are used)
		set,
		max,
		min,
		function,
		forbid,
		restrict,
		randomize,
	}
}
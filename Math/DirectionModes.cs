public enum DirectionModes
{
	/// <summary>
	/// Up, down, left and right.
	/// </summary>
	Cardinal,

	/// <summary>
	/// Diagonals + orthogonals
	/// </summary>
	Ordinal,

	/// <summary>
	/// All left and vertical directions, right is mirrored from left.
	/// </summary>
	CardinalMirrored,

	/// <summary>
	/// Up, left and down, right is mirrored from left.
	/// </summary>
	OrdinalMirrored,
}
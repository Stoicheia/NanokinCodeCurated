namespace Combat
{
	public enum BattleBrains
	{
		/// <summary>
		/// No brain, skip turns.
		/// </summary>
		none,

		/// <summary>
		/// No brain, skip turns.
		/// </summary>
		skip,

		/// <summary>
		/// Let the player control the team.
		/// </summary>
		player,

		/// <summary>
		/// Use a debug UI to control the team.
		/// </summary>
		debug,

		/// <summary>
		/// Automatically assign default fighter brains specified in the FighterInfo.
		/// </summary>
		auto,

		/// <summary>
		/// Pick choices at random.
		/// </summary>
		random
	}
}
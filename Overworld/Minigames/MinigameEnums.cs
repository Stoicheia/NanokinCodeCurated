using System;
using Anjin.Scripting;

// ReSharper disable UnusedMember.Global

namespace Anjin.Minigames
{
	[LuaEnum]
	public enum CollectionSpawnMode { Region, Children }

	[LuaEnum]
	public enum MinigameDifficulty
	{
		None   = 0,
		Novice = 1,
		Easy   = 2,
		Medium = 3,
		Hard   = 4,
		Custom = 10,
	}

	[LuaEnum]
	public enum MinigameRank
	{
		None = 0,

		S, A, B, C, D, E, F
	}

	[LuaEnum]
	public enum MinigameState
	{
		Off,
		Intro,
		Running,
		Outro
	}

	public enum MinigameFinish {
		Normal,
		UserQuit,
		DebugWin,
		DebugLose,
	}

	[Flags]
	[LuaEnum]
	public enum MinigamePlayOptions
	{
		None      = 0,
		PlayIntro = 1 << 1,
		PlayOutro = 1 << 2,

		Default = PlayIntro | PlayOutro,
	}
}
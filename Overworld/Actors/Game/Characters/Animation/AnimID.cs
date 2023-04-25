namespace Anjin.Actors
{
	/// <summary>
	/// This defines a way to reference animations in code without using strings.
	/// </summary>
	public enum AnimID
	{
		// Ground
		None  = 0,
		Stand = 1,
		Walk  = 2,
		Run   = 3,

		// Air
		Air      = 20,
		Jump     = 21,
		Rise     = 22,
		Fall     = 23,
		Land     = 24,
		Dive     = 25,
		Glide    = 26,
		Sword    = 27,
		SwimIdle = 30,
		SwimMove = 31,
		DoubleJump = 32,
		WallSlide = 33,
		WallJump = 34,
		WallBonk = 35,
		WallTakeoff = 36,

		// Special
		Pogo = 40,
		Sit  = 41,

		// Idle
		Idle1 = 60,

		// Combat
		CombatIdle    = 150,
		CombatIdle2	  = 151,
		CombatIdle3	  = 152,
		CombatHurt    = 153,
		CombatHurt2	  = 154,
		CombatHurt3	  = 155,
		CombatTurn    = 156,
		CombatTurn2	  = 157,
		CombatTurn3	  = 158,
		CombatAction  = 159,
		CombatAction2 = 160,
		CombatAction3 = 161,
		CombatWin     = 162,
		CombatWin2	  = 163,
		CombatWin3	  = 164,
		CombatWinGoof = 165,
		CombatWinGoof2 = 166,
		CombatWinGoof3 = 167,
		CombatLoss = 168,
		CombatLoss2 = 169,
		CombatLoss3 = 170,

		// Fish
		FishRise  = 1000,
		FishStill = 1001,
		FishFall  = 1002,
		FishFlop  = 1003,

		// Landshark
		Roam   = 1100,
		Emerge = 1200,

		// Cannoneer
		Light  = 1300,
		Fire   = 1301
	}

	public static class AnimUtil
	{
		public static AnimID FromLeft(string str)
		{
			int i = str.IndexOf("_");

			if (i == -1) i = str.Length;
			return FromString(str.Substring(0, i));
		}

		public static Direction8 FromRight(string str)
		{
			int i = str.IndexOf("_");

			if (i == -1) return Direction8.None;
			if (i == 0) return Direction8.Down;
			return DirUtil.FromString(str.Substring(i + 1, str.Length - i - 1));
		}

		// TODO(C.L.): Add the others as needed.
		public const string ID_SWIM_MOVE = "swim-move";

		public static AnimID FromString(string str)
		{
			switch (str)
			{
				case "stand":     return AnimID.Stand;
				case "idle1":     return AnimID.Idle1;
				case "walk":      return AnimID.Walk;
				case "run":       return AnimID.Run;
				case "air":       return AnimID.Air;
				case "jump":      return AnimID.Jump;
				case "rise":      return AnimID.Rise;
				case "fall":      return AnimID.Fall;
				case "land":      return AnimID.Land;
				case "dive":      return AnimID.Dive;
				case "glide":     return AnimID.Glide;
				case "sword":     return AnimID.Sword;
				case "swim-idle": return AnimID.SwimIdle;
				case "swim-move": return AnimID.SwimMove;
				case "pogo":      return AnimID.Pogo;
				case "sit":       return AnimID.Sit;
				case "doublejump":	return AnimID.DoubleJump;
				case "wallslide":	return AnimID.WallSlide;
				case "walltakeoff":	return AnimID.WallTakeoff;
				//case "walljump":	return AnimID.WallJump;
				case "wallbonk":	return AnimID.WallBonk;

				case "air-fall":  return AnimID.FishFall;
				case "air-rise":  return AnimID.FishRise;
				case "air-still": return AnimID.FishStill;

				case "fish-rise":  return AnimID.FishRise;
				case "fish-still": return AnimID.FishStill;
				case "fish-fall":  return AnimID.FishFall;
				case "fish-flop":  return AnimID.FishFlop;

				case "light": return AnimID.Light;
				case "fire": return AnimID.Fire;

				case "roam":   return AnimID.Roam;
				case "emerge": return AnimID.Emerge;

				// Combat
				case "combat-idle":     return AnimID.CombatIdle;
				case "combat-idle-2": return AnimID.CombatIdle2;
				case "combat-idle-3": return AnimID.CombatIdle3;
				case "combat-hurt":     return AnimID.CombatHurt;
				case "combat-hurt-2": return AnimID.CombatHurt2;
				case "combat-hurt-3": return AnimID.CombatHurt3;
				case "combat-turn":     return AnimID.CombatTurn;
				case "combat-turn-2": return AnimID.CombatTurn2;
				case "combat-turn-3": return AnimID.CombatTurn3;
				case "combat-action":   return AnimID.CombatAction;
				case "combat-action-2": return AnimID.CombatAction2;
				case "combat-action-3": return AnimID.CombatAction3;
				case "combat-win":      return AnimID.CombatWin;
				case "combat-win-2": return AnimID.CombatWin2;
				case "combat-win-3": return AnimID.CombatWin3;
				case "combat-win-goof": return AnimID.CombatWinGoof;
				case "combat-win-goof-2": return AnimID.CombatWinGoof2;
				case "combat-win-goof-3": return AnimID.CombatWinGoof3;
				case "combat-loss": return AnimID.CombatIdle;
				case "combat-loss-2": return AnimID.CombatIdle2;
				case "combat-loss-3": return AnimID.CombatIdle3;

				default:
					return AnimID.None;
			}
		}
	}
}
using Anjin.Scripting;
using UnityEngine;

[LuaEnum]
public enum Direction8
{
	None,
	Down,
	DownLeft,
	Left,
	UpLeft,
	Up,
	UpRight,
	Right,
	DownRight,
}


public enum CardinalDirections
{
	Down,
	Left,
	Up,
	Right
}

public enum AxisDirection
{
	Horizontal,
	Vertical
}

public static class DirUtil
{
	public const string Down      = "down";
	public const string DownLeft  = "downLeft";
	public const string Left      = "left";
	public const string UpLeft    = "upLeft";
	public const string Up        = "up";
	public const string UpRight   = "upRight";
	public const string Right     = "right";
	public const string DownRight = "downRight";

	public static readonly float[] BlendingCardinal =
	{
		0 / 4f,
		1 / 4f,
		2 / 4f,
		3 / 4f,
	};

	public static readonly string[] Cardinals =
	{
		Down,
		Left,
		Up,
		Right,
	};

	public static readonly float[] BlendingOrdinal =
	{
		0 / 8f,
		1 / 8f,
		2 / 8f,
		3 / 8f,
		4 / 8f,
		5 / 8f,
		6 / 8f,
		7 / 8f,
	};

	public static readonly string[] Ordinals =
	{
		Down,
		DownLeft,
		Left,
		UpLeft,
		Up,
		UpRight,
		Right,
		DownRight,
	};

	public static readonly Direction8[] Directions =
	{
		Direction8.Down,
		Direction8.DownLeft,
		Direction8.Left,
		Direction8.UpLeft,
		Direction8.Up,
		Direction8.UpRight,
		Direction8.Right,
		Direction8.DownRight,
	};

	public static readonly Vector3[] OrdinalVectors =
	{
		Vector3.back,
		(Vector3.back + Vector3.left).normalized,
		Vector3.left,
		(Vector3.forward + Vector3.left).normalized,
		Vector3.forward,
		(Vector3.forward + Vector3.right).normalized,
		Vector3.right,
		(Vector3.back + Vector3.right).normalized
	};

	public static Direction8 FromString(this string dir)
	{
		switch (dir)
		{
			case Down:      return Direction8.Down;
			case Up:        return Direction8.Up;
			case UpRight:   return Direction8.UpRight;
			case Right:     return Direction8.Right;
			case DownRight: return Direction8.DownRight;
			case UpLeft:    return Direction8.UpLeft;
			case Left:      return Direction8.Left;
			case DownLeft:  return Direction8.DownLeft;

			default: return Direction8.None;
		}
	}

	public static string ToString(this Direction8 dir)
	{
		switch (dir)
		{
			case Direction8.Down:      return Down;
			case Direction8.Up:        return Up;
			case Direction8.UpRight:   return UpRight;
			case Direction8.Right:     return Right;
			case Direction8.DownRight: return DownRight;
			case Direction8.UpLeft:    return UpLeft;
			case Direction8.Left:      return Left;
			case Direction8.DownLeft:  return DownLeft;

			default: return string.Empty;
		}
	}

	public static Direction8 ForceLeft(this Direction8 dir)
	{
		switch (dir)
		{
			case Direction8.UpRight:   return Direction8.UpLeft;
			case Direction8.Right:     return Direction8.Left;
			case Direction8.DownRight: return Direction8.DownLeft;
			default:                   return dir;
		}
	}

	public static bool IsRight(this Direction8 dir)
	{
		switch (dir)
		{
			case Direction8.UpRight:   return true;
			case Direction8.Right:     return true;
			case Direction8.DownRight: return true;
			default:                   return false;
		}
	}

	public static bool IsLeft(this Direction8 dir)
	{
		switch (dir)
		{
			// Just in case
			case Direction8.UpLeft:   return true;
			case Direction8.Left:     return true;
			case Direction8.DownLeft: return true;
			default:                  return false;
		}
	}

	public static Direction8 FlipHorizontal(this Direction8 dir)
	{
		switch (dir)
		{
			case Direction8.UpRight:   return Direction8.UpLeft;
			case Direction8.Right:     return Direction8.Left;
			case Direction8.DownRight: return Direction8.DownLeft;

			// Just in case
			case Direction8.UpLeft:   return Direction8.UpRight;
			case Direction8.Left:     return Direction8.Right;
			case Direction8.DownLeft: return Direction8.DownRight;

			default: return dir;
		}
	}
}
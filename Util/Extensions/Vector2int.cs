using UnityEngine;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static Vector2 vec2(this Vector2Int vector2Int)
		{
			return new Vector2(vector2Int.x, vector2Int.y);
		}
	}
}
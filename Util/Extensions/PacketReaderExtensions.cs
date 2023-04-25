using NanokinBattleNet.Library.Utilities;
using UnityEngine;

namespace Util.Extensions
{
	public static class PacketReaderExtensions
	{
		public static Vector2Int V2Int(this PacketReader pr)
		{
			return new Vector2Int(pr.Int(), pr.Int());
		}
	}
}
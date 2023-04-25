using NanokinBattleNet.Library.Utilities;
using UnityEngine;

namespace Util.Extensions
{
	public static class PacketWriterExtensions
	{
		public static void V2Int(this PacketWriter pw, Vector2Int v2)
		{
			pw.Int(v2.x);
			pw.Int(v2.y);
		}
	}
}
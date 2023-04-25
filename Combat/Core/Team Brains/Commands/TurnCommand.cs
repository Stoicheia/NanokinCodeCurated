using System;
using Combat.Toolkit;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public abstract class TurnCommand
	{
		public abstract string Text { get; }

		public abstract BattleAnim GetAction(Battle battle);

		public virtual void WritePacket(PacketWriter pw)
		{
			throw new NotImplementedException($"The action command {ToString()} is unsupported in multiplayer.");
		}
	}
}
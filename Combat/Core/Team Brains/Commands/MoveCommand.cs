using System;
using Combat.Toolkit;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;
using UnityEngine;
using Util.Extensions;

namespace Combat
{
	public class MoveCommand : TurnCommand
	{
		public Fighter fighter;
		public Slot    slot;

		public MoveCommand(Fighter fighter, Slot destinationSlot)
		{
			this.fighter = fighter;
			slot         = destinationSlot;
		}

		public MoveCommand(PacketReader pr, [NotNull] Battle battle)
		{
			Vector2Int coord = pr.V2Int();

			slot    = battle.GetSlot(coord);
			fighter = (Fighter) battle.ActiveActer;
		}

		[NotNull]
		public override string Text => "Move";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new MoveAnim(fighter, slot, MoveSemantic.Auto);

		public override void WritePacket(PacketWriter pw)
		{
			// pw.Byte((byte) NetplayActionCommands.Move);
			// pw.V2Int(_cellPosition);
			throw new NotImplementedException();
		}
	}
}
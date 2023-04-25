using System;
using Combat.Toolkit;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public class HoldCommand : TurnCommand
	{
		public Fighter fighter;

		public HoldCommand(Fighter fighter)
		{
			this.fighter = fighter;
		}

		public HoldCommand(PacketReader pr, Battle battle)
		{
			fighter = (Fighter) battle.ActiveActer;
			throw new NotImplementedException();
		}

		[NotNull]
		public override string Text => "Hold";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new HoldAnim(fighter);
		//[NotNull]
		//public override BattleAction GetAction(Battle battle)
		//{
		//	HoldAction action = new HoldAction(fighter);
		//	action.core = battle.core;
		//	action.battle = battle;

		//	return action; //(TurnQueryAction) new MoveRestOfGroupToEnd();
		//}

		public override void WritePacket([NotNull] PacketWriter pw)
		{
			pw.Byte((byte) NetplayActionCommands.Hold);
		}
	}
}
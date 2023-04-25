using System;
using System.Collections.Generic;
using Combat.Components;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public class NetplayChip : Chip
	{
		public BattleClient                      Client                { get; set; }
		public Dictionary<int, NetplayBrain> RemoteTeamControllers { get; set; }

		[NotNull]
		public TurnCommand ReadCommand([NotNull] PacketReader pr)
		{
			var commandType = (NetplayActionCommands) pr.Byte();

			switch (commandType)
			{
				case NetplayActionCommands.Skill:     return new SkillCommand(pr, battle);
				case NetplayActionCommands.Move:      return new MoveCommand(pr, battle);
				case NetplayActionCommands.Hold:      return new HoldCommand(pr, battle);
				case NetplayActionCommands.Overdrive: return new OverdriveCommand(pr, battle);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void OnRemoteCommandReceived(int clientID, [NotNull] PacketReader pr)
		{
			TurnCommand      command   = ReadCommand(pr);
			NetplayBrain brain = RemoteTeamControllers[clientID];

			// TODO make sure this is not broken
			bool isFromMatchingPlayer = brain.team.fighters.Contains(battle.ActiveActer as Fighter);
			if (isFromMatchingPlayer)
			{
				// TODO need a real NetplayBrain, not this garbage
				// teamBrain.confirmedAction = command.GetAction(battle);
				throw new NotImplementedException();
			}
			else
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.BATTLE_STATE_MISMATCH();
				ServerPacketCreator.End(Client);
			}
		}
	}
}
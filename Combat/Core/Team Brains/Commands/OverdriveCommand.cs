using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Toolkit;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public class OverdriveCommand : TurnCommand
	{
		private readonly Fighter           _fighter;
		private readonly List<TurnCommand> _subCommands;

		public OverdriveCommand(Fighter fighter, List<TurnCommand> subCommands)
		{
			_fighter     = fighter;
			_subCommands = subCommands;
		}

		public OverdriveCommand(PacketReader pr, Battle battle)
		{
			throw new NotImplementedException();
		}

		[NotNull]
		public override string Text => "Overdrive";

		[NotNull] public override BattleAnim GetAction(Battle battle)
		{
			var actions = _subCommands.Select(cmd => cmd.GetAction(battle)).ToList();
			return new OverdriveAnim(_fighter, actions);
		}

		public override void WritePacket([NotNull] PacketWriter pw)
		{
			pw.Byte((byte) NetplayActionCommands.Overdrive);
			pw.List(_subCommands, command => command.WritePacket(pw));
		}
	}
}
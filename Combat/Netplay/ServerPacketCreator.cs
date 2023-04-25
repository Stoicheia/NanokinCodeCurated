using System;
using System.Collections.Generic;
using Assets.Nanokins;
using Combat.Startup;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Opcodes;
using NanokinBattleNet.Library.Utilities;
using Util.Network;

namespace Combat
{
	public static class ServerPacketCreator
	{
		private static PacketWriter _pw = new PacketWriter();

		public static void Begin()
		{
			_pw.Reset();
		}

		public static void Begin(ServerOpcodes opcode)
		{
			_pw.Reset();
			_pw.Short((short) opcode);
		}

		public static void End([NotNull] NetworkClient client)
		{
			client.Send(_pw.GetBytes());
		}

		public static void WriteUnitTeam([NotNull] TeamRecipe teamRecipe)
		{
			List<MonsterRecipe> units = teamRecipe.Monsters;

			_pw.Byte((byte) units.Count); // There shouldn't be more than 255 units in a team lol

			foreach (MonsterRecipe entry in units)
			{
				throw new NotImplementedException();
				// if (entry.unit is NanokinInstance nanokin)
				// {
				// 	_pw.String(nanokin.Name);
				// 	_pw.Short((short) nanokin.Points.hp);
				// 	_pw.Short((short) nanokin.Points.sp);
				//
				// 	// Write the limbs.
				// 	LimbInstance[] limbs = nanokin.Limbs.ToArray();
				//
				// 	_pw.Byte((byte) limbs.Length); // More than 255 limbs on a nanokin..? doubtful
				// 	foreach (LimbInstance monsterLimb in limbs)
				// 	{
				// 		_pw.Byte((byte) monsterLimb.level);
				// 		_pw.String(monsterLimb.AssetAddress);
				// 	}
				// }
				// else
				// {
				// 	Debug.LogError($"UNSUPPORTED UNIT TYPE OVER NETWORK: {units.GetType()}");
				// }
			}
		}

		public static void ROOM_ENTER_REQUEST(int roomId)
		{
			_pw.Opcode(ServerOpcodes.ROOM_ENTER_REQUEST);
			_pw.Int(roomId);
		}

		public static void CLIENT_NAME_SET(string name)
		{
			_pw.Opcode(ServerOpcodes.CLIENT_SET_NAME);
			_pw.String(name);
		}

		public static void CLIENT_SET_TEAM()
		{
			_pw.Opcode(ServerOpcodes.CLIENT_SET_TEAM);
			// WriteUnitTeam(SaveManager.Current.ToUnitTeam()); // Send the team in the inventory.
			throw new NotImplementedException();
		}

		public static void ROOM_LIST_REQUEST()
		{
			_pw.Opcode(ServerOpcodes.ROOM_LIST_REQUEST);
		}

		public static void ROOM_LEAVE_REQUEST()
		{
			_pw.Opcode(ServerOpcodes.ROOM_LEAVE_REQUEST);
		}

		public static void ROOM_BATTLE_START()
		{
			_pw.Opcode(ServerOpcodes.ROOM_BATTLE_START_REQUEST);
		}

		public static void BATTLE_CLIENT_READY()
		{
			_pw.Opcode(ServerOpcodes.BATTLE_CLIENT_READY);
		}

		public static void ROOM_CREATE(string name)
		{
			// _pw.Opcode(ServerOpcodes.ROOM_CREATE);
			// _pw.String(name);
		}

		public static void BATTLE_CLIENT_COMMAND([NotNull] TurnCommand command)
		{
			_pw.Opcode(ServerOpcodes.BATTLE_CLIENT_COMMAND);
			command.WritePacket(_pw);
		}

		public static void BATTLE_STATE_MISMATCH()
		{
			_pw.Opcode(ServerOpcodes.BATTLE_STATE_MISMATCH);
		}
	}
}
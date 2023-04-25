using System;
using System.Collections.Generic;
using Systems.Combat.Networking;
using Systems.Combat.Networking.Components;
using Anjin.Nanokin;
using Anjin.Util;
using Anjin.Utils;
using Combat.Networking;
using JetBrains.Annotations;
using NanokinBattleNet.Library;
using NanokinBattleNet.Library.Opcodes;
using NanokinBattleNet.Library.Utilities;
using SaveFiles;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.Network;

namespace Combat
{
	public class BattleClient : NetworkClient
	{
		[SerializeField] private GameObject                                _canvas;
		[SerializeField] private SceneReference                            _roomUIScene;
		[SerializeField] private Dictionary<NetplayArenas, SceneReference> _arenaScenes;
		[SerializeField] private bool                                      _printReceivedPackets;
		[SerializeField] private DialogUI                                  _dialogUI;

		private RoomUI        _roomUI;
		private GenericDialog _connectingDialog;

		public event Action onExit;

		public delegate void PacketHandler(PacketReader pr);

		private void Start()
		{
			Connect();
		}

		protected override void OnUpdate()
		{
#if UNITY_EDITOR
			if (GameInputs.IsPressed(Key.Numpad0))
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.ROOM_CREATE($"Random Room {RNG.Int(9999)}");
				ServerPacketCreator.End(this);
			}
#endif
		}

		protected override void OnStartConnecting()
		{
			_connectingDialog = _dialogUI.Empty();
			_connectingDialog.AddLabel("Connecting ...", Color.gray);
		}

		protected override void OnConnectionFailed()
		{
			_connectingDialog.Close();

			GenericDialog genericDialog = _dialogUI.Empty();

			genericDialog.AddLabel("Error!", Color.red);
			genericDialog.AddLabel("Failed to connect! Retry?");

			genericDialog.AddSpace();

			genericDialog.AddButton("Yes",
				() =>
				{
					Connect();
					genericDialog.Close();
				});

			genericDialog.AddButton("No",
				() =>
				{
					genericDialog.Close();
					SceneLoader.Unload(gameObject.scene);
					onExit?.Invoke();
				});
		}

		protected override void OnConnected()
		{
			SaveManager.current.HealParty();

			_connectingDialog.Close();
			ServerPacketCreator.Begin(ServerOpcodes.HANDSHAKE_REQUEST);
			ServerPacketCreator.End(this);
		}

		protected override void OnDataReceived([NotNull] PacketReader reader)
		{
			var           opcode  = (ClientOpcodes) reader.Opcode();
			PacketHandler handler = GetHandlerForOpcode(opcode);

			handler?.Invoke(reader);
		}

		[CanBeNull]
		private PacketHandler GetHandlerForOpcode(ClientOpcodes opcode)
		{
			if (_printReceivedPackets)
			{
				Debug.Log($"RECEIVED PACKET: {opcode}");
			}

			switch (opcode)
			{
				case ClientOpcodes.CLIENT_HANDSHAKE:             return HandleClientHandshake;
				case ClientOpcodes.CLIENT_REQUEST_TEAM:          return HandleClientRequestTeam;
				case ClientOpcodes.CLIENT_SHOW_POPUP:            return HandleClientShowPopup;
				case ClientOpcodes.INFO_ROOM_LIST:               return HandleInfoRoomList;
				case ClientOpcodes.INFO_CLIENT_TEAM:             return HandleInfoClientTeam;
				case ClientOpcodes.ROOM_ENTER:                   return HandleRoomEnter;
				case ClientOpcodes.ROOM_LEAVE:                   return HandleRoomLeave;
				case ClientOpcodes.ROOM_MESSAGE_ADD:             return HandleRoomMessageAdd;
				case ClientOpcodes.ROOM_MEMBER_ENTER:            return HandleRoomUserEnter;
				case ClientOpcodes.ROOM_MEMBER_LEAVE:            return HandleRoomUserLeave;
				case ClientOpcodes.ROOM_LOCK_INTERACTION:        return HandleRoomLockModifications;
				case ClientOpcodes.BATTLE_BEGIN_LOADING:         return HandleBattleBeginLoading;
				case ClientOpcodes.BATTLE_BEGIN_EXECUTION:       return HandleBattleBeginExecution;
				case ClientOpcodes.BATTLE_REMOTE_CLIENT_COMMAND: return HandleBattleRemoteClientCommand;
				case ClientOpcodes.BATTLE_END:                   return HandleBattleEnd;
			}

			Debug.LogError($"Packet '{opcode}' received from server is unhandled by the client.");
			return null;
		}

		private void HandleInfoClientTeam([NotNull] PacketReader pr)
		{
			NetworkInformation.Clients.UpdateTeam(pr);
		}

		private void HandleRoomMessageAdd(PacketReader pr) { }

		private void HandleRoomLeave(PacketReader pr)
		{
			_roomUI.roomInsideUI.HideUI();
			NetworkState.Room.Clear();

			ServerPacketCreator.Begin();
			ServerPacketCreator.ROOM_LIST_REQUEST();
			ServerPacketCreator.End(this);
		}

		private void HandleRoomLockModifications(PacketReader pr) { }

		private void HandleClientHandshake([NotNull] PacketReader pr)
		{
			NetworkState.ClientID = pr.Int();

			ServerPacketCreator.Begin();
			ServerPacketCreator.CLIENT_NAME_SET(GameOptions.current.netplay_username);
			ServerPacketCreator.End(this);

			SceneLoader.GetDriverScene<RoomUI>(_roomUIScene,
				(scene, ui) =>
				{
					_roomUI = ui;

					ServerPacketCreator.Begin();
					ServerPacketCreator.ROOM_LIST_REQUEST();
					ServerPacketCreator.End(this);
				});
		}

		private void HandleClientRequestTeam(PacketReader pr)
		{
			ServerPacketCreator.Begin();
			ServerPacketCreator.CLIENT_SET_TEAM();
			ServerPacketCreator.End(this);
		}

		private void HandleInfoRoomList([NotNull] PacketReader pr)
		{
			short numRooms = pr.Short();

			NetworkInformation.RoomRegistry.Clear();

			for (var i = 0; i < numRooms; i++)
			{
				int    roomId   = pr.Int();
				string roomName = pr.String();

				RoomInfo room = NetworkInformation.RoomRegistry.Update(roomId);
				room.name = roomName;
			}

			_roomUI.roomListUI.ShowUI(NetworkInformation.RoomRegistry.List);
			_roomUI.roomListUI.onRoomPicked = roomToEnter =>
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.ROOM_ENTER_REQUEST(roomToEnter.ID);
				ServerPacketCreator.End(this);
			};
		}

		private void HandleRoomEnter([NotNull] PacketReader pr)
		{
			// Hide the room list.
			_roomUI.roomListUI.HideUI();

			int roomId = pr.Int();

			// Read the room information.
			RoomInfo room = NetworkInformation.RoomRegistry[roomId];

			var insideInfo = new RoomInsideInfo();
			room.inside = insideInfo;

			short nMembers = pr.Short();
			for (var i = 0; i < nMembers; i++)
			{
				int    clientID   = pr.Int();
				string clientName = pr.String();

				ClientInfo client = NetworkInformation.Clients.Update(clientID);
				client.name = clientName;

				insideInfo.members.Add(clientID);
			}

			NetworkState.Room.Set(roomId);

			_roomUI.roomInsideUI.onLeave = () =>
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.ROOM_LEAVE_REQUEST();
				ServerPacketCreator.End(this);
			};

			_roomUI.roomInsideUI.onStartBattle = () =>
			{
				ServerPacketCreator.Begin();
				ServerPacketCreator.ROOM_BATTLE_START();
				ServerPacketCreator.End(this);
			};

			_roomUI.roomInsideUI.ShowUI(room);
		}

		private void HandleRoomUserEnter(PacketReader pr)
		{
			if (!NetworkState.Room.Get(out RoomInfo currentRoom))
			{
				Debug.LogError("User entered the room while we are not inside of any room.");
				return;
			}

			ClientInfo addedClient = NetworkInformation.Clients.UpdateDisplayInformation(pr);
			currentRoom.inside.AddMember(addedClient);

			_roomUI.roomInsideUI.UpdateMembers();
		}

		private void HandleRoomUserLeave(PacketReader pr)
		{
			if (!NetworkState.Room.Get(out RoomInfo currentRoom))
			{
				Debug.LogError("User entered the room while we are not inside of any room.");
				return;
			}

			int clientID = pr.Int();
			currentRoom.inside.members.Remove(clientID);

			_roomUI.roomInsideUI.UpdateMembers();
		}

		private void HandleBattleBeginLoading([NotNull] PacketReader pr)
		{
			_roomUI.roomInsideUI.HideUI();

			NetworkState.Battle = new BattleInfo(pr);
			NetworkState.Battle.BeginLoading(this, _arenaScenes);
		}

		private void HandleBattleBeginExecution(PacketReader pr)
		{
			NetworkState.Battle.BeginExecution(this);
		}

		private void HandleBattleRemoteClientCommand([NotNull] PacketReader pr)
		{
			int clientID = pr.Int();
			NetworkState.Battle.OnRemoteCommand(clientID, pr);
		}

		private void HandleBattleEnd([NotNull] PacketReader pr)
		{
			var reason = (BattleEndReasons) pr.Byte();

			switch (reason)
			{
				case BattleEndReasons.Win:
					break;

				case BattleEndReasons.Desync:
					_dialogUI.ErrorText("The battle has desynced.");
					1f.Wait(() =>
					{
						// NetworkState.Battle.End();
						NetworkState.Battle.Unload(() =>
						{
							_roomUI.roomInsideUI.ShowUI();
						});
					});
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void HandleClientShowPopup([NotNull] PacketReader pr)
		{
			var popup = (PopupModes) pr.Byte();


			switch (popup)
			{
				case PopupModes.Text:
					string text = pr.String();
					_dialogUI.Text(text);
					break;

				case PopupModes.YesNo:
					throw new NotImplementedException();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using Anjin.Utils;
using JetBrains.Annotations;
using NanokinBattleNet.Library;
using NanokinBattleNet.Library.Utilities;
using UnityEngine;

namespace Combat.Networking
{
	public class BattleInfo
	{
		private NetplayArenas      _arena;
		private SceneReference     _arenaScene;
		private List<int>          _players;
		private Networked1V1Recipe _recipe;

		private Dictionary<int, NetplayBrain> _remoteTeamControllers = new Dictionary<int, NetplayBrain>();
		private NetplayChip                       _netplayChip;

		public BattleInfo([NotNull] PacketReader pr)
		{
			_arena = (NetplayArenas) pr.Byte();

			pr.Short(); // number of participants. Maximum of 2 players for now (1v1). 2v2 could be a fun alternate game mode in the future.

			_players = new List<int>
			{
				pr.Int(), // ID of player 1
				pr.Int()  // ID of player 2
			};
		}

		public bool  IsRunning { get; set; }
		public Arena Arena     { get; set; }

		public void BeginLoading(BattleClient client, [NotNull] Dictionary<NetplayArenas, SceneReference> arenaScenes)
		{
			string[] slotLayouts =
			{
				"player",
				"enemy"
			};

			_recipe = new Networked1V1Recipe();

			// Iterate the networked battle's players.
			for (var i = 0; i < _players.Count; i++)
			{
				int participantID = _players[i];

				bool isClient = participantID == NetworkState.ClientID;
				if (isClient)
				{
					// The client is participating in the battle. i.e. not a spectator
					_recipe.player = new Networked1V1Recipe.Player(client, slotLayouts[i]);
				}
				else
				{
					_recipe.remote = new Networked1V1Recipe.Remote(slotLayouts[i], NetworkInformation.Clients[participantID].teamRecipe);
					_recipe.remote.OnCreatingController += brain =>
					{
						AddNetworkController(participantID, brain);
					};
				}
			}

			// Load the arena, then signal to the server that we're ready to start.
			_arenaScene = arenaScenes[_arena];
			SceneLoader.GetDriverScene<Arena>(_arenaScene,
				(scene, arena) =>
				{
					NetworkState.Battle.Arena = arena;

					ServerPacketCreator.Begin();
					ServerPacketCreator.BATTLE_CLIENT_READY();
					ServerPacketCreator.End(client);
				});
		}

		public void AddNetworkController(int clientID, NetplayBrain netplayBrain)
		{
			if (IsRunning)
			{
				Debug.LogError("Cannot add a remote team brain while the battle is already in progress.");
				return;
			}

			_remoteTeamControllers.Add(clientID, netplayBrain);
		}

		public NetplayBrain GetRemoteController(int id) => _remoteTeamControllers[id];

		public void BeginExecution(BattleClient client)
		{
			throw new NotImplementedException();
			// var setup = new Startup.BattleInfo(_recipe) {arena = Arena};
			//
			// setup.core.Starting += battle =>
			// {
			// 	_netplayChip = new NetplayChip
			// 	{
			// 		Client                = client,
			// 		RemoteTeamControllers = _remoteTeamControllers
			// 	};
			//
			// 	setup.core.AddChip(_netplayChip);
			// };
		}

		public void OnRemoteCommand(int clientID, PacketReader pr)
		{
			_netplayChip.OnRemoteCommandReceived(clientID, pr);
		}

		public void Unload([CanBeNull] Action onUnloaded = null)
		{
			SceneLoader.Unload(_arenaScene).OnDone(onUnloaded);
		}
	}
}
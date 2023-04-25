using System;
using NanokinBattleNet.Library.Utilities;
using Sirenix.OdinInspector;
using Telepathy;
using UnityEngine;
using EventType = Telepathy.EventType;
using Logger = Telepathy.Logger;

namespace Util.Network
{
	public abstract class NetworkClient : SerializedMonoBehaviour
	{
		[SerializeField] private string _ip;

		private          Client       _client       = new Client();
		private readonly PacketReader _packetReader = new PacketReader();
		private          bool         _isConnecting;

		public void Connect()
		{
			Logger.Log        = Debug.Log;
			Logger.LogWarning = Debug.LogWarning;
			Logger.LogError   = Debug.LogError;

			_client.Connect(_ip, 6460);
			_isConnecting = true;

			OnStartConnecting();
		}

		public void Send(byte[] bytes)
		{
			_client.Send(bytes);
		}

		private void OnApplicationQuit()
		{
			_client.Disconnect();
		}

		private void Update()
		{
			if (_isConnecting && !_client.Connecting && !_client.Connected)
			{
				_isConnecting = false;
				OnConnectionFailed();
			}

			if (!_client.Connected)
				return;

			while (_client.GetNextMessage(out Message msg))
			{
				switch (msg.eventType)
				{
					case EventType.Connected:
						OnConnected();
						break;

					case EventType.Data:
						_packetReader.Begin(msg.data);
						OnDataReceived(_packetReader);
						break;

					case EventType.Disconnected:
						OnDisconnected();
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			OnUpdate();
		}

		protected virtual void OnUpdate()
		{ }

		protected virtual void OnStartConnecting()
		{ }

		protected virtual void OnConnectionFailed()
		{ }

		protected virtual void OnConnected()
		{ }

		protected virtual void OnDataReceived(PacketReader reader)
		{ }

		protected virtual void OnDisconnected()
		{ }
	}
}
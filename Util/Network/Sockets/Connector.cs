using System;
using System.Net.Sockets;

namespace Util.Network
{
	public class Connector
	{
		public Socket socket;

		private readonly string ip;
        private readonly int port;

        public event Action<Session> OnConnected;
        public event Action<SocketError> OnError;

		public bool isReady = true;

        public Connector(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void Connect()
        {
	        isReady = false;
	        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	        socket.BeginConnect(ip, port, EndConnect, socket);
        }

        private void EndConnect(IAsyncResult result)
        {
            Socket sock = (Socket)result.AsyncState;

	        if (!sock.Connected)
		        isReady = true;

            try
            {
                sock.EndConnect(result);

                Session session = new Session(sock);
				session.Diconnected += () => isReady = true;

                OnConnected?.Invoke(session);

                session.Start();
            }
            catch (SocketException se)
            {
                using (sock)
                    OnError?.Invoke(se.SocketErrorCode);
            }
        }
	}
}


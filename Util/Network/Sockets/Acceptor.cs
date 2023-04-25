using System;
using System.Net;
using System.Net.Sockets;

namespace Util.Network
{
    public sealed class Acceptor : IDisposable
    {
        private Socket socket;
        private bool isDisposed;

        public Action<Session> OnClientAccepted;

        public short Port { get; }

        public Acceptor(IPAddress address, short port)
        {
            Port = port;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(address, port));

            isDisposed = false;
        }

        public void Start()
        {
            socket.Listen(10);
            socket.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult iar)
        {
            if (isDisposed) { return; }

            try
            {
                Socket client = socket.EndAccept(iar);
                var session = new Session(client);
	            OnClientAccepted?.Invoke(session);

	            session.Start();
            }
            finally
            {
                if (isDisposed == false)
                   socket.BeginAccept(AcceptCallback, null);
            }

        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                socket.Dispose();
            }
        }
    }
}

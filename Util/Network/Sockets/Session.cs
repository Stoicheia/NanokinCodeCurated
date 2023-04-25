using System;
using System.Net.Sockets;

namespace Util.Network
{
    public sealed class Session : IDisposable
    {
        public const short ReceiveSize = 1024;

        private readonly Socket socket;

        private object sync;

        private byte[] recvBuffer, packetBuffer;
        private int m_cursor;

        private object locker;
        private bool isDisposed;

        public event Action<byte[]> PacketReceived;
        public event Action Diconnected;

	    public Session(Socket socket)
        {
            this.socket = socket;

            sync = new object();

            recvBuffer = new byte[ReceiveSize];
            packetBuffer = new byte[ReceiveSize];
            m_cursor = 0;

            locker = new object();

            isDisposed = false;
        }

        internal void Start()
        {
            WaitForData();
        }

        private void WaitForData()
        {
            if (isDisposed) { return; }

	        socket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, out SocketError error, PacketCallback, null);

            if (error != SocketError.Success)
            {
                Dispose();
            }
        }

        private void Append(int length)
        {
            if (packetBuffer.Length - m_cursor < length)
            {
                int newSize = packetBuffer.Length * 2;

                while (newSize < m_cursor + length)
                    newSize *= 2;

                Array.Resize(ref packetBuffer, newSize);
            }

            Cipher(recvBuffer, 0, length); //cipha

            Buffer.BlockCopy(recvBuffer, 0, packetBuffer, m_cursor, length);



            m_cursor += length;
        }
        private void HandleStream()
        {
            while (m_cursor > 4) //header room
            {
                int packetSize = BitConverter.ToInt32(packetBuffer, 0);

                if (packetSize < 2 || packetSize > 4096) //illegal
                {
                    Dispose();
                    return;
                }

                if (m_cursor < packetSize + 4) //header + packet room
                    break;

                byte[] buffer = new byte[packetSize];
                Buffer.BlockCopy(this.packetBuffer, 4, buffer, 0, packetSize); //copy packet

                m_cursor -= packetSize + 4; //fix len

                if (m_cursor > 0) //move reamining bytes
                {
                    Buffer.BlockCopy(packetBuffer, packetSize + 4, packetBuffer, 0, m_cursor);
                }

	            PacketReceived?.Invoke(buffer);
            }

        }

        private void PacketCallback(IAsyncResult iar)
        {
            if (isDisposed) { return; }

            SocketError error;
            int length = socket.EndReceive(iar, out error);

            if (length == 0 || error != SocketError.Success)
            {
                Dispose();
            }
            else
            {
                Append(length);
                HandleStream();
                WaitForData();
            }
        }

        public void Send(byte[] packet)
        {
            if (isDisposed) { return; }

            lock (locker)
            {
                int length = packet.Length;

                byte[] final = new byte[length + 4];
                Buffer.BlockCopy(packet, 0, final, 4, length);

                var buf = BitConverter.GetBytes(length);
                Buffer.BlockCopy(buf, 0, final, 0, buf.Length);

                Cipher(final, 0, final.Length); //cipha

                SendRaw(final);
            }
        }
        private void SendRaw(byte[] final)
        {
            if (isDisposed) { return; }

            int offset = 0;

            while (offset < final.Length)
            {
	            int sent = socket.Send(final, offset, final.Length - offset, SocketFlags.None, out SocketError outError);

                if (sent == 0 || outError != SocketError.Success)
                {
                    Dispose();
                    return;
                }

                offset += sent;
            }
        }

        public override string ToString() => socket?.RemoteEndPoint.ToString() ?? base.ToString();

        private static void Cipher(byte[] buffer,int start,int length)
        {
//            for (int i = start; i < length; i++)
//                buffer[i] ^= 69;
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (!isDisposed)
                {
                    isDisposed = true;

                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    catch { }
                    finally
                    {
                        packetBuffer = null;
                        recvBuffer = null;
                        m_cursor = 0;

                        locker = null;

	                    Diconnected?.Invoke();
                    }
                }
            }
        }
    }
}
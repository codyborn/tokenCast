using TokenCastWebApp.Managers.Interfaces;

namespace TokenCastWebApp.Models
{
    public sealed class SocketClientMessage
    {
        public SocketClientMessage(IWebSocketConnection connection, byte[] payload)
        {
            Connection = connection;
            Payload = payload;
        }

        public IWebSocketConnection Connection { get; }
        public byte[] Payload { get; }
    }
}

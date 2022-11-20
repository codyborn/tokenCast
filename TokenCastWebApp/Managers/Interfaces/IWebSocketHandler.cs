using TokenCastWebApp.Models;

namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IWebSocketHandler
    {
        void HandleMessage(SocketClientMessage message);
        void HandleDisconnection(IWebSocketConnection connection);
    }
}

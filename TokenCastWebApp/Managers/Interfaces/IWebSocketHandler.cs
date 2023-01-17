using TokenCastWebApp.Models;

namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IWebSocketHandler
    {
        void HandleMessage(SocketClientMessage message);
        void HandleDisconnection(IWebSocketConnection connection);
    }

    public interface IStatusWebSocketHandler
    {
        void HandleMessage(StatusSocketClientMessage message);
        void HandleDisconnection(IStatusWebSocketConnection connection);
    }
}

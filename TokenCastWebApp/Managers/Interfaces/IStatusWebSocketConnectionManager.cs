using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using TokenCastWebApp.Models;
using System.Collections.Generic;
using TokenCast;

namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IStatusWebSocketConnectionManager
    {
        string GenerateConnectionId(string address);

        bool TryGetDeviceId(string connectionId, out string address);

        Task ConnectAsync(string connectionId, string address, WebSocket webSocket, CancellationToken cancellationToken);

        void SendMessage(string address, ClientMessageResponse message);
    }
}

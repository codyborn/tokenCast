using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using TokenCastWebApp.Models;
using System.Collections.Generic;

namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IStatusWebSocketConnectionManager
    {
        string GenerateConnectionId(List<string> deviceIds);

        bool TryGetDeviceId(string connectionId, out List<string> deviceIds);

        Task ConnectAsync(string connectionId, List<string> deviceIds, WebSocket webSocket, CancellationToken cancellationToken);

        void SendMessage(List<string> deviceIds, ClientMessageResponse message);
    }
}

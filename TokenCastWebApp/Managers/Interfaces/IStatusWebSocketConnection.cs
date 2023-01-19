using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IStatusWebSocketConnection : IDisposable, IEquatable<IWebSocketConnection>
    {
        string ConnectionId { get; }

        List<string> DeviceIds { get; }
        string Address { get; }

        void Send(byte[] message);

        Task StartReceiveMessageAsync();
    }
}

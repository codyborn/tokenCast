using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System;
using TokenCastWebApp.Managers.Interfaces;
using TokenCastWebApp.Models;
using TokenCastWebApp.Socket;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using TokenCast;

namespace TokenCastWebApp.Managers
{
    public sealed class StatusWebSocketConnectionManager : IStatusWebSocketConnectionManager, IStatusWebSocketHandler
    {
        #region Private members

        private readonly IMemoryCache _tempSessionIdCache;
        private readonly ILogger<IStatusWebSocketConnectionManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RealtimeOptions _realtimeOptions;
        private readonly ISystemTextJsonSerializer _serializer;
        private readonly IDatabase _database;

        private ConcurrentDictionary<string, IStatusWebSocketConnection> _webSockets = new ConcurrentDictionary<string, IStatusWebSocketConnection>();

       
        #endregion

        #region Constructor

        public StatusWebSocketConnectionManager(ILogger<IStatusWebSocketConnectionManager> logger,
            ILoggerFactory loggerFactory,
            IOptions<RealtimeOptions> realtimeOptions,
            ISystemTextJsonSerializer serializer,
            IDatabase database)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _realtimeOptions = realtimeOptions.Value;
            _serializer = serializer;
            _database = database;

            var cacheOptions = new MemoryCacheOptions { ExpirationScanFrequency = TimeSpan.FromSeconds(_realtimeOptions.ExpirationScanFrequency) };
            _tempSessionIdCache = new MemoryCache(cacheOptions, _loggerFactory);
        }

        #endregion

        #region IWebSocketConnectionManager members

        public string GenerateConnectionId(string address)
        {
            var connectionId = Guid.NewGuid().ToString();

            _tempSessionIdCache.Set(connectionId, address, TimeSpan.FromSeconds(_realtimeOptions.CacheItemExpirationTime));

            return connectionId;
        }

        public bool TryGetDeviceId(string connectionId, out string address)
        {
            var exist = _tempSessionIdCache.TryGetValue(connectionId, out address);

            if (exist)
                _tempSessionIdCache.Remove(connectionId);

            return exist;
        }

        public async Task ConnectAsync(string connectionId, string address, WebSocket webSocket, CancellationToken cancellationToken)
        {
            var webSocketConnection = new StatusWebSocketConnection(connectionId, address, webSocket, cancellationToken, _loggerFactory, this, _serializer, _database);

            _logger.LogInformation($"New WebSocket session {webSocketConnection.ConnectionId} connected.");

            _webSockets.TryAdd(address.ToUpperInvariant(), webSocketConnection);

            await webSocketConnection.StartReceiveMessageAsync().ConfigureAwait(false);
        }

        #endregion

        #region IWebSocketHandler members

        public void HandleDisconnection(IStatusWebSocketConnection connection)
        {
            _webSockets.TryRemove(connection.Address, out var conn);
        }

        public void HandleMessage(StatusSocketClientMessage message)
        {
            var response = new ClientMessageResponse();
            try
            {
                _logger.LogInformation($"Start handle socket client message.");
                var subscribeMessage = ConvertBytesToMessage(message.Payload);

                if (subscribeMessage == default)
                {
                    response.Success = false;
                    response.Message = "Invalid request model";
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                response.Success = false;
                response.Message = ex.Message;
            }
            finally
            {
                var bytes = ConvertMessageToBytes(response);
                message.Connection.Send(bytes);
            }
        }

        public void SendMessage(string deviceId, ClientMessageResponse message)
        {
            message.DeviceId = deviceId;
            var accounts = _database.GetAllAccount().GetAwaiter().GetResult();
            var address = accounts.FirstOrDefault(x => x.devices.Contains(deviceId));

            if(_webSockets.TryGetValue(address.address.ToUpperInvariant(), out var webSocketConnection))
            {
                webSocketConnection.Send(ConvertMessageToBytes(message));
            }

        }

        #endregion

        #region Private methods

        private byte[] ConvertMessageToBytes<T>(T message)
        {
            return _serializer.SerializeToBytes(message);
        }

        private ClientMessageRequest ConvertBytesToMessage(byte[] messageBytes)
        {
            try
            {
                var request = _serializer.Deserialize<ClientMessageRequest>(messageBytes);
                if (request == null)
                    throw new InvalidDataException("Invalid data");

                return request;
            }
            catch (JsonException ex)
            {
                return null;
            }
        }

        #endregion
    }
}

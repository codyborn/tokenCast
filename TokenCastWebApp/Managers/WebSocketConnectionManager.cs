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

namespace TokenCastWebApp.Managers
{
    public sealed class WebSocketConnectionManager : IWebSocketConnectionManager, IWebSocketHandler
    {
        #region Private members

        private readonly IMemoryCache _tempSessionIdCache;
        private readonly ILogger<IWebSocketConnectionManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RealtimeOptions _realtimeOptions;
        private readonly ISystemTextJsonSerializer _serializer;

        private ConcurrentDictionary<string, IWebSocketConnection> _webSockets = new ConcurrentDictionary<string, IWebSocketConnection>();


        #endregion

        #region Constructor

        public WebSocketConnectionManager(ILogger<IWebSocketConnectionManager> logger,
            ILoggerFactory loggerFactory,
            IOptions<RealtimeOptions> realtimeOptions,
            ISystemTextJsonSerializer serializer)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _realtimeOptions = realtimeOptions.Value;
            _serializer = serializer;

            var cacheOptions = new MemoryCacheOptions { ExpirationScanFrequency = TimeSpan.FromSeconds(_realtimeOptions.ExpirationScanFrequency) };
            _tempSessionIdCache = new MemoryCache(cacheOptions, _loggerFactory);
        }

        #endregion

        #region IWebSocketConnectionManager members

        public string GenerateConnectionId(string deviceId)
        {
            var connectionId = Guid.NewGuid().ToString();

            _tempSessionIdCache.Set(connectionId, deviceId, TimeSpan.FromSeconds(_realtimeOptions.CacheItemExpirationTime));

            return connectionId;
        }

        public bool TryGetDeviceId(string connectionId, out string deviceId)
        {
            var exist = _tempSessionIdCache.TryGetValue(connectionId, out deviceId);

            if (exist)
                _tempSessionIdCache.Remove(connectionId);

            return exist;
        }

        public async Task ConnectAsync(string connectionId, string deviceId, WebSocket webSocket, CancellationToken cancellationToken)
        {
            var webSocketConnection = new WebSocketConnection(connectionId, deviceId, webSocket, cancellationToken, _loggerFactory, this, _serializer);

            _logger.LogInformation($"New WebSocket session {webSocketConnection.ConnectionId} connected.");

            _webSockets.TryAdd(deviceId, webSocketConnection);

            await webSocketConnection.StartReceiveMessageAsync().ConfigureAwait(false);
        }

        #endregion

        #region IWebSocketHandler members

        public void HandleDisconnection(IWebSocketConnection connection)
        {
            _webSockets.TryRemove(connection.DeviceId, out var conn);
        }

        public void HandleMessage(SocketClientMessage message)
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

                if(subscribeMessage.Action == SubscribeAction.UpdateNft)
                {
                    response.Event = EventType.NFTUpdated;
                    response.Message = "Event raised!";
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
            if(_webSockets.TryGetValue(deviceId, out var connection))
            {
                connection.Send(ConvertMessageToBytes(message));
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

using Microsoft.Extensions.Logging;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System;
using TokenCastWebApp.Managers.Interfaces;
using TokenCastWebApp.Processors;
using TokenCastWebApp.Models;
using System.Timers;

namespace TokenCastWebApp.Socket
{
    public sealed class WebSocketConnection : IWebSocketConnection, IDisposable
    {
        #region Private members

        private const int _receivePayloadBufferSize = 4096; // 4KB

        private bool _isDisposed;

        private readonly WebSocket _webSocket;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger<IWebSocketConnection> _logger;
        private readonly QueueProcessor<byte[]> _messagesQueueProcessor;
        private readonly IWebSocketHandler _handler;
        private readonly System.Timers.Timer _timer;
        private readonly ISystemTextJsonSerializer _serializer;

        #endregion

        #region Constructor

        public WebSocketConnection(string connectionId,
        string deviceId,
        WebSocket webSocket,
            CancellationToken cancellationToken,
            ILoggerFactory loggerFactory,
            IWebSocketHandler handler,
            ISystemTextJsonSerializer serializer)
        {
            if(string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (webSocket == null)
            {
                throw new ArgumentNullException(nameof(webSocket));
            }

            if(handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _timer = new System.Timers.Timer();
            _timer.Interval = 15000;
            _timer.Elapsed += _timer_Elapsed;
            ConnectionId = connectionId;
            DeviceId = deviceId;
            _webSocket = webSocket;
            _cancellationToken = cancellationToken;
            _logger = loggerFactory.CreateLogger<IWebSocketConnection>();
            _handler = handler;
            _serializer = serializer;
            _messagesQueueProcessor = new QueueProcessor<byte[]>(ProcessSendAsync, loggerFactory.CreateLogger<QueueProcessor<byte[]>>());
        }

        #endregion

        #region IWebSocketConnection members

        public string ConnectionId { get; }

        public string DeviceId { get; }

        public void Send(byte[] message)
        {
            if (_isDisposed)
                return;

            _messagesQueueProcessor.OnQueueItemReceived(message);
        }

        public Task StartReceiveMessageAsync()
        {
            if (_isDisposed)
                return Task.CompletedTask;

            _timer.Start();

            return ReceiveMessagesUntilCloseAsync();
        }

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _webSocket?.Dispose();
                _timer?.Dispose();
                //maybe queue processor clear
            }
        }

        #endregion

        #region IEquatable members

        public bool Equals(IWebSocketConnection other)
        {
            if (other is null)
                return false;

            return ConnectionId == other.ConnectionId;
        }

        public override bool Equals(object other) => Equals(other as IWebSocketConnection);

        public override int GetHashCode() => ConnectionId.GetHashCode();

        #endregion

        #region Private methods

        private Task ProcessSendAsync(byte[] message)
        {
            if (_webSocket.State == WebSocketState.Open && !_isDisposed)
            {
                try
                {
                    //will extend this for supporting binary messages
                    return _webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    //Send operation was canceled that indicates about session was disconnected
                    _logger.LogInformation($"Send operation was canceled. Session {ConnectionId}.");

                    return DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while sending message to session {ConnectionId}. Error: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }

        private async Task ReceiveMessagesUntilCloseAsync()
        {
            try
            {
                byte[] buffer = new byte[_receivePayloadBufferSize];

                WebSocketReceiveResult webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (webSocketReceiveResult.MessageType != WebSocketMessageType.Close)
                {
                    byte[] webSocketMessage = await ReceiveMessagePayloadAsync(webSocketReceiveResult, buffer);

                    if (webSocketReceiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        _handler.HandleMessage(new SocketClientMessage(this, webSocketMessage));
                    }

                    webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await DisconnectAsync(webSocketReceiveResult.CloseStatus.Value, webSocketReceiveResult.CloseStatusDescription);
            }
            catch (Exception ex)
            {
                if (ex is WebSocketException wsException)
                    _logger.LogError(ex, $"WebSocketException occured. WebSocketError: {wsException.WebSocketErrorCode}. Message: {wsException.Message}");
                else
                    _logger.LogError(ex, ex.Message);

                await DisconnectAsync();
            }
        }

        private async Task<byte[]> ReceiveMessagePayloadAsync(WebSocketReceiveResult webSocketReceiveResult, byte[] receivePayloadBuffer)
        {
            byte[] messagePayload = null;

            if (webSocketReceiveResult.EndOfMessage)
            {
                messagePayload = new byte[webSocketReceiveResult.Count];
                Array.Copy(receivePayloadBuffer, messagePayload, webSocketReceiveResult.Count);
            }
            else
            {
                using (MemoryStream messagePayloadStream = new MemoryStream())
                {
                    messagePayloadStream.Write(receivePayloadBuffer, 0, webSocketReceiveResult.Count);

                    while (!webSocketReceiveResult.EndOfMessage)
                    {
                        webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receivePayloadBuffer), CancellationToken.None);
                        messagePayloadStream.Write(receivePayloadBuffer, 0, webSocketReceiveResult.Count);
                    }

                    messagePayload = messagePayloadStream.ToArray();
                }
            }

            return messagePayload;
        }

        private async Task DisconnectAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string closeStatusDescription = "Normal closure.")
        {
            try
            {
                _timer?.Stop();
                await _webSocket.CloseAsync(closeStatus, closeStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Safe disconnect failed for session {ConnectionId}. Reason: {ex.Message}");
            }
            finally
            {
                Dispose();
            }

            _handler.HandleDisconnection(this);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Send(ConvertMessageToBytes(new ClientMessageResponse { Event = EventType.Heartbeat, Message = "Event raised!", Success = true }));
        }

        private byte[] ConvertMessageToBytes<T>(T message)
        {
            return _serializer.SerializeToBytes(message);
        }

        #endregion
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using TokenCastWebApp.Managers.Interfaces;
using System.Linq;

namespace TokenCastWebApp.Middlewares
{
    public sealed class WebSocketMiddleware
    {
        private const string ConnectionIdQueryParamName = "connectionId";
        private const string DeviceIdQueryParamName = "deviceId";
        private const string CookieTokenParamName = "token";

        private readonly IWebSocketConnectionManager _connectionManager;

        public WebSocketMiddleware(RequestDelegate next,
            IWebSocketConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            string connectionId;
            string deviceId;

            var cancellationToken = context.RequestAborted;


            if (!TryGetConnectionIdQueryParam(context, out connectionId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            if (!TryGetDeviceIdQueryParam(context, out deviceId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await _connectionManager.ConnectAsync(connectionId, deviceId, webSocket, cancellationToken);
        }

        #region Private methods

        private bool TryGetConnectionIdQueryParam(HttpContext context, out string connectionId)
        {
            connectionId = null;
            var connectionIdParameter = context.Request.Query.FirstOrDefault(t =>
                string.Equals(t.Key,
                    ConnectionIdQueryParamName,
                    StringComparison.CurrentCultureIgnoreCase));

            if (!connectionIdParameter.Equals(default(KeyValuePair<string, StringValues>)))
            {
                connectionId = connectionIdParameter.Value;
            }

            return !string.IsNullOrEmpty(connectionId);
        }

        private bool TryGetDeviceIdQueryParam(HttpContext context, out string deviceId)
        {
            deviceId = null;
            var connectionIdParameter = context.Request.Query.FirstOrDefault(t =>
                string.Equals(t.Key,
                    DeviceIdQueryParamName,
                    StringComparison.CurrentCultureIgnoreCase));

            if (!connectionIdParameter.Equals(default(KeyValuePair<string, StringValues>)))
            {
                deviceId = connectionIdParameter.Value;
            }

            return !string.IsNullOrEmpty(deviceId);
        }

        private bool TryGetCookieTokenParam(HttpContext context, out string token)
        {
            context.Request.Cookies.TryGetValue(CookieTokenParamName, out token);

            return !string.IsNullOrEmpty(token);
        }

        #endregion
    }
}

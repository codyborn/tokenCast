using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TokenCastWebApp.Managers.Interfaces;

namespace TokenCastWebApp.Controllers
{
    [Route("connect")]
    public class ConnectController : Controller
    {
        private readonly IWebSocketConnectionManager _webSocketConnectionManager;

        public ConnectController(IWebSocketConnectionManager webSocketConnectionManager)
        {
            _webSocketConnectionManager = webSocketConnectionManager;
        }

        [HttpPost("create")]
        public Task<IActionResult> CreateAsync([FromQuery] string deviceId)
        {
            var connectionId = _webSocketConnectionManager.GenerateConnectionId(deviceId);
            return Task.FromResult<IActionResult>(Ok(new
            {
                ConnectionId = connectionId
            }));
        }

        [HttpGet]
        public async Task<IActionResult> ConnectAsync([FromQuery] string connectionId, [FromQuery] string deviceId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
                return Ok();

            var cancellationToken = HttpContext.RequestAborted;


            if (string.IsNullOrWhiteSpace(connectionId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Forbid();
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await _webSocketConnectionManager.ConnectAsync(connectionId, deviceId, webSocket, cancellationToken);

            return Ok();
        }

        #region Private methods

        #endregion
    }
}

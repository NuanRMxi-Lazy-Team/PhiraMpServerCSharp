using Microsoft.AspNetCore.Mvc;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServerCSharpWebApi.Services;

namespace PhiraMpServerCSharpWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly ILogger<ServerController> _logger;
    private readonly PhiraMpClientService _clientService;

    public ServerController(
        ILogger<ServerController> logger,
        PhiraMpClientService clientService)
    {
        _logger = logger;
        _clientService = clientService;
    }

    /// <summary>
    /// 获取所有服务器的连接状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetConnectionStatus()
    {
        var statuses = _clientService.Clients.Select(kvp => new
        {
            serverName = kvp.Key,
            isConnected = kvp.Value.IsConnected,
            status = kvp.Value.IsConnected ? "Connected" : "Disconnected"
        }).ToList();

        return Ok(new
        {
            totalServers = statuses.Count,
            connectedServers = statuses.Count(s => s.isConnected),
            servers = statuses
        });
    }

    /// <summary>
    /// 获取指定服务器的连接状态
    /// </summary>
    [HttpGet("{serverName}/status")]
    public IActionResult GetServerStatus(string serverName)
    {
        var client = _clientService.GetClient(serverName);
        if (client == null)
        {
            return NotFound(new { error = $"Server '{serverName}' not found" });
        }

        return Ok(new
        {
            serverName,
            isConnected = client.IsConnected,
            status = client.IsConnected ? "Connected" : "Disconnected"
        });
    }

    /// <summary>
    /// 获取指定服务器的详细状态信息
    /// </summary>
    [HttpGet("{serverName}/details")]
    public async Task<IActionResult> GetServerStatusDetails(string serverName)
    {
        try
        {
            var client = _clientService.GetClient(serverName);
            if (client == null)
            {
                return NotFound(new { error = $"Server '{serverName}' not found" });
            }

            if (!client.IsConnected)
            {
                return StatusCode(503, new { error = $"Server '{serverName}' is not connected" });
            }

            var command = new GetServerStatusCommand();
            var response = await client.SendCommandAndWaitAsync(command);

            if (response is GetServerStatusResponse statusResponse)
            {
                return Ok(new
                {
                    success = true,
                    uptime = statusResponse.Uptime.TotalSeconds,
                    maxPlayers = statusResponse.MaxPlayers,
                    currentPlayers = statusResponse.CurrentPlayers,
                    externalAddress = statusResponse.ExternalAddress,
                    isConnected = client.IsConnected
                });
            }

            return Ok(new
            {
                success = false,
                message = "Unexpected response type"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server status for '{ServerName}'", serverName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 设置指定服务器的全局房间最大玩家数
    /// </summary>
    [HttpPut("{serverName}/maxplayers")]
    public async Task<IActionResult> SetRoomMaxPlayers(string serverName, [FromBody] SetMaxPlayersRequest request)
    {
        try
        {
            var client = _clientService.GetClient(serverName);
            if (client == null)
            {
                return NotFound(new { error = $"Server '{serverName}' not found" });
            }

            if (!client.IsConnected)
            {
                return StatusCode(503, new { error = $"Server '{serverName}' is not connected" });
            }

            if (request.MaxPlayers <= 0)
            {
                return BadRequest(new { error = "MaxPlayers must be greater than 0" });
            }

            var command = new SetServerRoomMaxPlayersCommand
            {
                MaxPlayers = request.MaxPlayers
            };

            var response = await client.SendCommandAndWaitAsync(command);

            if (response is SetServerRoomMaxPlayersResponse setMaxPlayersResponse)
            {
                return Ok(new
                {
                    success = setMaxPlayersResponse.IsSuccess,
                    maxPlayers = request.MaxPlayers,
                    message = $"服务器 '{serverName}' 的全局房间最大玩家数已设置",
                    token = response.Token
                });
            }

            return Ok(new
            {
                success = false,
                message = "Unexpected response type",
                token = response.Token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set global room max players for server '{ServerName}'", serverName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 设置所有服务器的全局房间最大玩家数
    /// </summary>
    [HttpPut("maxplayers")]
    public async Task<IActionResult> SetAllServersMaxPlayers([FromBody] SetMaxPlayersRequest request)
    {
        try
        {
            if (request.MaxPlayers <= 0)
            {
                return BadRequest(new { error = "MaxPlayers must be greater than 0" });
            }

            var results = new List<object>();

            foreach (var (serverName, client) in _clientService.Clients)
            {
                try
                {
                    if (!client.IsConnected)
                    {
                        results.Add(new
                        {
                            serverName,
                            success = false,
                            error = "Server not connected"
                        });
                        continue;
                    }

                    var command = new SetServerRoomMaxPlayersCommand
                    {
                        MaxPlayers = request.MaxPlayers
                    };

                    var response = await client.SendCommandAndWaitAsync(command);

                    if (response is SetServerRoomMaxPlayersResponse setMaxPlayersResponse)
                    {
                        results.Add(new
                        {
                            serverName,
                            success = setMaxPlayersResponse.IsSuccess,
                            maxPlayers = request.MaxPlayers
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            serverName,
                            success = false,
                            error = "Unexpected response type"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set max players for server '{ServerName}'", serverName);
                    results.Add(new
                    {
                        serverName,
                        success = false,
                        error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                totalServers = _clientService.Clients.Count,
                successCount = results.Count(r => (bool)(r.GetType().GetProperty("success")?.GetValue(r) ?? false)),
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set max players for all servers");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record SetMaxPlayersRequest(int MaxPlayers);


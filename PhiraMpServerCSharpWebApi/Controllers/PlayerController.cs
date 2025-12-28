using Microsoft.AspNetCore.Mvc;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServerCSharpWebApi.Services;

namespace PhiraMpServerCSharpWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly ILogger<PlayerController> _logger;
    private readonly PhiraMpClientService _clientService;

    public PlayerController(
        ILogger<PlayerController> logger,
        PhiraMpClientService clientService)
    {
        _logger = logger;
        _clientService = clientService;
    }

    /// <summary>
    /// 获取所有服务器的玩家列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlayers()
    {
        try
        {
            var results = new List<object>();

            foreach (var (serverName, client) in _clientService.Clients)
            {
                try
                {
                    if (!client.IsConnected)
                    {
                        results.Add(new
                        {
                            success = false,
                            error = "Server not connected"
                        });
                        continue;
                    }

                    var command = new GetAllPlayerCommand();
                    var response = await client.SendCommandAndWaitAsync(command);

                    if (response is GetAllPlayerResponse playerResponse)
                    {
                        results.Add(new
                        {
                            serverName,
                            success = true,
                            players = playerResponse.PlayerList
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            success = false,
                            error = "Unexpected response type"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get players from server '{ServerName}'", serverName);
                    results.Add(new
                    {
                        success = false,
                        error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                totalServers = _clientService.Clients.Count,
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all players");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定服务器的玩家列表
    /// </summary>
    [HttpGet("{serverName}")]
    public async Task<IActionResult> GetPlayersByServer(string serverName)
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

            var command = new GetAllPlayerCommand();
            var response = await client.SendCommandAndWaitAsync(command);

            if (response is GetAllPlayerResponse playerResponse)
            {
                return Ok(new
                {
                    success = true,
                    players = playerResponse.PlayerList
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
            _logger.LogError(ex, "Failed to get players from server '{ServerName}'", serverName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServerCSharpWebApi.Services;

namespace PhiraMpServerCSharpWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly ILogger<RoomController> _logger;
    private readonly PhiraMpClientService _clientService;

    public RoomController(
        ILogger<RoomController> logger,
        PhiraMpClientService clientService)
    {
        _logger = logger;
        _clientService = clientService;
    }

    /// <summary>
    /// 获取所有服务器的房间信息
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
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

                    var command = new GetAllRoomCommand();
                    var response = await client.SendCommandAndWaitAsync(command);

                    if (response is GetAllRoomResponse roomResponse)
                    {
                        results.Add(new
                        {
                            success = true,
                            rooms = roomResponse.RoomIdList,
                            roomCount = roomResponse.RoomIdList.Count,
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
                    _logger.LogError(ex, "Failed to get rooms from server '{ServerName}'", serverName);
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
            _logger.LogError(ex, "Failed to get all rooms");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定服务器的房间信息
    /// </summary>
    [HttpGet("{serverName}")]
    public async Task<IActionResult> GetRoomsByServer(string serverName)
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

            var command = new GetAllRoomCommand();
            var response = await client.SendCommandAndWaitAsync(command);

            if (response is GetAllRoomResponse roomResponse)
            {
                return Ok(new
                {
                    success = true,
                    rooms = roomResponse.RoomIdList,
                    roomCount = roomResponse.RoomIdList.Count
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
            _logger.LogError(ex, "Failed to get rooms from server '{ServerName}'", serverName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}



using Microsoft.AspNetCore.Mvc;
using PhiraMpServerCSharpWebApi.Services;

namespace PhiraMpServerCSharpWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    private readonly ILogger<SampleController> _logger;
    private readonly PhiraMpClientService _clientService;

    public SampleController(
        ILogger<SampleController> logger,
        PhiraMpClientService clientService)
    {
        _logger = logger;
        _clientService = clientService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var servers = _clientService.Clients.Select(kvp => new
        {
            name = kvp.Key,
            isConnected = kvp.Value.IsConnected
        }).ToList();

        return Ok(new 
        { 
            message = "Hello from SampleController!", 
            totalServers = servers.Count,
            connectedServers = servers.Count(s => s.isConnected),
            servers,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(new { id, message = $"You requested item with ID: {id}" });
    }

    [HttpPost]
    public IActionResult Post([FromBody] SampleRequest request)
    {
        _logger.LogInformation("Received POST request with name: {Name}", request.Name);
        return Ok(new { message = $"Hello, {request.Name}!" });
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] SampleRequest request)
    {
        return Ok(new { id, message = $"Updated item {id} with name: {request.Name}" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _logger.LogInformation("Deleting item with ID: {Id}", id);
        return Ok(new { message = $"Deleted item with ID: {id}" });
    }
}

public record SampleRequest(string Name);


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RealtimeController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public RealtimeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("ice-servers")]
    public IActionResult GetIceServers()
    {
        var servers = _configuration.GetSection("Realtime:IceServers").Get<List<RealtimeIceServerDto>>() ?? new();
        var validServers = servers
            .Where(s => s.Urls is { Count: > 0 })
            .Select(s => new RealtimeIceServerDto
            {
                Urls = s.Urls.Where(u => !string.IsNullOrWhiteSpace(u)).ToList(),
                Username = s.Username,
                Credential = s.Credential
            })
            .Where(s => s.Urls.Count > 0)
            .ToList();

        if (!validServers.Any())
        {
            validServers =
            [
                new RealtimeIceServerDto { Urls = ["stun:stun.l.google.com:19302"] },
                new RealtimeIceServerDto { Urls = ["stun:stun1.l.google.com:19302"] }
            ];
        }

        return Ok(validServers);
    }
}

public class RealtimeIceServerDto
{
    public List<string> Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Credential { get; set; }
}

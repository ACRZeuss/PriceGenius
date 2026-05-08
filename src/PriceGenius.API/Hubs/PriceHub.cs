using Microsoft.AspNetCore.SignalR;

namespace PriceGenius.API.Hubs;

public class PriceHub : Hub
{
    private readonly ILogger<PriceHub> _logger;

    public PriceHub(ILogger<PriceHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🔌 Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("🔌 Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

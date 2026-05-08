using Microsoft.AspNetCore.SignalR;
using PriceGenius.API.DTOs;
using PriceGenius.API.Services;

namespace PriceGenius.API.BackgroundServices;

public class MarketScannerService : BackgroundService
{
    private readonly ILogger<MarketScannerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IHubContext<Hubs.PriceHub> _hubContext;

    public MarketScannerService(
        ILogger<MarketScannerService> logger,
        IServiceScopeFactory scopeFactory,
        IRabbitMqService rabbitMqService,
        IHubContext<Hubs.PriceHub> hubContext)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _rabbitMqService = rabbitMqService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔍 MarketScannerService started. Scanning every 30 seconds...");

        // Wait for initial startup
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mockService = scope.ServiceProvider.GetRequiredService<IMockCompetitorService>();

                var changes = await mockService.ScanMarketAsync();

                if (changes.Count > 0)
                {
                    _logger.LogInformation("📊 Market scan found {Count} changes", changes.Count);

                    foreach (var change in changes)
                    {
                        // Publish to market_analysis_queue
                        await _rabbitMqService.PublishAsync(RabbitMqService.MarketAnalysisQueue, change);

                        // Send log to frontend via SignalR
                        await _hubContext.Clients.All.SendAsync("LogMessage", new LogMessageDto
                        {
                            Level = "info",
                            Source = "MarketScanner",
                            Message = $"Piyasa değişikliği tespit edildi: {change.ProductName} — {change.ChangeType}",
                            Timestamp = DateTime.UtcNow,
                            Data = new { change.ProductId, change.ChangeType, change.CurrentPrice }
                        }, stoppingToken);
                    }

                    await _hubContext.Clients.All.SendAsync("MarketAlert", new
                    {
                        Count = changes.Count,
                        Timestamp = DateTime.UtcNow,
                        Products = changes.Select(c => new { c.ProductId, c.ProductName, c.ChangeType })
                    }, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("📊 Market scan completed — no changes detected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during market scan");

                await _hubContext.Clients.All.SendAsync("LogMessage", new LogMessageDto
                {
                    Level = "error",
                    Source = "MarketScanner",
                    Message = $"Market scan hatası: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                }, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

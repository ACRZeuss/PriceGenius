using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PriceGenius.API.Data;
using PriceGenius.API.DTOs;
using PriceGenius.API.Models;
using PriceGenius.API.Services;

namespace PriceGenius.API.BackgroundServices;

public class PriceUpdateConsumerService : BackgroundService
{
    private readonly ILogger<PriceUpdateConsumerService> _logger;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<Hubs.PriceHub> _hubContext;

    public PriceUpdateConsumerService(
        ILogger<PriceUpdateConsumerService> logger,
        IRabbitMqService rabbitMqService,
        IServiceScopeFactory scopeFactory,
        IHubContext<Hubs.PriceHub> hubContext)
    {
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("👂 PriceUpdateConsumerService started. Listening on price_update_queue...");

        await _rabbitMqService.SubscribeAsync<PriceUpdateMessage>(
            RabbitMqService.PriceUpdateQueue,
            HandlePriceUpdateAsync,
            stoppingToken);
    }

    private async Task HandlePriceUpdateAsync(PriceUpdateMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = await db.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == message.ProductId);

        if (product == null)
        {
            _logger.LogWarning("⚠️ Product not found: {ProductId}", message.ProductId);
            return;
        }

        var oldPrice = product.CurrentPrice;
        var suggestedPrice = message.SuggestedPrice;
        var appliedPrice = suggestedPrice;
        var wasOverridden = false;

        // --- Safety Rules ---

        // Rule 1: Minimum profit margin check
        var minAllowedPrice = product.CostPrice * (1 + product.Seller.MinProfitMargin / 100);
        if (appliedPrice < minAllowedPrice)
        {
            _logger.LogWarning("🛡️ Safety override: Price {Suggested} TL below min profit margin. Adjusted to {Min} TL",
                suggestedPrice, minAllowedPrice);
            appliedPrice = Math.Round(minAllowedPrice, 2);
            wasOverridden = true;
        }

        // Rule 2: Min price check
        if (appliedPrice < product.MinPrice)
        {
            _logger.LogWarning("🛡️ Safety override: Price {Applied} TL below minimum {Min} TL", appliedPrice, product.MinPrice);
            appliedPrice = product.MinPrice;
            wasOverridden = true;
        }

        // Rule 3: Max price check
        if (appliedPrice > product.MaxPrice)
        {
            _logger.LogWarning("🛡️ Safety override: Price {Applied} TL above maximum {Max} TL", appliedPrice, product.MaxPrice);
            appliedPrice = product.MaxPrice;
            wasOverridden = true;
        }

        // Update product
        product.CurrentPrice = appliedPrice;
        product.UpdatedAt = DateTime.UtcNow;

        // Create price history record
        var priceHistory = new PriceHistory
        {
            ProductId = product.Id,
            OldPrice = oldPrice,
            NewPrice = appliedPrice,
            Reason = message.Reasoning ?? "",
            Strategy = message.Strategy ?? "",
            ChangedAt = DateTime.UtcNow
        };
        db.PriceHistories.Add(priceHistory);

        // Create agent decision record
        var decision = new AgentDecision
        {
            ProductId = product.Id,
            Decision = message.Reasoning ?? "",
            SuggestedPrice = suggestedPrice,
            AppliedPrice = appliedPrice,
            WasOverridden = wasOverridden,
            Status = "Applied",
            Strategy = message.Strategy ?? "",
            ConfidenceScore = message.ConfidenceScore,
            CreatedAt = DateTime.UtcNow
        };
        db.AgentDecisions.Add(decision);

        await db.SaveChangesAsync();

        _logger.LogInformation("✅ Price updated: {Product} — {Old} TL → {New} TL (Strategy: {Strategy}, Override: {Override})",
            product.Name, oldPrice, appliedPrice, message.Strategy, wasOverridden);

        // Send real-time notifications via SignalR
        await _hubContext.Clients.All.SendAsync("PriceUpdated", new
        {
            ProductId = product.Id,
            ProductName = product.Name,
            OldPrice = oldPrice,
            NewPrice = appliedPrice,
            SuggestedPrice = suggestedPrice,
            WasOverridden = wasOverridden,
            Strategy = message.Strategy,
            Timestamp = DateTime.UtcNow
        });

        await _hubContext.Clients.All.SendAsync("NewAgentDecision", new AgentDecisionDto
        {
            Id = decision.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Decision = decision.Decision,
            SuggestedPrice = decision.SuggestedPrice,
            AppliedPrice = decision.AppliedPrice,
            WasOverridden = decision.WasOverridden,
            Status = decision.Status,
            Strategy = decision.Strategy,
            ConfidenceScore = decision.ConfidenceScore,
            CreatedAt = decision.CreatedAt
        });

        var logLevel = wasOverridden ? "warning" : "success";
        await _hubContext.Clients.All.SendAsync("LogMessage", new LogMessageDto
        {
            Level = logLevel,
            Source = "PriceEngine",
            Message = wasOverridden
                ? $"⚠️ {product.Name}: AI {suggestedPrice:F2} TL önerdi, güvenlik kuralı {appliedPrice:F2} TL'ye düzeltti"
                : $"✅ {product.Name}: Fiyat {oldPrice:F2} TL → {appliedPrice:F2} TL güncellendi ({message.Strategy})",
            Timestamp = DateTime.UtcNow,
            Data = new { product.Id, OldPrice = oldPrice, NewPrice = appliedPrice, wasOverridden }
        });
    }
}

public class PriceUpdateMessage
{
    public int ProductId { get; set; }
    public decimal SuggestedPrice { get; set; }
    public string? Strategy { get; set; }
    public string? Reasoning { get; set; }
    public int ConfidenceScore { get; set; }
    public DateTime Timestamp { get; set; }
}

using PriceGenius.API.Data;
using PriceGenius.API.Models;
using Microsoft.EntityFrameworkCore;

namespace PriceGenius.API.Services;

public interface IMockCompetitorService
{
    Task<List<CompetitorChangeEvent>> ScanMarketAsync();
}

public class CompetitorChangeEvent
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MinProfitMargin { get; set; }
    public List<CompetitorSnapshot> Competitors { get; set; } = new();
    public string ChangeType { get; set; } = string.Empty; // "stock_out", "price_drop", "price_increase"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompetitorSnapshot
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public decimal PreviousPrice { get; set; }
    public int PreviousStock { get; set; }
}

public class MockCompetitorService : IMockCompetitorService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MockCompetitorService> _logger;
    private readonly Random _random = new();

    public MockCompetitorService(IServiceScopeFactory scopeFactory, ILogger<MockCompetitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<CompetitorChangeEvent>> ScanMarketAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var changes = new List<CompetitorChangeEvent>();
        var products = await db.Products
            .Include(p => p.Seller)
            .Include(p => p.Competitors)
            .ToListAsync();

        foreach (var product in products)
        {
            // %30 chance of a change for each product per scan
            if (_random.NextDouble() > 0.30)
                continue;

            var changeType = PickRandomChange();
            var hasChanged = false;

            var snapshots = new List<CompetitorSnapshot>();

            foreach (var comp in product.Competitors)
            {
                var prevPrice = comp.Price;
                var prevStock = comp.StockQuantity;

                switch (changeType)
                {
                    case "stock_out":
                        // One random competitor runs out of stock
                        if (_random.NextDouble() > 0.5 && comp.StockQuantity > 0)
                        {
                            comp.StockQuantity = 0;
                            hasChanged = true;
                        }
                        break;

                    case "price_drop":
                        // Random competitor drops price by 5-15%
                        if (_random.NextDouble() > 0.5)
                        {
                            var dropPercent = 0.05m + (decimal)_random.NextDouble() * 0.10m;
                            comp.Price = Math.Round(comp.Price * (1 - dropPercent), 2);
                            hasChanged = true;
                        }
                        break;

                    case "price_increase":
                        // Random competitor increases price by 5-20%
                        if (_random.NextDouble() > 0.5)
                        {
                            var increasePercent = 0.05m + (decimal)_random.NextDouble() * 0.15m;
                            comp.Price = Math.Round(comp.Price * (1 + increasePercent), 2);
                            hasChanged = true;
                        }
                        break;

                    case "restock":
                        // Competitor restocks
                        if (comp.StockQuantity == 0)
                        {
                            comp.StockQuantity = _random.Next(20, 100);
                            hasChanged = true;
                        }
                        break;
                }

                comp.LastChecked = DateTime.UtcNow;

                snapshots.Add(new CompetitorSnapshot
                {
                    Name = comp.Name,
                    Price = comp.Price,
                    StockQuantity = comp.StockQuantity,
                    PreviousPrice = prevPrice,
                    PreviousStock = prevStock
                });
            }

            if (hasChanged)
            {
                changes.Add(new CompetitorChangeEvent
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentPrice = product.CurrentPrice,
                    CostPrice = product.CostPrice,
                    MinProfitMargin = product.Seller.MinProfitMargin,
                    Competitors = snapshots,
                    ChangeType = changeType,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("🔄 Market change detected: {Product} — {ChangeType}", product.Name, changeType);
            }
        }

        await db.SaveChangesAsync();
        return changes;
    }

    private string PickRandomChange()
    {
        var roll = _random.NextDouble();
        return roll switch
        {
            < 0.30 => "stock_out",
            < 0.55 => "price_drop",
            < 0.80 => "price_increase",
            _ => "restock"
        };
    }
}

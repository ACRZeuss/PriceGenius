namespace PriceGenius.API.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int StockQuantity { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public List<PriceHistory> PriceHistories { get; set; } = new();
    public List<Competitor> Competitors { get; set; } = new();
    public List<AgentDecision> AgentDecisions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

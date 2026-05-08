namespace PriceGenius.API.Models;

public class Competitor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
}

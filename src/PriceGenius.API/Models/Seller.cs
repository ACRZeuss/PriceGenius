namespace PriceGenius.API.Models;

public class Seller
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal MinProfitMargin { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

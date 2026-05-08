namespace PriceGenius.API.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int StockQuantity { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal ProfitMargin => CostPrice > 0 ? Math.Round((CurrentPrice - CostPrice) / CostPrice * 100, 2) : 0;
    public List<CompetitorDto> Competitors { get; set; } = new();
}

public class CompetitorDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime LastChecked { get; set; }
}

public class ProductUpdateDto
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? StockQuantity { get; set; }
}

public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int StockQuantity { get; set; }
    public int SellerId { get; set; }
}

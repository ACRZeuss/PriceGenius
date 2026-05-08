namespace PriceGenius.API.DTOs;

public class SellerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal MinProfitMargin { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class SellerCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal MinProfitMargin { get; set; } = 15;
}

public class SellerUpdateDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public decimal? MinProfitMargin { get; set; }
    public bool? IsActive { get; set; }
}

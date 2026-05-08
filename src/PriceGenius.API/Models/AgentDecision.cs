namespace PriceGenius.API.Models;

public class AgentDecision
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public decimal AppliedPrice { get; set; }
    public bool WasOverridden { get; set; }
    public string Status { get; set; } = "Pending";
    public string Strategy { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
}

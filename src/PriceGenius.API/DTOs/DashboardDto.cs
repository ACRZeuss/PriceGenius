namespace PriceGenius.API.DTOs;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int ActiveSellers { get; set; }
    public decimal AverageProfitMargin { get; set; }
    public int TodayPriceChanges { get; set; }
    public int PendingDecisions { get; set; }
    public string AgentStatus { get; set; } = "Active";
}

public class AgentDecisionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public decimal AppliedPrice { get; set; }
    public bool WasOverridden { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PriceHistoryDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public decimal ChangePercent => OldPrice > 0 ? Math.Round((NewPrice - OldPrice) / OldPrice * 100, 2) : 0;
}

public class LogMessageDto
{
    public string Level { get; set; } = "info";
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }
}

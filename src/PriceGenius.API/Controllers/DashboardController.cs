using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceGenius.API.Data;
using PriceGenius.API.DTOs;

namespace PriceGenius.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var today = DateTime.UtcNow.Date;

        var totalProducts = await _db.Products.CountAsync();
        var activeSellers = await _db.Sellers.CountAsync(s => s.IsActive);

        var products = await _db.Products.ToListAsync();
        var avgProfitMargin = products.Count > 0
            ? Math.Round(products.Average(p => p.CostPrice > 0 ? (p.CurrentPrice - p.CostPrice) / p.CostPrice * 100 : 0), 2)
            : 0;

        var todayPriceChanges = await _db.PriceHistories
            .CountAsync(ph => ph.ChangedAt >= today);

        var pendingDecisions = await _db.AgentDecisions
            .CountAsync(d => d.Status == "Pending");

        return Ok(new DashboardSummaryDto
        {
            TotalProducts = totalProducts,
            ActiveSellers = activeSellers,
            AverageProfitMargin = avgProfitMargin,
            TodayPriceChanges = todayPriceChanges,
            PendingDecisions = pendingDecisions,
            AgentStatus = "Active"
        });
    }

    [HttpGet("decisions")]
    public async Task<ActionResult<List<AgentDecisionDto>>> GetDecisions([FromQuery] int take = 20)
    {
        var decisions = await _db.AgentDecisions
            .Include(d => d.Product)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .Select(d => new AgentDecisionDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product.Name,
                Decision = d.Decision,
                SuggestedPrice = d.SuggestedPrice,
                AppliedPrice = d.AppliedPrice,
                WasOverridden = d.WasOverridden,
                Status = d.Status,
                Strategy = d.Strategy,
                ConfidenceScore = d.ConfidenceScore,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(decisions);
    }

    [HttpGet("price-history")]
    public async Task<ActionResult<List<PriceHistoryDto>>> GetPriceHistory([FromQuery] int take = 30)
    {
        var history = await _db.PriceHistories
            .Include(ph => ph.Product)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(take)
            .Select(ph => new PriceHistoryDto
            {
                Id = ph.Id,
                ProductName = ph.Product.Name,
                OldPrice = ph.OldPrice,
                NewPrice = ph.NewPrice,
                Reason = ph.Reason,
                Strategy = ph.Strategy,
                ChangedAt = ph.ChangedAt
            })
            .ToListAsync();

        return Ok(history);
    }
}

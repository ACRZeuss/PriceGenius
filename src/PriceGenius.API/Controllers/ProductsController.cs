using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceGenius.API.Data;
using PriceGenius.API.DTOs;
using PriceGenius.API.Models;

namespace PriceGenius.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Seller)
            .Include(p => p.Competitors)
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                CostPrice = p.CostPrice,
                CurrentPrice = p.CurrentPrice,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                StockQuantity = p.StockQuantity,
                SellerName = p.Seller.Name,
                Competitors = p.Competitors.Select(c => new CompetitorDto
                {
                    Name = c.Name,
                    Price = c.Price,
                    StockQuantity = c.StockQuantity,
                    LastChecked = c.LastChecked
                }).ToList()
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.Seller)
            .Include(p => p.Competitors)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        return Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            CostPrice = product.CostPrice,
            CurrentPrice = product.CurrentPrice,
            MinPrice = product.MinPrice,
            MaxPrice = product.MaxPrice,
            StockQuantity = product.StockQuantity,
            SellerName = product.Seller.Name,
            Competitors = product.Competitors.Select(c => new CompetitorDto
            {
                Name = c.Name,
                Price = c.Price,
                StockQuantity = c.StockQuantity,
                LastChecked = c.LastChecked
            }).ToList()
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        if (dto.MinPrice.HasValue) product.MinPrice = dto.MinPrice.Value;
        if (dto.MaxPrice.HasValue) product.MaxPrice = dto.MaxPrice.Value;
        if (dto.StockQuantity.HasValue) product.StockQuantity = dto.StockQuantity.Value;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<PriceHistoryDto>>> GetHistory(int id)
    {
        var history = await _db.PriceHistories
            .Include(ph => ph.Product)
            .Where(ph => ph.ProductId == id)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(50)
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

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto dto)
    {
        var seller = await _db.Sellers.FindAsync(dto.SellerId);
        if (seller == null)
            return BadRequest("Geçersiz satıcı ID'si.");

        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            CostPrice = dto.CostPrice,
            CurrentPrice = dto.CurrentPrice,
            MinPrice = dto.MinPrice,
            MaxPrice = dto.MaxPrice,
            StockQuantity = dto.StockQuantity,
            SellerId = dto.SellerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // 2. Add Mock Competitors
        var random = new Random();
        var competitors = new List<Competitor>
        {
            new Competitor { ProductId = product.Id, Name = "CompetitorA", Price = Math.Round(product.CurrentPrice * (decimal)(0.90 + random.NextDouble() * 0.20), 2), StockQuantity = random.Next(0, 100), LastChecked = DateTime.UtcNow },
            new Competitor { ProductId = product.Id, Name = "CompetitorB", Price = Math.Round(product.CurrentPrice * (decimal)(0.90 + random.NextDouble() * 0.20), 2), StockQuantity = random.Next(0, 100), LastChecked = DateTime.UtcNow },
            new Competitor { ProductId = product.Id, Name = "CompetitorC", Price = Math.Round(product.CurrentPrice * (decimal)(0.90 + random.NextDouble() * 0.20), 2), StockQuantity = random.Next(0, 100), LastChecked = DateTime.UtcNow }
        };

        _db.Competitors.AddRange(competitors);
        await _db.SaveChangesAsync();

        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            CostPrice = product.CostPrice,
            CurrentPrice = product.CurrentPrice,
            MinPrice = product.MinPrice,
            MaxPrice = product.MaxPrice,
            StockQuantity = product.StockQuantity,
            SellerName = seller.Name,
            Competitors = competitors.Select(c => new CompetitorDto
            {
                Name = c.Name,
                Price = c.Price,
                StockQuantity = c.StockQuantity,
                LastChecked = c.LastChecked
            }).ToList()
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, productDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

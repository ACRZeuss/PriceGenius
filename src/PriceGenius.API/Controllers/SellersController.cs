using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceGenius.API.Data;
using PriceGenius.API.DTOs;
using PriceGenius.API.Models;

namespace PriceGenius.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SellersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SellersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<SellerDto>>> GetAll()
    {
        var sellers = await _db.Sellers
            .Include(s => s.Products)
            .Select(s => new SellerDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                MinProfitMargin = s.MinProfitMargin,
                IsActive = s.IsActive,
                ProductCount = s.Products.Count
            })
            .ToListAsync();

        return Ok(sellers);
    }

    [HttpPost]
    public async Task<ActionResult<SellerDto>> Create([FromBody] SellerCreateDto dto)
    {
        var seller = new Seller
        {
            Name = dto.Name,
            Email = dto.Email,
            MinProfitMargin = dto.MinProfitMargin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Sellers.Add(seller);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new SellerDto
        {
            Id = seller.Id,
            Name = seller.Name,
            Email = seller.Email,
            MinProfitMargin = seller.MinProfitMargin,
            IsActive = seller.IsActive,
            ProductCount = 0
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] SellerUpdateDto dto)
    {
        var seller = await _db.Sellers.FindAsync(id);
        if (seller == null) return NotFound();

        if (dto.Name != null) seller.Name = dto.Name;
        if (dto.Email != null) seller.Email = dto.Email;
        if (dto.MinProfitMargin.HasValue) seller.MinProfitMargin = dto.MinProfitMargin.Value;
        if (dto.IsActive.HasValue) seller.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

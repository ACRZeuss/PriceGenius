using Microsoft.EntityFrameworkCore;
using PriceGenius.API.Models;

namespace PriceGenius.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<AgentDecision> AgentDecisions => Set<AgentDecision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configure decimal precision ---
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.CostPrice).HasPrecision(18, 2);
            e.Property(p => p.CurrentPrice).HasPrecision(18, 2);
            e.Property(p => p.MinPrice).HasPrecision(18, 2);
            e.Property(p => p.MaxPrice).HasPrecision(18, 2);
        });
        modelBuilder.Entity<Seller>(e =>
        {
            e.Property(s => s.MinProfitMargin).HasPrecision(5, 2);
        });
        modelBuilder.Entity<PriceHistory>(e =>
        {
            e.Property(p => p.OldPrice).HasPrecision(18, 2);
            e.Property(p => p.NewPrice).HasPrecision(18, 2);
        });
        modelBuilder.Entity<Competitor>(e =>
        {
            e.Property(c => c.Price).HasPrecision(18, 2);
        });
        modelBuilder.Entity<AgentDecision>(e =>
        {
            e.Property(a => a.SuggestedPrice).HasPrecision(18, 2);
            e.Property(a => a.AppliedPrice).HasPrecision(18, 2);
        });

        // --- Relationships ---
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SellerId);

        modelBuilder.Entity<PriceHistory>()
            .HasOne(ph => ph.Product)
            .WithMany(p => p.PriceHistories)
            .HasForeignKey(ph => ph.ProductId);

        modelBuilder.Entity<Competitor>()
            .HasOne(c => c.Product)
            .WithMany(p => p.Competitors)
            .HasForeignKey(c => c.ProductId);

        modelBuilder.Entity<AgentDecision>()
            .HasOne(ad => ad.Product)
            .WithMany(p => p.AgentDecisions)
            .HasForeignKey(ad => ad.ProductId);

        // --- Seed Data ---
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Sellers
        modelBuilder.Entity<Seller>().HasData(
            new Seller { Id = 1, Name = "TechStore", Email = "info@techstore.com", MinProfitMargin = 15m, IsActive = true, CreatedAt = now },
            new Seller { Id = 2, Name = "GadgetWorld", Email = "sales@gadgetworld.com", MinProfitMargin = 20m, IsActive = true, CreatedAt = now }
        );

        // Products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Mouse", SKU = "WM-001", CostPrice = 80m, CurrentPrice = 120m, MinPrice = 95m, MaxPrice = 200m, StockQuantity = 150, SellerId = 1, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 2, Name = "Mechanical Keyboard", SKU = "MK-002", CostPrice = 200m, CurrentPrice = 350m, MinPrice = 240m, MaxPrice = 500m, StockQuantity = 75, SellerId = 1, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 3, Name = "USB-C Hub", SKU = "UH-003", CostPrice = 120m, CurrentPrice = 180m, MinPrice = 145m, MaxPrice = 300m, StockQuantity = 200, SellerId = 1, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 4, Name = "Webcam HD 1080p", SKU = "WC-004", CostPrice = 150m, CurrentPrice = 250m, MinPrice = 180m, MaxPrice = 400m, StockQuantity = 90, SellerId = 1, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 5, Name = "Bluetooth Headset", SKU = "BH-005", CostPrice = 100m, CurrentPrice = 160m, MinPrice = 120m, MaxPrice = 250m, StockQuantity = 300, SellerId = 1, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 6, Name = "Gaming Monitor 27\"", SKU = "GM-006", CostPrice = 2500m, CurrentPrice = 3800m, MinPrice = 3000m, MaxPrice = 5500m, StockQuantity = 30, SellerId = 2, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 7, Name = "Laptop Stand", SKU = "LS-007", CostPrice = 60m, CurrentPrice = 110m, MinPrice = 75m, MaxPrice = 180m, StockQuantity = 500, SellerId = 2, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 8, Name = "External SSD 1TB", SKU = "ES-008", CostPrice = 400m, CurrentPrice = 600m, MinPrice = 480m, MaxPrice = 900m, StockQuantity = 120, SellerId = 2, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 9, Name = "Smart Power Strip", SKU = "SP-009", CostPrice = 90m, CurrentPrice = 145m, MinPrice = 110m, MaxPrice = 220m, StockQuantity = 250, SellerId = 2, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 10, Name = "Wireless Charger", SKU = "WCH-010", CostPrice = 70m, CurrentPrice = 115m, MinPrice = 85m, MaxPrice = 180m, StockQuantity = 400, SellerId = 2, CreatedAt = now, UpdatedAt = now }
        );

        // Competitors
        modelBuilder.Entity<Competitor>().HasData(
            // Product 1 - Wireless Mouse
            new Competitor { Id = 1, Name = "CompetitorA", ProductId = 1, Price = 115m, StockQuantity = 50, LastChecked = now },
            new Competitor { Id = 2, Name = "CompetitorB", ProductId = 1, Price = 125m, StockQuantity = 30, LastChecked = now },
            new Competitor { Id = 3, Name = "CompetitorC", ProductId = 1, Price = 110m, StockQuantity = 80, LastChecked = now },
            // Product 2 - Mechanical Keyboard
            new Competitor { Id = 4, Name = "CompetitorA", ProductId = 2, Price = 340m, StockQuantity = 20, LastChecked = now },
            new Competitor { Id = 5, Name = "CompetitorB", ProductId = 2, Price = 360m, StockQuantity = 15, LastChecked = now },
            new Competitor { Id = 6, Name = "CompetitorC", ProductId = 2, Price = 330m, StockQuantity = 40, LastChecked = now },
            // Product 3 - USB-C Hub
            new Competitor { Id = 7, Name = "CompetitorA", ProductId = 3, Price = 175m, StockQuantity = 60, LastChecked = now },
            new Competitor { Id = 8, Name = "CompetitorB", ProductId = 3, Price = 190m, StockQuantity = 45, LastChecked = now },
            new Competitor { Id = 9, Name = "CompetitorC", ProductId = 3, Price = 170m, StockQuantity = 100, LastChecked = now },
            // Product 4 - Webcam
            new Competitor { Id = 10, Name = "CompetitorA", ProductId = 4, Price = 240m, StockQuantity = 25, LastChecked = now },
            new Competitor { Id = 11, Name = "CompetitorB", ProductId = 4, Price = 260m, StockQuantity = 10, LastChecked = now },
            new Competitor { Id = 12, Name = "CompetitorC", ProductId = 4, Price = 235m, StockQuantity = 55, LastChecked = now },
            // Product 5 - Bluetooth Headset
            new Competitor { Id = 13, Name = "CompetitorA", ProductId = 5, Price = 155m, StockQuantity = 70, LastChecked = now },
            new Competitor { Id = 14, Name = "CompetitorB", ProductId = 5, Price = 165m, StockQuantity = 40, LastChecked = now },
            new Competitor { Id = 15, Name = "CompetitorC", ProductId = 5, Price = 150m, StockQuantity = 90, LastChecked = now },
            // Product 6 - Gaming Monitor
            new Competitor { Id = 16, Name = "CompetitorA", ProductId = 6, Price = 3700m, StockQuantity = 10, LastChecked = now },
            new Competitor { Id = 17, Name = "CompetitorB", ProductId = 6, Price = 3900m, StockQuantity = 5, LastChecked = now },
            new Competitor { Id = 18, Name = "CompetitorC", ProductId = 6, Price = 3600m, StockQuantity = 15, LastChecked = now },
            // Product 7 - Laptop Stand
            new Competitor { Id = 19, Name = "CompetitorA", ProductId = 7, Price = 105m, StockQuantity = 120, LastChecked = now },
            new Competitor { Id = 20, Name = "CompetitorB", ProductId = 7, Price = 115m, StockQuantity = 80, LastChecked = now },
            new Competitor { Id = 21, Name = "CompetitorC", ProductId = 7, Price = 100m, StockQuantity = 200, LastChecked = now },
            // Product 8 - External SSD
            new Competitor { Id = 22, Name = "CompetitorA", ProductId = 8, Price = 580m, StockQuantity = 35, LastChecked = now },
            new Competitor { Id = 23, Name = "CompetitorB", ProductId = 8, Price = 620m, StockQuantity = 20, LastChecked = now },
            new Competitor { Id = 24, Name = "CompetitorC", ProductId = 8, Price = 570m, StockQuantity = 50, LastChecked = now },
            // Product 9 - Smart Power Strip
            new Competitor { Id = 25, Name = "CompetitorA", ProductId = 9, Price = 140m, StockQuantity = 60, LastChecked = now },
            new Competitor { Id = 26, Name = "CompetitorB", ProductId = 9, Price = 150m, StockQuantity = 40, LastChecked = now },
            new Competitor { Id = 27, Name = "CompetitorC", ProductId = 9, Price = 135m, StockQuantity = 80, LastChecked = now },
            // Product 10 - Wireless Charger
            new Competitor { Id = 28, Name = "CompetitorA", ProductId = 10, Price = 110m, StockQuantity = 100, LastChecked = now },
            new Competitor { Id = 29, Name = "CompetitorB", ProductId = 10, Price = 120m, StockQuantity = 70, LastChecked = now },
            new Competitor { Id = 30, Name = "CompetitorC", ProductId = 10, Price = 105m, StockQuantity = 150, LastChecked = now }
        );
    }
}

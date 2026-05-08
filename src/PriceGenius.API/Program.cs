using Microsoft.EntityFrameworkCore;
using PriceGenius.API.BackgroundServices;
using PriceGenius.API.Data;
using PriceGenius.API.Hubs;
using PriceGenius.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- RabbitMQ (Singleton) ---
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// --- Application Services ---
builder.Services.AddScoped<IMockCompetitorService, MockCompetitorService>();

// --- Background Services ---
builder.Services.AddHostedService<MarketScannerService>();
builder.Services.AddHostedService<PriceUpdateConsumerService>();

// --- SignalR ---
builder.Services.AddSignalR();

// --- Controllers + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PriceGenius API", Version = "v1" });
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- Initialize RabbitMQ ---
var rabbitMqService = app.Services.GetRequiredService<IRabbitMqService>();
_ = Task.Run(async () =>
{
    try
    {
        await rabbitMqService.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize RabbitMQ");
    }
});

// --- Apply migrations and seed data ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply migrations. Ensure PostgreSQL is running.");
    }
}

// --- Middleware Pipeline ---
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseRouting();
app.MapControllers();
app.MapHub<PriceHub>("/pricehub");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

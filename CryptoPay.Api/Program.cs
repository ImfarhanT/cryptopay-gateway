using Microsoft.EntityFrameworkCore;
using CryptoPay.Api.Data;
using CryptoPay.Api.Models;
using CryptoPay.Api.Services;
using CryptoPay.Api.DTOs;
using CryptoPay.Api.Workers;
using CryptoPay.Api.Middleware;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register TronService for blockchain operations
builder.Services.AddSingleton<TronService>();
builder.Services.AddScoped<PaymentIntentService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

builder.Services.AddAuthorization();

// Background worker for monitoring blockchain
builder.Services.AddHostedService<BlockchainPollingWorker>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Apply migrations
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration error: {ex.Message}");
}

// API Endpoints (protected)
var api = app.MapGroup("/v1").RequireAuthorization();

api.MapPost("/intents", async (CreateIntentRequest request, PaymentIntentService service, HttpContext context) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString();
    if (string.IsNullOrEmpty(apiKey))
        return Results.Unauthorized();

    try
    {
        var response = await service.CreateIntentAsync(request, apiKey);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

api.MapGet("/intents/{intentId}", async (Guid intentId, PaymentIntentService service, HttpContext context) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString();
    if (string.IsNullOrEmpty(apiKey))
        return Results.Unauthorized();

    var response = await service.GetIntentAsync(intentId, apiKey);
    return response == null ? Results.NotFound() : Results.Ok(response);
});

// Admin endpoints (unprotected for now - secure in production!)
app.MapPost("/admin/create-merchant", async (CreateMerchantRequest request, ApplicationDbContext db) =>
{
    var merchant = new Merchant
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        ApiKey = request.ApiKey,
        WebhookUrl = request.WebhookUrl,
        WebhookSecret = request.WebhookSecret,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    db.Merchants.Add(merchant);
    await db.SaveChangesAsync();
    return Results.Ok(new { merchantId = merchant.Id, merchant.Name, merchant.ApiKey });
});

app.MapGet("/admin/merchants", async (ApplicationDbContext db) =>
{
    var merchants = await db.Merchants
        .Select(m => new { m.Id, m.Name, m.ApiKey, m.IsActive })
        .ToListAsync();
    return Results.Ok(merchants);
});

app.MapGet("/admin/pending-payments", async (ApplicationDbContext db) =>
{
    var pending = await db.PaymentIntents
        .Where(pi => pi.Status == PaymentIntentStatus.Pending)
        .Select(pi => new { pi.Id, pi.OrderRef, pi.CryptoAmount, pi.PayAddress, pi.Network, pi.ExpiresAt })
        .ToListAsync();
    return Results.Ok(pending);
});

app.MapGet("/admin/check-payment/{intentId}", async (Guid intentId, ApplicationDbContext db, TronService tronService) =>
{
    var intent = await db.PaymentIntents.FindAsync(intentId);
    if (intent == null) return Results.NotFound();

    var sinceTimestamp = ((DateTimeOffset)intent.CreatedAt).ToUnixTimeMilliseconds();
    var tx = await tronService.FindPaymentByAmountAsync(intent.CryptoAmount, sinceTimestamp);
    
    if (tx != null)
    {
        return Results.Ok(new { found = true, tx.TxHash, tx.Amount, tx.FromAddress });
    }
    return Results.Ok(new { found = false, expectedAmount = intent.CryptoAmount });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Cloud Run uses PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

public record CreateMerchantRequest(string Name, string ApiKey, string WebhookUrl, string WebhookSecret);

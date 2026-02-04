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

builder.Services.AddScoped<PaymentIntentService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<IBlockchainProvider, TronProvider>();
builder.Services.AddScoped<IBlockchainProvider, EvmProvider>();
builder.Services.AddScoped<BlockchainProviderFactory>();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

builder.Services.AddAuthorization();

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
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
}

// API Endpoints
var api = app.MapGroup("/v1").RequireAuthorization();

api.MapPost("/intents", async (CreateIntentRequest request, PaymentIntentService service, HttpContext context) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    try
    {
        var response = await service.CreateIntentAsync(request, apiKey);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateIntent")
.WithOpenApi();

api.MapGet("/intents/{intentId}", async (Guid intentId, PaymentIntentService service, HttpContext context) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].ToString();
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    var response = await service.GetIntentAsync(intentId, apiKey);
    if (response == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(response);
})
.WithName("GetIntent")
.WithOpenApi();

// Admin endpoint for seeding addresses (should be protected in production)
app.MapPost("/admin/seed-addresses", async (SeedAddressesRequest request, ApplicationDbContext db) =>
{
    // TODO: Add proper authentication/authorization for admin endpoints
    // For now, this is unprotected - secure it in production!
    
    for (int i = 0; i < request.Count; i++)
    {
        var address = new WalletAddress
        {
            Id = Guid.NewGuid(),
            Network = request.Network,
            Address = request.Addresses[i],
            IsAssigned = false
        };
        db.WalletAddresses.Add(address);
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { message = $"Seeded {request.Count} addresses for {request.Network}" });
})
.WithName("SeedAddresses")
.WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Cloud Run uses PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

public record SeedAddressesRequest(string Network, int Count, List<string> Addresses);

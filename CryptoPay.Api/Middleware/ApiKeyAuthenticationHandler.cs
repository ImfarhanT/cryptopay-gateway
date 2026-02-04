using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CryptoPay.Api.Data;

namespace CryptoPay.Api.Middleware;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApplicationDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.ApiKey == apiKey && m.IsActive);

        if (merchant == null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, merchant.Id.ToString()),
            new Claim("MerchantId", merchant.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

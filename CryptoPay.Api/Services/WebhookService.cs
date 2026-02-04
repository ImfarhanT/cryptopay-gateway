using System.Security.Cryptography;
using System.Text;
using CryptoPay.Api.Data;
using CryptoPay.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoPay.Api.Services;

public class WebhookService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendWebhookAsync(PaymentIntent intent)
    {
        if (intent.Status != PaymentIntentStatus.Paid)
        {
            return;
        }

        var merchant = await _db.Merchants.FindAsync(intent.MerchantId);
        if (merchant == null || string.IsNullOrEmpty(merchant.WebhookUrl))
        {
            _logger.LogWarning("Merchant {MerchantId} has no webhook URL configured", intent.MerchantId);
            return;
        }

        // Idempotency: check if webhook was already sent successfully
        if (intent.LastWebhookStatus == "sent" && intent.LastWebhookSentAt.HasValue)
        {
            _logger.LogInformation("Webhook already sent for intent {IntentId}, skipping", intent.Id);
            return;
        }

        var payload = new
        {
            eventType = "payment.paid",
            intentId = intent.Id.ToString(),
            orderRef = intent.OrderRef,
            status = "PAID",
            cryptoAmount = intent.CryptoAmount,
            cryptoCurrency = intent.CryptoCurrency,
            network = intent.Network,
            txHash = intent.TxHash,
            confirmations = intent.Confirmations,
            paidAt = intent.PaidAt
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = ComputeHmacSignature(json, merchant.WebhookSecret);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CryptoPay-Signature", signature);
        client.DefaultRequestHeaders.Add("X-CryptoPay-Event", "payment.paid");

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(merchant.WebhookUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                intent.LastWebhookStatus = "sent";
                intent.LastWebhookSentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Webhook sent successfully for intent {IntentId}", intent.Id);
            }
            else
            {
                intent.LastWebhookStatus = "failed";
                _logger.LogWarning("Webhook failed for intent {IntentId}: {StatusCode}", intent.Id, response.StatusCode);
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            intent.LastWebhookStatus = "failed";
            _logger.LogError(ex, "Error sending webhook for intent {IntentId}", intent.Id);
            await _db.SaveChangesAsync();
        }
    }

    private string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

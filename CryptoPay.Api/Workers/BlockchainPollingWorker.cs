using CryptoPay.Api.Data;
using CryptoPay.Api.Models;
using CryptoPay.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CryptoPay.Api.Workers;

public class BlockchainPollingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockchainPollingWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(15);

    public BlockchainPollingWorker(IServiceProvider serviceProvider, ILogger<BlockchainPollingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blockchain Polling Worker started");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingPaymentsAsync();
                await ExpireOldIntentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in blockchain polling worker");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task CheckPendingPaymentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tronService = scope.ServiceProvider.GetRequiredService<TronService>();
        var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();

        // Get all pending TRC20 intents
        var pendingIntents = await db.PaymentIntents
            .Include(pi => pi.Merchant)
            .Where(pi => pi.Status == PaymentIntentStatus.Pending && 
                        pi.Network == "TRC20" &&
                        pi.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        if (!pendingIntents.Any())
        {
            return;
        }

        _logger.LogInformation("Checking {Count} pending TRC20 payments", pendingIntents.Count);

        // Get oldest pending intent timestamp for filtering transactions
        var oldestIntent = pendingIntents.MinBy(pi => pi.CreatedAt);
        var sinceTimestamp = ((DateTimeOffset)(oldestIntent?.CreatedAt ?? DateTime.UtcNow.AddHours(-1))).ToUnixTimeMilliseconds();

        // Get recent transactions from TronGrid
        var payAddress = tronService.GetPaymentAddress();
        var transactions = await tronService.GetTrc20TransactionsAsync(payAddress, sinceTimestamp);

        _logger.LogInformation("Found {Count} recent TRC20 transactions", transactions.Count);

        foreach (var intent in pendingIntents)
        {
            // Look for a transaction matching this intent's unique amount
            var matchingTx = transactions.FirstOrDefault(tx =>
                tx.ToAddress.Equals(intent.PayAddress, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(tx.Amount - intent.CryptoAmount) < 0.01m && // Tolerance for rounding
                tx.Timestamp >= ((DateTimeOffset)intent.CreatedAt).ToUnixTimeMilliseconds());

            if (matchingTx != null)
            {
                _logger.LogInformation("Found payment for intent {IntentId}: {TxHash} - {Amount} USDT",
                    intent.Id, matchingTx.TxHash, matchingTx.Amount);

                // Update intent status
                intent.Status = PaymentIntentStatus.Paid;
                intent.TxHash = matchingTx.TxHash;
                intent.PaidAt = DateTime.UtcNow;
                intent.Confirmations = 1; // TRC20 confirms fast

                await db.SaveChangesAsync();

                // Send webhook notification
                try
                {
                    await webhookService.SendPaymentConfirmedWebhookAsync(intent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send webhook for intent {IntentId}", intent.Id);
                }
            }
        }
    }

    private async Task ExpireOldIntentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredIntents = await db.PaymentIntents
            .Where(pi => pi.Status == PaymentIntentStatus.Pending && pi.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var intent in expiredIntents)
        {
            intent.Status = PaymentIntentStatus.Expired;
            _logger.LogInformation("Expired payment intent {IntentId}", intent.Id);
        }

        if (expiredIntents.Any())
        {
            await db.SaveChangesAsync();
        }
    }
}

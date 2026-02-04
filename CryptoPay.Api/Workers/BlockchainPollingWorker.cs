using CryptoPay.Api.Data;
using CryptoPay.Api.Models;
using CryptoPay.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CryptoPay.Api.Workers;

public class BlockchainPollingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockchainPollingWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(20);

    public BlockchainPollingWorker(
        IServiceProvider serviceProvider,
        ILogger<BlockchainPollingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingIntentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in blockchain polling worker");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingIntentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<BlockchainProviderFactory>();
        var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();

        var pendingIntents = await db.PaymentIntents
            .Where(pi => pi.Status == PaymentIntentStatus.Pending && pi.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var intent in pendingIntents)
        {
            try
            {
                var provider = providerFactory.GetProvider(intent.Network);
                var fromUtc = intent.CreatedAt.AddMinutes(-5); // Check transactions from 5 minutes before intent creation
                
                var recentTxs = await provider.GetRecentIncomingTxAsync(intent.PayAddress, fromUtc);
                
                // Find matching transaction
                var matchingTx = recentTxs.FirstOrDefault(tx =>
                    tx.ToAddress.Equals(intent.PayAddress, StringComparison.OrdinalIgnoreCase) &&
                    Math.Abs(tx.Amount - intent.CryptoAmount) < 0.000001m && // Allow small rounding differences
                    tx.Timestamp >= intent.CreatedAt);

                if (matchingTx != null)
                {
                    // Check confirmations threshold
                    var confirmationsThreshold = intent.Network.Equals("TRC20", StringComparison.OrdinalIgnoreCase) ? 1 : 6;
                    var confirmations = await provider.GetConfirmationsAsync(matchingTx.TxHash);
                    
                    if (confirmations >= confirmationsThreshold)
                    {
                        // Mark as paid
                        intent.Status = PaymentIntentStatus.Paid;
                        intent.TxHash = matchingTx.TxHash;
                        intent.Confirmations = confirmations;
                        intent.PaidAt = DateTime.UtcNow;
                        
                        await db.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Payment intent {IntentId} marked as PAID. TxHash: {TxHash}, Confirmations: {Confirmations}",
                            intent.Id, matchingTx.TxHash, confirmations);

                        // Send webhook
                        await webhookService.SendWebhookAsync(intent);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Payment intent {IntentId} has transaction but insufficient confirmations: {Confirmations}/{Threshold}",
                            intent.Id, confirmations, confirmationsThreshold);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing intent {IntentId}", intent.Id);
            }
        }

        // Mark expired intents
        var expiredIntents = await db.PaymentIntents
            .Where(pi => pi.Status == PaymentIntentStatus.Pending && pi.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var intent in expiredIntents)
        {
            intent.Status = PaymentIntentStatus.Expired;
            _logger.LogInformation("Payment intent {IntentId} expired", intent.Id);
        }

        if (expiredIntents.Any())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

using CryptoPay.Api.Models;

namespace CryptoPay.Api.Services;

public class TronProvider : IBlockchainProvider
{
    private readonly ILogger<TronProvider> _logger;

    public TronProvider(ILogger<TronProvider> logger)
    {
        _logger = logger;
    }

    public bool SupportsNetwork(string network)
    {
        return network.Equals("TRC20", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ChainTx>> GetRecentIncomingTxAsync(string address, DateTime fromUtc)
    {
        // TODO: Implement TRON API integration
        // Use TronGrid API or TronWeb to query USDT TRC20 transactions
        // Example: https://api.trongrid.io/v1/accounts/{address}/transactions/trc20
        // Filter by USDT contract address: TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t
        // Parse response and map to ChainTx objects
        
        _logger.LogWarning("TronProvider.GetRecentIncomingTxAsync not implemented - returning empty list");
        await Task.CompletedTask;
        return new List<ChainTx>();
    }

    public async Task<int> GetConfirmationsAsync(string txHash)
    {
        // TODO: Implement TRON confirmation check
        // Query TronGrid API for transaction details
        // Calculate confirmations based on block height
        
        _logger.LogWarning("TronProvider.GetConfirmationsAsync not implemented - returning 0");
        await Task.CompletedTask;
        return 0;
    }
}

using CryptoPay.Api.Models;

namespace CryptoPay.Api.Services;

public class EvmProvider : IBlockchainProvider
{
    private readonly ILogger<EvmProvider> _logger;
    private readonly string _rpcUrl;

    public EvmProvider(ILogger<EvmProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _rpcUrl = configuration["Blockchain:EvmRpcUrl"] ?? "https://mainnet.infura.io/v3/YOUR_KEY";
    }

    public bool SupportsNetwork(string network)
    {
        return network.Equals("ERC20", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ChainTx>> GetRecentIncomingTxAsync(string address, DateTime fromUtc)
    {
        // TODO: Implement EVM RPC integration
        // Use JSON-RPC calls to query USDT ERC20 transfers
        // USDT contract: 0xdAC17F958D2ee523a2206206994597C13D831ec7 (Ethereum mainnet)
        // Use eth_getLogs to filter Transfer events to the target address
        // Parse logs and map to ChainTx objects
        
        _logger.LogWarning("EvmProvider.GetRecentIncomingTxAsync not implemented - returning empty list");
        await Task.CompletedTask;
        return new List<ChainTx>();
    }

    public async Task<int> GetConfirmationsAsync(string txHash)
    {
        // TODO: Implement EVM confirmation check
        // Use eth_getTransactionReceipt to get block number
        // Get current block number with eth_blockNumber
        // Calculate confirmations = currentBlock - txBlock
        
        _logger.LogWarning("EvmProvider.GetConfirmationsAsync not implemented - returning 0");
        await Task.CompletedTask;
        return 0;
    }
}

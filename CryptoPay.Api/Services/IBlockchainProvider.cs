using CryptoPay.Api.Models;

namespace CryptoPay.Api.Services;

public interface IBlockchainProvider
{
    Task<List<ChainTx>> GetRecentIncomingTxAsync(string address, DateTime fromUtc);
    Task<int> GetConfirmationsAsync(string txHash);
    bool SupportsNetwork(string network);
}

public class ChainTx
{
    public string TxHash { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public int Confirmations { get; set; }
}

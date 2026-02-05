using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoPay.Api.Services;

public class EthereumService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EthereumService> _logger;
    private readonly string _apiKey;
    private readonly string _adminAddress;
    private readonly string _usdtContractAddress = "0xdAC17F958D2ee523a2206206994597C13D831ec7"; // USDT ERC20 mainnet

    public EthereumService(IConfiguration configuration, ILogger<EthereumService> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _apiKey = configuration["Ethereum:ApiKey"] ?? "";
        _adminAddress = configuration["Ethereum:AdminAddress"] ?? "";
    }

    public string GetPaymentAddress()
    {
        return _adminAddress;
    }

    public async Task<List<EthTransaction>> GetErc20TransactionsAsync(string address, long minTimestamp = 0)
    {
        try
        {
            // Use Etherscan API to get ERC20 token transfers
            var url = $"https://api.etherscan.io/api?module=account&action=tokentx&address={address}&contractaddress={_usdtContractAddress}&sort=desc&apikey={_apiKey}";
            
            if (minTimestamp > 0)
            {
                // Convert milliseconds to seconds for Etherscan
                var startBlock = await GetBlockNumberByTimestampAsync(minTimestamp / 1000);
                if (startBlock > 0)
                {
                    url += $"&startblock={startBlock}";
                }
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Etherscan Response: {Content}", content.Substring(0, Math.Min(500, content.Length)));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Etherscan API error: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<EthTransaction>();
            }

            var result = JsonSerializer.Deserialize<EtherscanResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result?.Status != "1" || result?.Result == null)
            {
                _logger.LogWarning("Etherscan returned no results or error: {Message}", result?.Message);
                return new List<EthTransaction>();
            }

            return result.Result
                .Where(tx => tx.To?.Equals(address, StringComparison.OrdinalIgnoreCase) == true)
                .Select(tx => new EthTransaction
                {
                    TxHash = tx.Hash ?? "",
                    FromAddress = tx.From ?? "",
                    ToAddress = tx.To ?? "",
                    Amount = ParseUsdtAmount(tx.Value ?? "0"),
                    Timestamp = long.TryParse(tx.TimeStamp, out var ts) ? ts * 1000 : 0, // Convert to milliseconds
                    BlockNumber = long.TryParse(tx.BlockNumber, out var bn) ? bn : 0,
                    Confirmations = int.TryParse(tx.Confirmations, out var conf) ? conf : 0
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ERC20 transactions");
            return new List<EthTransaction>();
        }
    }

    private async Task<long> GetBlockNumberByTimestampAsync(long timestamp)
    {
        try
        {
            var url = $"https://api.etherscan.io/api?module=block&action=getblocknobytime&timestamp={timestamp}&closest=before&apikey={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<EtherscanBlockResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Status == "1" && long.TryParse(result.Result, out var blockNumber))
            {
                return blockNumber;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block number by timestamp");
        }
        return 0;
    }

    public async Task<EthTransaction?> FindPaymentByAmountAsync(decimal expectedAmount, long sinceTimestamp)
    {
        var transactions = await GetErc20TransactionsAsync(_adminAddress, sinceTimestamp);
        
        var tolerance = 0.01m;
        var matchingTx = transactions.FirstOrDefault(tx => 
            tx.ToAddress.Equals(_adminAddress, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(tx.Amount - expectedAmount) < tolerance &&
            tx.Timestamp >= sinceTimestamp);

        return matchingTx;
    }

    private decimal ParseUsdtAmount(string value)
    {
        // USDT has 6 decimals on Ethereum
        if (decimal.TryParse(value, out var amount))
        {
            return amount / 1_000_000m;
        }
        return 0;
    }

    public decimal GenerateUniqueAmount(decimal baseAmount)
    {
        var random = new Random();
        var cents = random.Next(1, 99) / 100m;
        return Math.Round(baseAmount + cents, 2);
    }
}

public class EthTransaction
{
    public string TxHash { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
    public long Timestamp { get; set; }
    public long BlockNumber { get; set; }
    public int Confirmations { get; set; }
}

public class EtherscanResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public List<EtherscanTx>? Result { get; set; }
}

public class EtherscanTx
{
    [JsonPropertyName("blockNumber")]
    public string? BlockNumber { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public string? TimeStamp { get; set; }
    
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }
    
    [JsonPropertyName("from")]
    public string? From { get; set; }
    
    [JsonPropertyName("to")]
    public string? To { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("tokenSymbol")]
    public string? TokenSymbol { get; set; }
    
    [JsonPropertyName("confirmations")]
    public string? Confirmations { get; set; }
}

public class EtherscanBlockResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? Result { get; set; }
}

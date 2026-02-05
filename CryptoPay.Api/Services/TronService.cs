using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoPay.Api.Services;

public class TronService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TronService> _logger;
    private readonly string _apiKey;
    private readonly string _privateKey;
    private readonly string _adminAddress;
    private readonly string _usdtContractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT TRC20 mainnet

    public TronService(IConfiguration configuration, ILogger<TronService> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _apiKey = configuration["Tron:ApiKey"] ?? "";
        _privateKey = configuration["Tron:PrivateKey"] ?? "";
        _adminAddress = configuration["Tron:AdminAddress"] ?? "";
        
        _httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", _apiKey);
    }

    public string GetPaymentAddress()
    {
        // Return the admin address - all payments go here
        // We identify payments by unique amounts
        return _adminAddress;
    }

    public async Task<List<TronTransaction>> GetTrc20TransactionsAsync(string address, long minTimestamp = 0)
    {
        try
        {
            var url = $"https://api.trongrid.io/v1/accounts/{address}/transactions/trc20?limit=50&contract_address={_usdtContractAddress}";
            if (minTimestamp > 0)
            {
                url += $"&min_timestamp={minTimestamp}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("TronGrid Response: {Content}", content.Substring(0, Math.Min(500, content.Length)));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TronGrid API error: {StatusCode} - {Content}", response.StatusCode, content);
                return new List<TronTransaction>();
            }

            var result = JsonSerializer.Deserialize<TronGridTrc20Response>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result?.Data == null)
                return new List<TronTransaction>();

            return result.Data.Select(tx => new TronTransaction
            {
                TxHash = tx.TransactionId ?? "",
                FromAddress = tx.From ?? "",
                ToAddress = tx.To ?? "",
                Amount = ParseUsdtAmount(tx.Value ?? "0"),
                Timestamp = tx.BlockTimestamp,
                TokenSymbol = tx.TokenInfo?.Symbol ?? "USDT"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TRC20 transactions");
            return new List<TronTransaction>();
        }
    }

    public async Task<TronTransaction?> FindPaymentByAmountAsync(decimal expectedAmount, long sinceTimestamp)
    {
        var transactions = await GetTrc20TransactionsAsync(_adminAddress, sinceTimestamp);
        
        // Find transaction matching the expected amount (with small tolerance for rounding)
        var tolerance = 0.001m;
        var matchingTx = transactions.FirstOrDefault(tx => 
            tx.ToAddress.Equals(_adminAddress, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(tx.Amount - expectedAmount) < tolerance);

        return matchingTx;
    }

    private decimal ParseUsdtAmount(string value)
    {
        // USDT has 6 decimals on TRON
        if (decimal.TryParse(value, out var amount))
        {
            return amount / 1_000_000m;
        }
        return 0;
    }

    public decimal GenerateUniqueAmount(decimal baseAmount)
    {
        // Add random cents (0.01 to 0.99) to make amount unique
        var random = new Random();
        var cents = random.Next(1, 99) / 100m;
        return Math.Round(baseAmount + cents, 2);
    }
}

public class TronTransaction
{
    public string TxHash { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
    public long Timestamp { get; set; }
    public string TokenSymbol { get; set; } = "";
}

public class TronGridTrc20Response
{
    public List<TronGridTrc20Tx>? Data { get; set; }
    public bool Success { get; set; }
}

public class TronGridTrc20Tx
{
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
    
    [JsonPropertyName("from")]
    public string? From { get; set; }
    
    [JsonPropertyName("to")]
    public string? To { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("block_timestamp")]
    public long BlockTimestamp { get; set; }
    
    [JsonPropertyName("token_info")]
    public TronTokenInfo? TokenInfo { get; set; }
}

public class TronTokenInfo
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }
}

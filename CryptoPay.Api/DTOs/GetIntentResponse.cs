namespace CryptoPay.Api.DTOs;

public class GetIntentResponse
{
    public string IntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayAddress { get; set; } = string.Empty;
    public decimal CryptoAmount { get; set; }
    public string? TxHash { get; set; }
    public int? Confirmations { get; set; }
    public DateTime ExpiresAt { get; set; }
}

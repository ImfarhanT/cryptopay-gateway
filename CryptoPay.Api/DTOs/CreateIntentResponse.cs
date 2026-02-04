namespace CryptoPay.Api.DTOs;

public class CreateIntentResponse
{
    public string IntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayAddress { get; set; } = string.Empty;
    public decimal CryptoAmount { get; set; }
    public string QrString { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

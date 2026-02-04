namespace CryptoPay.Api.Models;

public class PaymentIntent
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    
    public string OrderRef { get; set; } = string.Empty;
    public string FiatCurrency { get; set; } = string.Empty;
    public decimal FiatAmount { get; set; }
    public string CryptoCurrency { get; set; } = string.Empty; // USDT
    public string Network { get; set; } = string.Empty; // TRC20, ERC20
    public string? CustomerEmail { get; set; }
    public string? ReturnUrl { get; set; }
    
    public PaymentIntentStatus Status { get; set; } = PaymentIntentStatus.Pending;
    public string PayAddress { get; set; } = string.Empty;
    public decimal CryptoAmount { get; set; }
    public string? TxHash { get; set; }
    public int? Confirmations { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    
    public string? LastWebhookStatus { get; set; }
    public DateTime? LastWebhookSentAt { get; set; }
}

public enum PaymentIntentStatus
{
    Pending,
    Paid,
    Expired,
    Failed
}

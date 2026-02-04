namespace CryptoPay.Api.DTOs;

public class CreateIntentRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public string OrderRef { get; set; } = string.Empty;
    public string FiatCurrency { get; set; } = string.Empty;
    public decimal FiatAmount { get; set; }
    public string CryptoCurrency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? ReturnUrl { get; set; }
}

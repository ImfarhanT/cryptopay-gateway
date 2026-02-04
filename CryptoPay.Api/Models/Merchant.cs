namespace CryptoPay.Api.Models;

public class Merchant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<PaymentIntent> PaymentIntents { get; set; } = new List<PaymentIntent>();
}

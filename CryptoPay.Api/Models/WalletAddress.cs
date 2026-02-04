namespace CryptoPay.Api.Models;

public class WalletAddress
{
    public Guid Id { get; set; }
    public string Network { get; set; } = string.Empty; // TRC20, ERC20
    public string Address { get; set; } = string.Empty;
    public bool IsAssigned { get; set; } = false;
    public Guid? PaymentIntentId { get; set; }
    public PaymentIntent? PaymentIntent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

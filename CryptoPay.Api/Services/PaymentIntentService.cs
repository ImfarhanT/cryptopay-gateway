using CryptoPay.Api.Data;
using CryptoPay.Api.Models;
using CryptoPay.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CryptoPay.Api.Services;

public class PaymentIntentService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentIntentService> _logger;
    private readonly BlockchainProviderFactory _providerFactory;
    private readonly IConfiguration _configuration;

    public PaymentIntentService(
        ApplicationDbContext db,
        ILogger<PaymentIntentService> logger,
        BlockchainProviderFactory providerFactory,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _providerFactory = providerFactory;
        _configuration = configuration;
    }

    public async Task<CreateIntentResponse> CreateIntentAsync(CreateIntentRequest request, string apiKey)
    {
        // Validate merchant
        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.Id == Guid.Parse(request.MerchantId) && m.ApiKey == apiKey && m.IsActive);
        
        if (merchant == null)
        {
            throw new UnauthorizedAccessException("Invalid merchant or API key");
        }

        // Check for duplicate order reference (idempotency)
        var existingIntent = await _db.PaymentIntents
            .FirstOrDefaultAsync(pi => pi.MerchantId == merchant.Id && pi.OrderRef == request.OrderRef);
        
        if (existingIntent != null)
        {
            return MapToCreateResponse(existingIntent);
        }

        // Get available address
        var address = await GetAvailableAddressAsync(request.Network);
        if (address == null)
        {
            throw new InvalidOperationException($"No available addresses for network {request.Network}");
        }

        // Calculate crypto amount (simplified - in production, use real-time exchange rates)
        var cryptoAmount = await CalculateCryptoAmountAsync(request.FiatAmount, request.FiatCurrency, request.CryptoCurrency);

        // Create intent
        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            MerchantId = merchant.Id,
            OrderRef = request.OrderRef,
            FiatCurrency = request.FiatCurrency,
            FiatAmount = request.FiatAmount,
            CryptoCurrency = request.CryptoCurrency,
            Network = request.Network,
            CustomerEmail = request.CustomerEmail,
            ReturnUrl = request.ReturnUrl,
            Status = PaymentIntentStatus.Pending,
            PayAddress = address.Address,
            CryptoAmount = cryptoAmount,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30 minute expiry
        };

        address.IsAssigned = true;
        address.PaymentIntentId = intent.Id;

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created payment intent {IntentId} for merchant {MerchantId}", intent.Id, merchant.Id);

        return MapToCreateResponse(intent);
    }

    public async Task<GetIntentResponse?> GetIntentAsync(Guid intentId, string apiKey)
    {
        var intent = await _db.PaymentIntents
            .Include(pi => pi.Merchant)
            .FirstOrDefaultAsync(pi => pi.Id == intentId && pi.Merchant.ApiKey == apiKey);
        
        if (intent == null)
        {
            return null;
        }

        return new GetIntentResponse
        {
            IntentId = intent.Id.ToString(),
            Status = intent.Status.ToString().ToUpper(),
            PayAddress = intent.PayAddress,
            CryptoAmount = intent.CryptoAmount,
            TxHash = intent.TxHash,
            Confirmations = intent.Confirmations,
            ExpiresAt = intent.ExpiresAt
        };
    }

    private CreateIntentResponse MapToCreateResponse(PaymentIntent intent)
    {
        var qrString = GenerateQrCode(intent.PayAddress, intent.CryptoAmount, intent.Network);
        
        return new CreateIntentResponse
        {
            IntentId = intent.Id.ToString(),
            Status = intent.Status.ToString().ToUpper(),
            PayAddress = intent.PayAddress,
            CryptoAmount = intent.CryptoAmount,
            QrString = qrString,
            ExpiresAt = intent.ExpiresAt
        };
    }

    private string GenerateQrCode(string address, decimal amount, string network)
    {
        // Generate payment URI based on network
        string uri;
        if (network.Equals("TRC20", StringComparison.OrdinalIgnoreCase))
        {
            // TRON payment URI format
            uri = $"tron:{address}?amount={amount}";
        }
        else if (network.Equals("ERC20", StringComparison.OrdinalIgnoreCase))
        {
            // Ethereum payment URI format
            uri = $"ethereum:{address}?value={amount}";
        }
        else
        {
            uri = address;
        }

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeBytes);
    }

    private async Task<WalletAddress?> GetAvailableAddressAsync(string network)
    {
        return await _db.WalletAddresses
            .FirstOrDefaultAsync(wa => wa.Network == network && !wa.IsAssigned);
    }

    private async Task<decimal> CalculateCryptoAmountAsync(decimal fiatAmount, string fiatCurrency, string cryptoCurrency)
    {
        // TODO: Integrate with exchange rate API (e.g., CoinGecko, Binance)
        // For MVP, use a hardcoded rate or configuration
        var rate = _configuration.GetValue<decimal>($"ExchangeRates:{fiatCurrency}:{cryptoCurrency}", 1.0m);
        await Task.CompletedTask;
        return fiatAmount / rate;
    }
}

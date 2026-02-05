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
    private readonly TronService _tronService;
    private readonly EthereumService _ethereumService;
    private readonly IConfiguration _configuration;

    public PaymentIntentService(
        ApplicationDbContext db,
        ILogger<PaymentIntentService> logger,
        TronService tronService,
        EthereumService ethereumService,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _tronService = tronService;
        _ethereumService = ethereumService;
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

        // Get payment address based on network
        var payAddress = GetPaymentAddressForNetwork(request.Network);
        
        if (string.IsNullOrEmpty(payAddress))
        {
            throw new InvalidOperationException($"No admin address configured for {request.Network} network");
        }

        // Calculate crypto amount with unique identifier
        var baseCryptoAmount = await CalculateCryptoAmountAsync(request.FiatAmount, request.FiatCurrency, request.CryptoCurrency);
        
        // Make amount unique by adding random cents (for payment identification)
        var uniqueCryptoAmount = GenerateUniqueAmountForNetwork(baseCryptoAmount, request.Network);
        
        // Ensure this exact amount isn't already pending for this network
        while (await _db.PaymentIntents.AnyAsync(pi => 
            pi.Status == PaymentIntentStatus.Pending && 
            pi.CryptoAmount == uniqueCryptoAmount &&
            pi.Network == request.Network))
        {
            uniqueCryptoAmount = GenerateUniqueAmountForNetwork(baseCryptoAmount, request.Network);
        }

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
            PayAddress = payAddress,
            CryptoAmount = uniqueCryptoAmount,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created payment intent {IntentId} for {Amount} USDT ({Network}) to {Address}", 
            intent.Id, uniqueCryptoAmount, request.Network, payAddress);

        return MapToCreateResponse(intent);
    }

    private string GetPaymentAddressForNetwork(string network)
    {
        return network.ToUpperInvariant() switch
        {
            "TRC20" => _tronService.GetPaymentAddress(),
            "ERC20" => _ethereumService.GetPaymentAddress(),
            _ => _tronService.GetPaymentAddress() // Default to TRC20
        };
    }

    private decimal GenerateUniqueAmountForNetwork(decimal baseAmount, string network)
    {
        return network.ToUpperInvariant() switch
        {
            "TRC20" => _tronService.GenerateUniqueAmount(baseAmount),
            "ERC20" => _ethereumService.GenerateUniqueAmount(baseAmount),
            _ => _tronService.GenerateUniqueAmount(baseAmount)
        };
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
        var qrString = GenerateQrCode(intent.PayAddress);
        
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

    private string GenerateQrCode(string address)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(address, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(qrCodeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return "";
        }
    }

    private async Task<decimal> CalculateCryptoAmountAsync(decimal fiatAmount, string fiatCurrency, string cryptoCurrency)
    {
        var rate = _configuration.GetValue<decimal>($"ExchangeRates:{fiatCurrency}:{cryptoCurrency}", 1.0m);
        await Task.CompletedTask;
        return fiatAmount / rate;
    }
}

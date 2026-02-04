# CryptoPay - Non-Custodial Crypto Payment Gateway

A complete payment gateway solution for accepting USDT payments via TRC20 and ERC20 networks, with support for WooCommerce and any web application via REST API.

## 🚀 Quick Start

**New to the plugin?** Start here:
- 📖 **[Complete Plugin Usage Guide](PLUGIN_USAGE_GUIDE.md)** - Step-by-step instructions for installing and using the WooCommerce plugin
- 🔧 **[Configuration Guide](CryptoPay.Api/CONFIGURATION.md)** - Backend API configuration details
- ☁️ **[Deploy to Google Cloud (100% Online)](CryptoPay.Api/SETUP_ONLINE_STEP_BY_STEP.md)** - Complete step-by-step guide using only web browser
- 📚 **[Full GCP Deployment Guide](CryptoPay.Api/DEPLOY_GCP.md)** - Comprehensive Google Cloud deployment instructions
- 🚀 **[Quick Deploy](CryptoPay.Api/QUICK_DEPLOY.md)** - Quick deployment guide (requires local tools)

## Architecture

- **Backend API**: ASP.NET Core .NET 8 Minimal API with SQL Server
- **Blockchain Monitoring**: Background service polling every 20 seconds
- **WooCommerce Plugin**: PHP plugin for WordPress/WooCommerce integration
- **Authentication**: API Key per merchant + HMAC signed webhooks

## Project Structure

```
Crypto-New-Project/
├── CryptoPay.Api/              # Backend API (.NET 8)
│   ├── Models/                 # Entity models
│   ├── Data/                   # EF Core DbContext
│   ├── DTOs/                   # Request/Response DTOs
│   ├── Services/               # Business logic services
│   ├── Workers/                # Background services
│   ├── Middleware/             # Authentication middleware
│   └── Migrations/             # Database migrations
├── cryptopay-woocommerce/      # WooCommerce plugin
│   ├── includes/               # PHP classes
│   ├── templates/              # Payment page template
│   └── assets/                 # JS/CSS files
├── docs/                       # Documentation
│   └── postman-collection.json # API collection
└── examples/                   # Example implementations
    └── web-checkout.html       # Standalone HTML example
```

## Prerequisites

### Backend API
- .NET 8 SDK
- SQL Server 2019+ (or SQL Server Express)
- Visual Studio 2022 / VS Code / Rider

### WooCommerce Plugin
- WordPress 5.8+
- WooCommerce 5.0+
- PHP 7.4+

## Setup Instructions

### 1. Backend API Setup

#### Step 1: Configure Database

1. Update `appsettings.json` with your SQL Server connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CryptoPayDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

2. For production, use environment variables:
```bash
export ConnectionStrings__DefaultConnection="Server=your-server;Database=CryptoPayDb;User Id=user;Password=pass;"
```

#### Step 2: Run Migrations

The database will be automatically migrated on first run. Alternatively, you can run:

```bash
cd CryptoPay.Api
dotnet ef database update
```

#### Step 3: Seed Test Addresses

Use the admin endpoint to seed wallet addresses (⚠️ **Secure this endpoint in production!**):

```bash
curl -X POST http://localhost:5000/admin/seed-addresses \
  -H "Content-Type: application/json" \
  -d '{
    "network": "TRC20",
    "count": 10,
    "addresses": [
      "TYourTronAddress1",
      "TYourTronAddress2",
      ...
    ]
  }'
```

Repeat for ERC20 addresses.

#### Step 4: Create a Test Merchant

You'll need to manually insert a merchant into the database:

```sql
INSERT INTO Merchants (Id, Name, ApiKey, WebhookUrl, WebhookSecret, IsActive, CreatedAt)
VALUES (
  NEWID(),
  'Test Merchant',
  'your-api-key-here',
  'https://your-wordpress-site.com/wp-json/cryptopay/v1/webhook',
  'your-webhook-secret-here',
  1,
  GETUTCDATE()
);
```

Generate secure API keys and webhook secrets using:
```bash
# Generate API Key
openssl rand -hex 32

# Generate Webhook Secret
openssl rand -hex 32
```

#### Step 5: Run the API

```bash
cd CryptoPay.Api
dotnet run
```

The API will be available at `http://localhost:5000` (or the port configured in `launchSettings.json`).

### 2. WooCommerce Plugin Setup

#### Step 1: Install Plugin

1. Copy the `cryptopay-woocommerce` folder to your WordPress `wp-content/plugins/` directory
2. Activate the plugin from WordPress admin → Plugins

#### Step 2: Configure Plugin

1. Go to WooCommerce → Settings → Payments
2. Click on "CryptoPay (USDT)" to configure
3. Fill in the settings:
   - **Enable/Disable**: Enable
   - **Backend API Base URL**: `http://localhost:5000` (or your API URL)
   - **Merchant ID**: The merchant ID from your database
   - **API Key**: The API key you generated
   - **Webhook Secret**: The webhook secret you generated
   - **Default Fiat Currency**: USD (or your preferred currency)
   - **Enable TRC20**: Yes
   - **Enable ERC20**: Yes

#### Step 3: Test Checkout

1. Create a test product in WooCommerce
2. Add to cart and proceed to checkout
3. Select "CryptoPay (USDT)" as payment method
4. Complete the order - you'll be redirected to the payment page

### 3. Blockchain Provider Implementation

The blockchain providers (`TronProvider` and `EvmProvider`) are currently stubs. You need to implement:

#### TronProvider (TRC20)
- Integrate with TronGrid API: `https://api.trongrid.io`
- Query USDT TRC20 contract: `TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t`
- Implement `GetRecentIncomingTxAsync` to fetch transactions
- Implement `GetConfirmationsAsync` to check confirmation count

#### EvmProvider (ERC20)
- Integrate with Ethereum RPC (Infura, Alchemy, or your own node)
- Query USDT ERC20 contract: `0xdAC17F958D2ee523a2206206994597C13D831ec7` (Ethereum mainnet)
- Use `eth_getLogs` to filter Transfer events
- Calculate confirmations from block numbers

See the TODO comments in the provider files for implementation details.

## API Endpoints

### POST /v1/intents
Create a new payment intent.

**Headers:**
```
X-API-Key: your-api-key
Content-Type: application/json
```

**Request Body:**
```json
{
  "merchantId": "guid",
  "orderRef": "order-123",
  "fiatCurrency": "USD",
  "fiatAmount": 100.00,
  "cryptoCurrency": "USDT",
  "network": "TRC20",
  "customerEmail": "customer@example.com",
  "returnUrl": "https://example.com/return"
}
```

**Response:**
```json
{
  "intentId": "guid",
  "status": "PENDING",
  "payAddress": "TYourTronAddress",
  "cryptoAmount": 100.00,
  "qrString": "base64-encoded-qr-code",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### GET /v1/intents/{intentId}
Get payment intent status.

**Headers:**
```
X-API-Key: your-api-key
```

**Response:**
```json
{
  "intentId": "guid",
  "status": "PAID",
  "payAddress": "TYourTronAddress",
  "cryptoAmount": 100.00,
  "txHash": "transaction-hash",
  "confirmations": 6,
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### POST /wp-json/cryptopay/v1/webhook
WooCommerce webhook endpoint (handled by plugin).

## Webhook Payload

When a payment is confirmed, the backend sends a webhook to the merchant's configured URL:

**Headers:**
```
X-CryptoPay-Signature: hmac-sha256-signature
X-CryptoPay-Event: payment.paid
Content-Type: application/json
```

**Body:**
```json
{
  "eventType": "payment.paid",
  "intentId": "guid",
  "orderRef": "order-123",
  "status": "PAID",
  "cryptoAmount": 100.00,
  "cryptoCurrency": "USDT",
  "network": "TRC20",
  "txHash": "transaction-hash",
  "confirmations": 6,
  "paidAt": "2024-01-01T12:00:00Z"
}
```

**Signature Verification:**
```php
$signature = hash_hmac('sha256', $payload, $webhook_secret);
```

## Standalone Web Integration

See `examples/web-checkout.html` for a complete example of integrating CryptoPay into any web application.

## Production Hardening Checklist

### Security
- [ ] Secure admin endpoints with authentication
- [ ] Use HTTPS for all API endpoints
- [ ] Rotate API keys and webhook secrets regularly
- [ ] Implement rate limiting
- [ ] Add request validation and sanitization
- [ ] Use environment variables for sensitive configuration
- [ ] Enable SQL injection protection (EF Core handles this)
- [ ] Implement CORS restrictions (currently allows all origins)

### Infrastructure
- [ ] Set up proper logging (Serilog, Application Insights, etc.)
- [ ] Configure health checks and monitoring
- [ ] Set up database backups
- [ ] Use connection pooling
- [ ] Implement retry policies for external API calls
- [ ] Set up alerting for failed webhooks

### Blockchain
- [ ] Implement real blockchain provider integrations
- [ ] Add support for multiple RPC endpoints (fallback)
- [ ] Implement transaction replay protection
- [ ] Add support for HD wallet derivation (replace address pool)
- [ ] Monitor for double-spending attempts
- [ ] Implement proper exchange rate fetching

### Address Management
- [ ] Replace address pool with HD wallet (BIP32/BIP44)
- [ ] Implement address reuse prevention
- [ ] Add address monitoring for all networks
- [ ] Implement address rotation

### Testing
- [ ] Add unit tests for services
- [ ] Add integration tests for API endpoints
- [ ] Test webhook delivery and retry logic
- [ ] Test idempotency guarantees
- [ ] Load testing for high transaction volumes

## Development

### Running Locally

1. Start SQL Server
2. Update connection string in `appsettings.json`
3. Run the API: `dotnet run --project CryptoPay.Api`
4. Seed test addresses via admin endpoint
5. Create test merchant in database
6. Configure WooCommerce plugin with test credentials

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName --project CryptoPay.Api
```

Apply migrations:
```bash
dotnet ef database update --project CryptoPay.Api
```

## Troubleshooting

### API returns 401 Unauthorized
- Check that `X-API-Key` header is set correctly
- Verify merchant exists and `IsActive` is true
- Check API key matches in database

### Webhooks not received
- Verify webhook URL is accessible from backend
- Check webhook secret matches
- Review backend logs for webhook delivery errors
- Ensure WordPress REST API is enabled

### Payment not detected
- Verify blockchain provider is implemented
- Check that addresses are seeded in database
- Review background worker logs
- Verify transaction amount matches exactly (within tolerance)

## License

[Your License Here]

## Support

For issues and questions, please open an issue in the repository.

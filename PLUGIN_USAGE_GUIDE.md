# CryptoPay WooCommerce Plugin - Complete Usage Guide

This guide will walk you through installing, configuring, and using the CryptoPay plugin step by step.

## Prerequisites

Before you begin, make sure you have:
- ✅ WordPress 5.8 or higher installed
- ✅ WooCommerce 5.0 or higher installed and activated
- ✅ PHP 7.4 or higher
- ✅ CryptoPay Backend API running and accessible
- ✅ A merchant account created in the backend database

## Step 1: Install the Plugin

### Option A: Manual Installation (Recommended for Development)

1. **Locate your WordPress plugins directory:**
   - Usually located at: `/wp-content/plugins/`
   - You can access this via FTP, cPanel File Manager, or SSH

2. **Upload the plugin folder:**
   - Copy the entire `cryptopay-woocommerce` folder
   - Paste it into your WordPress `wp-content/plugins/` directory
   - The final path should be: `/wp-content/plugins/cryptopay-woocommerce/`

3. **Verify the files are in place:**
   ```
   wp-content/plugins/cryptopay-woocommerce/
   ├── cryptopay-woocommerce.php (main plugin file)
   ├── includes/
   ├── templates/
   └── assets/
   ```

### Option B: ZIP Installation (For Production)

1. **Create a ZIP file:**
   - Compress the `cryptopay-woocommerce` folder into a ZIP file
   - Name it: `cryptopay-woocommerce.zip`

2. **Install via WordPress Admin:**
   - Go to **WordPress Admin → Plugins → Add New**
   - Click **Upload Plugin**
   - Choose the ZIP file and click **Install Now**
   - Click **Activate Plugin**

## Step 2: Activate the Plugin

1. Go to **WordPress Admin Dashboard**
2. Navigate to **Plugins → Installed Plugins**
3. Find **"CryptoPay (USDT)"** in the list
4. Click **Activate**

✅ You should see a success message confirming activation.

## Step 3: Set Up the Backend API

Before configuring the plugin, you need to have your backend API running:

### 3.1 Start the Backend API

```bash
cd CryptoPay.Api
dotnet run
```

The API should be running at `http://localhost:5000` (or your configured port).

### 3.2 Create a Merchant Account

You need to create a merchant in the database. Use the SQL script provided:

1. **Open SQL Server Management Studio** (or your SQL client)
2. **Connect to your database**
3. **Run the script** from `CryptoPay.Api/Scripts/seed-test-merchant.sql`

Or use this SQL (replace the values):

```sql
DECLARE @MerchantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(64) = 'your-generated-api-key-here';
DECLARE @WebhookSecret NVARCHAR(64) = 'your-generated-webhook-secret-here';
DECLARE @WebhookUrl NVARCHAR(500) = 'https://your-wordpress-site.com/wp-json/cryptopay/v1/webhook';

INSERT INTO Merchants (Id, Name, ApiKey, WebhookUrl, WebhookSecret, IsActive, CreatedAt)
VALUES (
    @MerchantId,
    'My Store',
    @ApiKey,
    @WebhookUrl,
    @WebhookSecret,
    1,
    GETUTCDATE()
);

-- Save these values - you'll need them!
SELECT Id AS MerchantId, ApiKey, WebhookSecret FROM Merchants WHERE Id = @MerchantId;
```

**Generate secure keys:**
```bash
# Generate API Key (32 bytes)
openssl rand -hex 32

# Generate Webhook Secret (32 bytes)
openssl rand -hex 32
```

### 3.3 Seed Wallet Addresses

You need to add wallet addresses to the database. Use the admin endpoint:

```bash
curl -X POST http://localhost:5000/admin/seed-addresses \
  -H "Content-Type: application/json" \
  -d '{
    "network": "TRC20",
    "count": 3,
    "addresses": [
      "TYourTronAddress1",
      "TYourTronAddress2",
      "TYourTronAddress3"
    ]
  }'
```

Repeat for ERC20 if needed:
```bash
curl -X POST http://localhost:5000/admin/seed-addresses \
  -H "Content-Type: application/json" \
  -d '{
    "network": "ERC20",
    "count": 3,
    "addresses": [
      "0xYourEthereumAddress1",
      "0xYourEthereumAddress2",
      "0xYourEthereumAddress3"
    ]
  }'
```

⚠️ **Important:** Replace with your actual wallet addresses. These addresses should be monitored by your backend.

## Step 4: Configure the Plugin

1. **Go to WooCommerce Settings:**
   - Navigate to **WooCommerce → Settings → Payments**

2. **Find CryptoPay (USDT):**
   - Scroll down to find **"CryptoPay (USDT)"** in the payment methods list
   - Click on it to expand the settings

3. **Fill in the configuration:**

   | Setting | Description | Example |
   |---------|-------------|---------|
   | **Enable/Disable** | Check this box to enable the payment gateway | ☑️ Checked |
   | **Title** | Payment method name shown to customers | "Pay with USDT" |
   | **Description** | Text shown during checkout | "Pay with USDT via TRC20 or ERC20" |
   | **Backend API Base URL** | Your CryptoPay API URL | `http://localhost:5000` or `https://api.yoursite.com` |
   | **Merchant ID** | The Merchant ID from your database | `123e4567-e89b-12d3-a456-426614174000` |
   | **API Key** | The API key you generated | `your-api-key-here` |
   | **Webhook Secret** | The webhook secret you generated | `your-webhook-secret-here` |
   | **Default Fiat Currency** | Default currency for orders | USD, EUR, or GBP |
   | **Enable TRC20** | Allow TRC20 payments | ☑️ Checked |
   | **Enable ERC20** | Allow ERC20 payments | ☑️ Checked |

4. **Save Changes:**
   - Click **Save changes** at the bottom

✅ Your plugin is now configured!

## Step 5: Test the Plugin

### 5.1 Create a Test Product

1. Go to **Products → Add New**
2. Create a simple product:
   - **Name:** Test Product
   - **Price:** $10.00
   - **Publish** the product

### 5.2 Test Checkout Flow

1. **Add product to cart:**
   - Visit your shop page
   - Add the test product to cart
   - Go to checkout

2. **Select payment method:**
   - You should see **"CryptoPay (USDT)"** in the payment options
   - Select it

3. **Place order:**
   - Fill in billing details
   - Click **Place Order**

4. **Payment page:**
   - You'll be redirected to the payment page showing:
     - **Amount to pay** in USDT
     - **Payment address** (with copy button)
     - **QR code** for easy scanning
     - **Countdown timer** showing time remaining
     - **Status message** showing "Waiting for payment..."

5. **Send payment:**
   - Copy the address or scan the QR code
   - Send the exact USDT amount to the address
   - The page will automatically poll for payment status

6. **Payment confirmation:**
   - Once payment is detected (after confirmations), you'll see:
     - Status changes to "Payment confirmed! Redirecting..."
     - Automatic redirect to order received page
   - The order status in WooCommerce will update to "Processing" or "Completed"

## Step 6: Verify Webhook is Working

1. **Check webhook URL is accessible:**
   - Visit: `https://your-wordpress-site.com/wp-json/cryptopay/v1/webhook`
   - You should see a response (even if it's an error, it means the endpoint is accessible)

2. **Check order status:**
   - After payment is confirmed, check the order in **WooCommerce → Orders**
   - The order should automatically update to "Processing" or "Completed"
   - Check order notes for transaction hash and confirmations

3. **Debug webhook issues:**
   - Check WordPress error logs: `wp-content/debug.log`
   - Verify webhook secret matches in both plugin and database
   - Ensure backend can reach your WordPress site

## Troubleshooting

### ❌ Payment gateway not showing in checkout

**Possible causes:**
- Plugin not activated
- Gateway not enabled in settings
- WooCommerce not installed/activated

**Solutions:**
1. Go to **Plugins** and verify CryptoPay is activated
2. Go to **WooCommerce → Settings → Payments** and enable the gateway
3. Verify WooCommerce is installed and active

### ❌ "Failed to create payment intent" error

**Possible causes:**
- API URL incorrect
- API key invalid
- Merchant ID incorrect
- Backend API not running

**Solutions:**
1. Verify backend API is running: `curl http://localhost:5000/health`
2. Check API Base URL in plugin settings
3. Verify API key and Merchant ID match database
4. Check browser console for detailed error messages

### ❌ Payment page shows "Failed to load payment information"

**Possible causes:**
- API connection issue
- Invalid intent ID
- CORS issues

**Solutions:**
1. Check API is accessible from your WordPress server
2. Verify intent was created (check backend logs)
3. Check browser console for errors
4. Ensure CORS is configured in backend (currently allows all origins)

### ❌ Webhook not updating order status

**Possible causes:**
- Webhook URL not accessible
- Webhook secret mismatch
- WordPress REST API disabled

**Solutions:**
1. Test webhook URL accessibility: `curl https://your-site.com/wp-json/cryptopay/v1/webhook`
2. Verify webhook secret matches in plugin settings and database
3. Check WordPress REST API is enabled (should be by default)
4. Review WordPress error logs
5. Check backend logs for webhook delivery errors

### ❌ Payment not detected

**Possible causes:**
- Blockchain provider not implemented (stub)
- Wrong address used
- Amount mismatch
- Backend worker not running

**Solutions:**
1. **Important:** The blockchain providers are currently stubs - you need to implement them!
2. Verify you sent to the correct address shown on payment page
3. Ensure amount matches exactly (within tolerance)
4. Check backend worker is running (should log every 20 seconds)
5. Review backend logs for blockchain polling errors

## Production Checklist

Before going live:

- [ ] Backend API is deployed and accessible via HTTPS
- [ ] Database is properly secured
- [ ] API keys and webhook secrets are strong and unique
- [ ] Admin endpoints are secured (currently unprotected!)
- [ ] Blockchain providers are fully implemented
- [ ] Real wallet addresses are seeded (not test addresses)
- [ ] Exchange rate API is integrated
- [ ] Webhook URL uses HTTPS
- [ ] CORS is restricted to your domain
- [ ] Logging and monitoring are set up
- [ ] Tested with real transactions on testnet first

## Support

If you encounter issues:

1. Check the main **README.md** for detailed documentation
2. Review **PROJECT_SUMMARY.md** for architecture overview
3. Check WordPress and backend error logs
4. Verify all prerequisites are met
5. Test with the standalone HTML example (`examples/web-checkout.html`) to isolate plugin issues

## Next Steps

Once everything is working:

1. **Implement blockchain providers** - Complete the `TronProvider` and `EvmProvider` implementations
2. **Add more networks** - Extend to support Bitcoin or other cryptocurrencies
3. **Replace address pool** - Implement HD wallet derivation for better address management
4. **Add exchange rate API** - Integrate real-time rates from CoinGecko or Binance
5. **Enhance UI** - Customize the payment page to match your brand

---

**Need help?** Check the main README.md or review the code comments for implementation details.

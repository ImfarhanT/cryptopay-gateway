# CryptoPay WooCommerce Plugin

WordPress/WooCommerce plugin for accepting USDT payments via CryptoPay gateway.

## Installation

1. Copy the `cryptopay-woocommerce` folder to your WordPress `wp-content/plugins/` directory
2. Activate the plugin from WordPress Admin → Plugins
3. Configure the plugin settings (see Configuration below)

## Configuration

1. Go to **WooCommerce → Settings → Payments**
2. Click on **CryptoPay (USDT)** to configure
3. Fill in the required settings:
   - **Enable/Disable**: Enable the payment gateway
   - **Backend API Base URL**: Your CryptoPay API URL (e.g., `https://api.cryptopay.example.com`)
   - **Merchant ID**: Your merchant ID from CryptoPay
   - **API Key**: Your API key from CryptoPay
   - **Webhook Secret**: Your webhook secret for verifying callbacks
   - **Default Fiat Currency**: Default currency for orders (USD, EUR, GBP)
   - **Enable TRC20**: Enable USDT TRC20 payments
   - **Enable ERC20**: Enable USDT ERC20 payments

## How It Works

1. Customer selects "CryptoPay (USDT)" as payment method during checkout
2. Plugin creates a payment intent via the CryptoPay API
3. Customer is redirected to a payment page showing:
   - Payment address (QR code and text)
   - Amount to pay in USDT
   - Countdown timer
4. Customer sends USDT to the displayed address
5. Backend monitors blockchain and detects payment
6. Webhook is sent to WordPress when payment is confirmed
7. Order status is automatically updated to "Processing" or "Completed"

## Webhook Endpoint

The plugin registers a webhook endpoint at:
```
/wp-json/cryptopay/v1/webhook
```

This endpoint:
- Verifies HMAC signature from backend
- Finds the order by intent ID
- Updates order status when payment is confirmed

## Requirements

- WordPress 5.8+
- WooCommerce 5.0+
- PHP 7.4+
- CryptoPay Backend API running and accessible

## Troubleshooting

### Payment gateway not showing in checkout
- Ensure the plugin is activated
- Check that the gateway is enabled in WooCommerce settings
- Verify WooCommerce is installed and active

### Webhooks not working
- Verify webhook URL is accessible from your backend
- Check that webhook secret matches in both plugin and backend
- Review WordPress REST API is enabled (should be by default)
- Check WordPress error logs for webhook processing errors

### Payment page not loading
- Verify API Base URL is correct
- Check API key and merchant ID are valid
- Review browser console for JavaScript errors
- Ensure jQuery is loaded (required for payment page polling)

## Support

For issues, please check the main project README or open an issue in the repository.

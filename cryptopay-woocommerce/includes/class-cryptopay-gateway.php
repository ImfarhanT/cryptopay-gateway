<?php
if (!defined('ABSPATH')) {
    exit;
}

class CryptoPay_Gateway extends WC_Payment_Gateway
{
    public function __construct()
    {
        $this->id = 'cryptopay';
        $this->icon = '';
        $this->has_fields = true;
        $this->method_title = 'CryptoPay (USDT)';
        $this->method_description = 'Accept USDT payments via TRC20 and ERC20 networks';

        $this->init_form_fields();
        $this->init_settings();

        $this->title = $this->get_option('title', 'CryptoPay (USDT)');
        $this->description = $this->get_option('description', 'Pay with USDT');
        $this->enabled = $this->get_option('enabled', 'no');
        $this->api_base_url = $this->get_option('api_base_url', '');
        $this->merchant_id = $this->get_option('merchant_id', '');
        $this->api_key = $this->get_option('api_key', '');
        $this->webhook_secret = $this->get_option('webhook_secret', '');
        $this->default_fiat_currency = $this->get_option('default_fiat_currency', 'USD');
        $this->enable_trc20 = $this->get_option('enable_trc20', 'yes');
        $this->enable_erc20 = $this->get_option('enable_erc20', 'yes');

        add_action('woocommerce_update_options_payment_gateways_' . $this->id, array($this, 'process_admin_options'));
        add_action('woocommerce_receipt_' . $this->id, array($this, 'receipt_page'));
        add_action('woocommerce_thankyou_' . $this->id, array($this, 'thankyou_page'));
    }

    public function payment_fields()
    {
        if ($this->description) {
            echo '<p>' . esc_html($this->description) . '</p>';
        }
        
        $show_trc20 = $this->enable_trc20 === 'yes';
        $show_erc20 = $this->enable_erc20 === 'yes';
        
        if ($show_trc20 && $show_erc20) {
            echo '<div class="cryptopay-network-selection" style="margin: 15px 0;">';
            echo '<p style="margin-bottom: 10px; font-weight: 600;">Select Network:</p>';
            echo '<label style="display: block; margin: 8px 0; cursor: pointer;">';
            echo '<input type="radio" name="cryptopay_network" value="TRC20" checked style="margin-right: 8px;">';
            echo 'USDT TRC20 (TRON) - <span style="color: #27ae60;">Lower fees</span>';
            echo '</label>';
            echo '<label style="display: block; margin: 8px 0; cursor: pointer;">';
            echo '<input type="radio" name="cryptopay_network" value="ERC20" style="margin-right: 8px;">';
            echo 'USDT ERC20 (Ethereum)';
            echo '</label>';
            echo '</div>';
        } elseif ($show_trc20) {
            echo '<input type="hidden" name="cryptopay_network" value="TRC20">';
            echo '<p style="color: #666;">Network: USDT TRC20 (TRON)</p>';
        } elseif ($show_erc20) {
            echo '<input type="hidden" name="cryptopay_network" value="ERC20">';
            echo '<p style="color: #666;">Network: USDT ERC20 (Ethereum)</p>';
        }
    }

    public function init_form_fields()
    {
        $this->form_fields = array(
            'enabled' => array(
                'title' => 'Enable/Disable',
                'label' => 'Enable CryptoPay',
                'type' => 'checkbox',
                'description' => '',
                'default' => 'no',
            ),
            'title' => array(
                'title' => 'Title',
                'type' => 'text',
                'description' => 'This controls the title which the user sees during checkout.',
                'default' => 'CryptoPay (USDT)',
                'desc_tip' => true,
            ),
            'description' => array(
                'title' => 'Description',
                'type' => 'textarea',
                'description' => 'This controls the description which the user sees during checkout.',
                'default' => 'Pay with USDT via TRC20 or ERC20 network',
            ),
            'api_base_url' => array(
                'title' => 'Backend API Base URL',
                'type' => 'text',
                'description' => 'The base URL of the CryptoPay API (e.g., https://api.cryptopay.example.com)',
                'default' => '',
                'required' => true,
            ),
            'merchant_id' => array(
                'title' => 'Merchant ID',
                'type' => 'text',
                'description' => 'Your merchant ID from CryptoPay',
                'default' => '',
                'required' => true,
            ),
            'api_key' => array(
                'title' => 'API Key',
                'type' => 'password',
                'description' => 'Your API key from CryptoPay',
                'default' => '',
                'required' => true,
            ),
            'webhook_secret' => array(
                'title' => 'Webhook Secret',
                'type' => 'password',
                'description' => 'Secret key for verifying webhook signatures',
                'default' => '',
                'required' => true,
            ),
            'default_fiat_currency' => array(
                'title' => 'Default Fiat Currency',
                'type' => 'select',
                'options' => array(
                    'USD' => 'USD',
                    'EUR' => 'EUR',
                    'GBP' => 'GBP',
                ),
                'default' => 'USD',
            ),
            'enable_trc20' => array(
                'title' => 'Enable TRC20',
                'type' => 'checkbox',
                'label' => 'Enable USDT TRC20 payments',
                'default' => 'yes',
            ),
            'enable_erc20' => array(
                'title' => 'Enable ERC20',
                'type' => 'checkbox',
                'label' => 'Enable USDT ERC20 payments',
                'default' => 'yes',
            ),
        );
    }

    public function process_payment($order_id)
    {
        $order = wc_get_order($order_id);

        if (!$order) {
            return array(
                'result' => 'fail',
                'redirect' => '',
            );
        }

        // Get selected network from form
        $network = isset($_POST['cryptopay_network']) ? sanitize_text_field($_POST['cryptopay_network']) : 'TRC20';
        
        // Validate network selection
        if ($network === 'TRC20' && $this->enable_trc20 !== 'yes') {
            $network = 'ERC20';
        }
        if ($network === 'ERC20' && $this->enable_erc20 !== 'yes') {
            $network = 'TRC20';
        }

        // Create payment intent
        $response = $this->create_payment_intent($order, $network);

        if (is_wp_error($response)) {
            wc_add_notice('Payment error: ' . $response->get_error_message(), 'error');
            return array(
                'result' => 'fail',
                'redirect' => '',
            );
        }

        $response_code = wp_remote_retrieve_response_code($response);
        $response_body = wp_remote_retrieve_body($response);
        $intent_data = json_decode($response_body, true);

        // Check for HTTP errors
        if ($response_code !== 200) {
            $error_message = isset($intent_data['detail']) ? $intent_data['detail'] : 
                            (isset($intent_data['title']) ? $intent_data['title'] : 'API Error: ' . $response_code);
            wc_add_notice('Payment error: ' . $error_message, 'error');
            error_log('CryptoPay API Error: ' . $response_body);
            return array(
                'result' => 'fail',
                'redirect' => '',
            );
        }

        if (!isset($intent_data['intentId'])) {
            wc_add_notice('Failed to create payment intent: Invalid response from server', 'error');
            error_log('CryptoPay Invalid Response: ' . $response_body);
            return array(
                'result' => 'fail',
                'redirect' => '',
            );
        }

        // Store all payment data in order meta
        $order->update_meta_data('_cryptopay_intent_id', $intent_data['intentId']);
        $order->update_meta_data('_cryptopay_network', $network);
        $order->update_meta_data('_cryptopay_pay_address', $intent_data['payAddress']);
        $order->update_meta_data('_cryptopay_crypto_amount', $intent_data['cryptoAmount']);
        $order->update_meta_data('_cryptopay_qr_string', $intent_data['qrString'] ?? '');
        $order->update_meta_data('_cryptopay_expires_at', $intent_data['expiresAt']);
        
        // Keep order as pending-payment so receipt page works
        $order->update_status('pending', __('Awaiting crypto payment', 'cryptopay-woocommerce'));
        $order->save();

        // Reduce stock levels
        wc_reduce_stock_levels($order_id);
        
        // Empty the cart
        WC()->cart->empty_cart();

        // Redirect to order-pay page which triggers receipt_page
        $payment_url = $order->get_checkout_payment_url(true);

        return array(
            'result' => 'success',
            'redirect' => $payment_url,
        );
    }

    private function create_payment_intent($order, $network)
    {
        $api_url = trailingslashit($this->api_base_url) . 'v1/intents';

        $body = array(
            'merchantId' => $this->merchant_id,
            'orderRef' => (string) $order->get_id(),
            'fiatCurrency' => $this->default_fiat_currency,
            'fiatAmount' => (float) $order->get_total(),
            'cryptoCurrency' => 'USDT',
            'network' => $network,
            'customerEmail' => $order->get_billing_email(),
            'returnUrl' => $this->get_return_url($order),
        );

        $args = array(
            'method' => 'POST',
            'headers' => array(
                'Content-Type' => 'application/json',
                'X-API-Key' => $this->api_key,
            ),
            'body' => json_encode($body),
            'timeout' => 30,
        );

        return wp_remote_post($api_url, $args);
    }

    public function receipt_page($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order) {
            echo '<p>Order not found.</p>';
            return;
        }

        $intent_id = $order->get_meta('_cryptopay_intent_id');
        $pay_address = $order->get_meta('_cryptopay_pay_address');
        $crypto_amount = $order->get_meta('_cryptopay_crypto_amount');
        $network = $order->get_meta('_cryptopay_network');
        $qr_string = $order->get_meta('_cryptopay_qr_string');
        $expires_at = $order->get_meta('_cryptopay_expires_at');

        if (empty($intent_id) || empty($pay_address)) {
            echo '<p>Payment information not available. Please contact support.</p>';
            return;
        }
        ?>
        <style>
            .cryptopay-payment { max-width: 500px; margin: 20px auto; padding: 30px; text-align: center; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8f9fa; border-radius: 12px; }
            .cryptopay-payment h2 { margin: 0 0 25px; color: #1a1a2e; font-size: 24px; }
            .cryptopay-amount-box { background: #fff; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
            .cryptopay-amount-label { font-size: 14px; color: #666; margin-bottom: 5px; }
            .cryptopay-amount { font-size: 32px; font-weight: 700; color: #27ae60; }
            .cryptopay-address-box { background: #fff; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
            .cryptopay-address { font-family: 'Courier New', monospace; font-size: 13px; word-break: break-all; background: #f1f3f4; padding: 12px; border-radius: 6px; margin: 10px 0; border: 1px solid #e0e0e0; }
            .cryptopay-copy-btn { padding: 12px 24px; background: #4CAF50; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 14px; font-weight: 600; transition: background 0.2s; }
            .cryptopay-copy-btn:hover { background: #45a049; }
            .cryptopay-qr { margin: 20px 0; }
            .cryptopay-qr img { max-width: 200px; border: 1px solid #e0e0e0; padding: 10px; background: white; border-radius: 8px; }
            .cryptopay-timer { background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; }
            .cryptopay-timer-label { font-size: 14px; color: #856404; }
            .cryptopay-countdown { font-size: 28px; font-weight: 700; color: #856404; }
            .cryptopay-status { padding: 15px; border-radius: 8px; margin-top: 20px; background: #e3f2fd; color: #1565c0; font-weight: 500; }
            .cryptopay-status.paid { background: #d4edda; color: #155724; }
            .cryptopay-status.expired { background: #f8d7da; color: #721c24; }
            .cryptopay-network-badge { display: inline-block; background: #e8f5e9; color: #2e7d32; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; margin-bottom: 15px; }
        </style>

        <div class="cryptopay-payment">
            <h2>💰 Pay with USDT</h2>
            <span class="cryptopay-network-badge"><?php echo esc_html($network); ?> Network</span>
            
            <div class="cryptopay-amount-box">
                <div class="cryptopay-amount-label">Amount to send:</div>
                <div class="cryptopay-amount"><?php echo esc_html(number_format((float)$crypto_amount, 6)); ?> USDT</div>
            </div>
            
            <div class="cryptopay-address-box">
                <div class="cryptopay-amount-label">Send to this address:</div>
                <div class="cryptopay-address" id="pay-address"><?php echo esc_html($pay_address); ?></div>
                <button class="cryptopay-copy-btn" onclick="cryptopayCopyAddress()">📋 Copy Address</button>
            </div>
            
            <?php if ($qr_string): ?>
            <div class="cryptopay-qr">
                <img src="data:image/png;base64,<?php echo esc_attr($qr_string); ?>" alt="QR Code">
            </div>
            <?php endif; ?>
            
            <div class="cryptopay-timer">
                <div class="cryptopay-timer-label">⏳ Time remaining:</div>
                <div class="cryptopay-countdown" id="cryptopay-countdown">--:--</div>
            </div>
            
            <div class="cryptopay-status" id="cryptopay-status">
                Waiting for payment...
            </div>
        </div>

        <script>
        (function() {
            var intentId = '<?php echo esc_js($intent_id); ?>';
            var apiUrl = '<?php echo esc_js(rtrim($this->api_base_url, "/")); ?>';
            var apiKey = '<?php echo esc_js($this->api_key); ?>';
            var returnUrl = '<?php echo esc_js($this->get_return_url($order)); ?>';
            var expiresAt = new Date('<?php echo esc_js($expires_at); ?>');
            
            window.cryptopayCopyAddress = function() {
                var address = document.getElementById('pay-address').textContent;
                navigator.clipboard.writeText(address).then(function() {
                    var btn = event.target;
                    btn.textContent = '✅ Copied!';
                    setTimeout(function() { btn.textContent = '📋 Copy Address'; }, 2000);
                });
            };
            
            function updateCountdown() {
                var now = new Date();
                var diff = expiresAt - now;
                if (diff <= 0) {
                    document.getElementById('cryptopay-countdown').textContent = 'EXPIRED';
                    document.getElementById('cryptopay-status').textContent = 'Payment expired. Please create a new order.';
                    document.getElementById('cryptopay-status').className = 'cryptopay-status expired';
                    return;
                }
                var minutes = Math.floor(diff / 60000);
                var seconds = Math.floor((diff % 60000) / 1000);
                document.getElementById('cryptopay-countdown').textContent = 
                    String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0');
            }
            
            function checkStatus() {
                fetch(apiUrl + '/v1/intents/' + intentId, {
                    headers: { 'X-API-Key': apiKey }
                })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (data.status === 'PAID') {
                        document.getElementById('cryptopay-status').textContent = '✅ Payment confirmed! Redirecting...';
                        document.getElementById('cryptopay-status').className = 'cryptopay-status paid';
                        setTimeout(function() { window.location.href = returnUrl; }, 2000);
                    }
                })
                .catch(function(err) { console.log('Status check error:', err); });
            }
            
            setInterval(updateCountdown, 1000);
            setInterval(checkStatus, 5000);
            updateCountdown();
            setTimeout(checkStatus, 1000);
        })();
        </script>
        <?php
    }

    public function thankyou_page($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order) {
            return;
        }

        $intent_id = $order->get_meta('_cryptopay_intent_id');
        if (empty($intent_id)) {
            return;
        }

        echo '<p style="padding: 15px; background: #d4edda; color: #155724; border-radius: 8px;">✅ ' . esc_html__('Your crypto payment has been received! Your order is being processed.', 'cryptopay-woocommerce') . '</p>';
    }
}

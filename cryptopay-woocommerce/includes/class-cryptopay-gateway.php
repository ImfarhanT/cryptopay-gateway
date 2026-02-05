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
                'description' => 'The base URL of the CryptoPay API',
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
                'options' => array('USD' => 'USD', 'EUR' => 'EUR', 'GBP' => 'GBP'),
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
            return array('result' => 'fail', 'redirect' => '');
        }

        $network = isset($_POST['cryptopay_network']) ? sanitize_text_field($_POST['cryptopay_network']) : 'TRC20';
        
        if ($network === 'TRC20' && $this->enable_trc20 !== 'yes') $network = 'ERC20';
        if ($network === 'ERC20' && $this->enable_erc20 !== 'yes') $network = 'TRC20';

        $response = $this->create_payment_intent($order, $network);

        if (is_wp_error($response)) {
            wc_add_notice('Payment error: ' . $response->get_error_message(), 'error');
            return array('result' => 'fail', 'redirect' => '');
        }

        $response_code = wp_remote_retrieve_response_code($response);
        $response_body = wp_remote_retrieve_body($response);
        $intent_data = json_decode($response_body, true);

        if ($response_code !== 200) {
            $error_message = isset($intent_data['detail']) ? $intent_data['detail'] : 
                            (isset($intent_data['title']) ? $intent_data['title'] : 'API Error: ' . $response_code);
            wc_add_notice('Payment error: ' . $error_message, 'error');
            return array('result' => 'fail', 'redirect' => '');
        }

        if (!isset($intent_data['intentId'])) {
            wc_add_notice('Failed to create payment intent', 'error');
            return array('result' => 'fail', 'redirect' => '');
        }

        $order->update_meta_data('_cryptopay_intent_id', $intent_data['intentId']);
        $order->update_meta_data('_cryptopay_network', $network);
        $order->update_meta_data('_cryptopay_pay_address', $intent_data['payAddress']);
        $order->update_meta_data('_cryptopay_crypto_amount', $intent_data['cryptoAmount']);
        $order->update_meta_data('_cryptopay_qr_string', $intent_data['qrString'] ?? '');
        $order->update_meta_data('_cryptopay_expires_at', $intent_data['expiresAt']);
        $order->update_status('pending', __('Awaiting crypto payment', 'cryptopay-woocommerce'));
        $order->save();

        wc_reduce_stock_levels($order_id);
        WC()->cart->empty_cart();

        return array('result' => 'success', 'redirect' => $order->get_checkout_payment_url(true));
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
        return wp_remote_post($api_url, array(
            'method' => 'POST',
            'headers' => array('Content-Type' => 'application/json', 'X-API-Key' => $this->api_key),
            'body' => json_encode($body),
            'timeout' => 30,
        ));
    }

    public function receipt_page($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order) { echo '<p>Order not found.</p>'; return; }

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
            .cpay-wrap { max-width: 600px; margin: 0 auto; padding: 20px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
            .cpay-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 24px; margin-bottom: 16px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
            .cpay-header { text-align: center; padding-bottom: 20px; border-bottom: 1px solid #e5e7eb; margin-bottom: 20px; }
            .cpay-title { font-size: 24px; font-weight: 700; color: #1f2937; margin: 0 0 8px; }
            .cpay-badge { display: inline-block; background: #ecfdf5; color: #059669; padding: 6px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; }
            
            .cpay-amount-section { text-align: center; padding: 20px 0; }
            .cpay-amount-label { font-size: 14px; color: #6b7280; margin-bottom: 8px; }
            .cpay-amount { font-size: 42px; font-weight: 700; color: #059669; }
            .cpay-amount span { font-size: 20px; color: #6b7280; margin-left: 8px; }
            
            .cpay-alert { background: #fffbeb; border: 1px solid #fcd34d; border-radius: 8px; padding: 12px 16px; margin-bottom: 20px; display: flex; align-items: center; gap: 12px; }
            .cpay-alert-icon { font-size: 20px; }
            .cpay-alert-text { font-size: 13px; color: #92400e; line-height: 1.5; }
            
            .cpay-address-section { margin-bottom: 20px; }
            .cpay-label { font-size: 13px; font-weight: 600; color: #374151; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; }
            .cpay-address-box { background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; position: relative; }
            .cpay-address { font-family: 'SF Mono', 'Fira Code', 'Consolas', monospace; font-size: 14px; color: #1f2937; word-break: break-all; line-height: 1.6; margin: 0 0 12px; }
            .cpay-copy-btn { width: 100%; background: #059669; color: #fff; border: none; border-radius: 8px; padding: 12px 20px; font-size: 15px; font-weight: 600; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px; transition: all 0.2s; }
            .cpay-copy-btn:hover { background: #047857; }
            .cpay-copy-btn.copied { background: #10b981; }
            
            .cpay-qr-section { text-align: center; padding: 20px 0; }
            .cpay-qr-box { display: inline-block; background: #fff; border: 2px solid #e5e7eb; border-radius: 12px; padding: 16px; }
            .cpay-qr-box img { width: 180px; height: 180px; display: block; }
            .cpay-qr-hint { font-size: 13px; color: #6b7280; margin-top: 12px; }
            
            .cpay-timer-section { text-align: center; background: #fef3c7; border: 1px solid #fcd34d; border-radius: 8px; padding: 16px; margin-bottom: 20px; }
            .cpay-timer-label { font-size: 13px; color: #92400e; margin-bottom: 4px; }
            .cpay-timer { font-size: 36px; font-weight: 700; color: #b45309; font-variant-numeric: tabular-nums; }
            
            .cpay-status { text-align: center; border-radius: 8px; padding: 20px; }
            .cpay-status.waiting { background: #eff6ff; border: 1px solid #bfdbfe; }
            .cpay-status.paid { background: #ecfdf5; border: 1px solid #a7f3d0; }
            .cpay-status.expired { background: #fef2f2; border: 1px solid #fecaca; }
            .cpay-status-text { font-size: 16px; font-weight: 600; margin: 0; }
            .cpay-status.waiting .cpay-status-text { color: #1d4ed8; }
            .cpay-status.paid .cpay-status-text { color: #059669; }
            .cpay-status.expired .cpay-status-text { color: #dc2626; }
            .cpay-status-hint { font-size: 13px; color: #6b7280; margin-top: 8px; }
            
            .cpay-spinner { width: 24px; height: 24px; border: 3px solid #bfdbfe; border-top-color: #1d4ed8; border-radius: 50%; animation: cpay-spin 1s linear infinite; margin: 0 auto 12px; }
            @keyframes cpay-spin { to { transform: rotate(360deg); } }
            
            .cpay-steps { padding-top: 20px; border-top: 1px solid #e5e7eb; }
            .cpay-steps-title { font-size: 14px; font-weight: 600; color: #374151; margin-bottom: 16px; }
            .cpay-step { display: flex; align-items: flex-start; gap: 12px; margin-bottom: 12px; }
            .cpay-step-num { width: 28px; height: 28px; background: #f3f4f6; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 13px; font-weight: 600; color: #374151; flex-shrink: 0; }
            .cpay-step-text { font-size: 14px; color: #4b5563; line-height: 1.5; padding-top: 4px; }
            
            /* Desktop Layout */
            @media (min-width: 768px) {
                .cpay-wrap { max-width: 700px; padding: 40px 20px; }
                .cpay-card { padding: 32px; }
                .cpay-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; align-items: start; }
                .cpay-qr-section { padding: 0; }
                .cpay-title { font-size: 28px; }
                .cpay-amount { font-size: 48px; }
            }
        </style>

        <div class="cpay-wrap">
            <div class="cpay-card">
                <div class="cpay-header">
                    <h1 class="cpay-title">Complete Your Payment</h1>
                    <span class="cpay-badge">💎 <?php echo esc_html($network); ?> Network</span>
                </div>
                
                <div class="cpay-amount-section">
                    <div class="cpay-amount-label">Send exactly this amount</div>
                    <div class="cpay-amount"><?php echo esc_html(number_format((float)$crypto_amount, 2)); ?><span>USDT</span></div>
                </div>
                
                <div class="cpay-alert">
                    <span class="cpay-alert-icon">⚠️</span>
                    <span class="cpay-alert-text"><strong>Important:</strong> Send the exact amount shown. Sending a different amount may delay your order.</span>
                </div>
                
                <div class="cpay-grid">
                    <div>
                        <div class="cpay-address-section">
                            <div class="cpay-label">Send to this address</div>
                            <div class="cpay-address-box">
                                <p class="cpay-address" id="payAddress"><?php echo esc_html($pay_address); ?></p>
                                <button class="cpay-copy-btn" id="copyBtn" onclick="copyAddr()">
                                    <span id="copyIcon">📋</span>
                                    <span id="copyText">Copy Address</span>
                                </button>
                            </div>
                        </div>
                        
                        <div class="cpay-timer-section">
                            <div class="cpay-timer-label">⏱️ Time remaining</div>
                            <div class="cpay-timer" id="timer">--:--</div>
                        </div>
                    </div>
                    
                    <?php if ($qr_string): ?>
                    <div class="cpay-qr-section">
                        <div class="cpay-qr-box">
                            <img src="data:image/png;base64,<?php echo esc_attr($qr_string); ?>" alt="QR Code">
                        </div>
                        <p class="cpay-qr-hint">Scan with your wallet app</p>
                    </div>
                    <?php endif; ?>
                </div>
                
                <div class="cpay-status waiting" id="statusBox">
                    <div class="cpay-spinner" id="spinner"></div>
                    <p class="cpay-status-text" id="statusText">⏳ Waiting for your payment...</p>
                    <p class="cpay-status-hint" id="statusHint">We'll detect it automatically once sent</p>
                </div>
            </div>
            
            <div class="cpay-card">
                <div class="cpay-steps">
                    <div class="cpay-steps-title">📝 How to complete your payment</div>
                    <div class="cpay-step">
                        <span class="cpay-step-num">1</span>
                        <span class="cpay-step-text">Open your crypto wallet (TronLink, Trust Wallet, Binance, etc.)</span>
                    </div>
                    <div class="cpay-step">
                        <span class="cpay-step-num">2</span>
                        <span class="cpay-step-text">Send exactly <strong><?php echo esc_html(number_format((float)$crypto_amount, 2)); ?> USDT</strong> to the address above</span>
                    </div>
                    <div class="cpay-step">
                        <span class="cpay-step-num">3</span>
                        <span class="cpay-step-text">Wait 1-2 minutes for blockchain confirmation</span>
                    </div>
                    <div class="cpay-step">
                        <span class="cpay-step-num">4</span>
                        <span class="cpay-step-text">You'll be redirected automatically once payment is confirmed</span>
                    </div>
                </div>
            </div>
        </div>

        <script>
        (function() {
            var intentId = '<?php echo esc_js($intent_id); ?>';
            var apiUrl = '<?php echo esc_js(rtrim($this->api_base_url, "/")); ?>';
            var apiKey = '<?php echo esc_js($this->api_key); ?>';
            var returnUrl = '<?php echo esc_js($this->get_return_url($order)); ?>';
            var expiresAt = new Date('<?php echo esc_js($expires_at); ?>');
            
            window.copyAddr = function() {
                var addr = document.getElementById('payAddress').textContent;
                navigator.clipboard.writeText(addr).then(function() {
                    var btn = document.getElementById('copyBtn');
                    document.getElementById('copyIcon').textContent = '✓';
                    document.getElementById('copyText').textContent = 'Copied!';
                    btn.classList.add('copied');
                    setTimeout(function() {
                        document.getElementById('copyIcon').textContent = '📋';
                        document.getElementById('copyText').textContent = 'Copy Address';
                        btn.classList.remove('copied');
                    }, 2000);
                });
            };
            
            function updateTimer() {
                var now = new Date();
                var diff = expiresAt - now;
                if (diff <= 0) {
                    document.getElementById('timer').textContent = 'EXPIRED';
                    document.getElementById('statusBox').className = 'cpay-status expired';
                    document.getElementById('spinner').style.display = 'none';
                    document.getElementById('statusText').textContent = '❌ Payment time expired';
                    document.getElementById('statusHint').textContent = 'Please place a new order to try again';
                    return;
                }
                var m = Math.floor(diff / 60000);
                var s = Math.floor((diff % 60000) / 1000);
                document.getElementById('timer').textContent = String(m).padStart(2,'0') + ':' + String(s).padStart(2,'0');
            }
            
            function checkPayment() {
                fetch(apiUrl + '/v1/intents/' + intentId, { headers: { 'X-API-Key': apiKey } })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (data.status === 'PAID') {
                        document.getElementById('statusBox').className = 'cpay-status paid';
                        document.getElementById('spinner').style.display = 'none';
                        document.getElementById('statusText').textContent = '✅ Payment confirmed!';
                        document.getElementById('statusHint').textContent = 'Redirecting to your order...';
                        setTimeout(function() { window.location.href = returnUrl; }, 2500);
                    }
                }).catch(function() {});
            }
            
            setInterval(updateTimer, 1000);
            setInterval(checkPayment, 5000);
            updateTimer();
            setTimeout(checkPayment, 1000);
        })();
        </script>
        <?php
    }

    public function thankyou_page($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order) return;
        $intent_id = $order->get_meta('_cryptopay_intent_id');
        if (empty($intent_id)) return;
        echo '<div style="background:#ecfdf5;border:1px solid #a7f3d0;border-radius:8px;padding:16px;margin:20px 0;"><p style="margin:0;color:#059669;font-weight:600;">✅ Your crypto payment has been received! Your order is being processed.</p></div>';
    }
}

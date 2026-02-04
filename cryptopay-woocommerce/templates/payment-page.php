<!DOCTYPE html>
<html <?php language_attributes(); ?>>
<head>
    <meta charset="<?php bloginfo('charset'); ?>">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?php echo esc_html__('Pay with CryptoPay', 'cryptopay-woocommerce'); ?> - <?php bloginfo('name'); ?></title>
    <?php wp_head(); ?>
</head>
<body <?php body_class(); ?>>
    <div class="cryptopay-payment-container">
        <div class="cryptopay-payment-box">
            <h1><?php echo esc_html__('Pay with USDT', 'cryptopay-woocommerce'); ?></h1>
            
            <?php if ($intent_data && isset($intent_data['status'])): ?>
                <?php if ($intent_data['status'] === 'PAID'): ?>
                    <div class="cryptopay-status paid">
                        <p class="status-message"><?php echo esc_html__('Payment confirmed! Redirecting...', 'cryptopay-woocommerce'); ?></p>
                    </div>
                <?php elseif ($intent_data['status'] === 'EXPIRED'): ?>
                    <div class="cryptopay-status expired">
                        <p class="status-message"><?php echo esc_html__('Payment expired. Please create a new order.', 'cryptopay-woocommerce'); ?></p>
                    </div>
                <?php else: ?>
                    <div class="cryptopay-payment-info">
                        <div class="amount-section">
                            <p class="label"><?php echo esc_html__('Amount to pay:', 'cryptopay-woocommerce'); ?></p>
                            <p class="amount"><?php echo esc_html(number_format($intent_data['cryptoAmount'] ?? 0, 8)); ?> USDT</p>
                        </div>

                        <div class="address-section">
                            <p class="label"><?php echo esc_html__('Send to address:', 'cryptopay-woocommerce'); ?></p>
                            <div class="address-box">
                                <code id="payment-address"><?php echo esc_html($intent_data['payAddress'] ?? ''); ?></code>
                                <button class="copy-btn" onclick="copyAddress()"><?php echo esc_html__('Copy', 'cryptopay-woocommerce'); ?></button>
                            </div>
                        </div>

                        <div class="qr-section">
                            <?php if (isset($intent_data['qrString'])): ?>
                                <img src="data:image/png;base64,<?php echo esc_attr($intent_data['qrString']); ?>" alt="QR Code" class="qr-code">
                            <?php endif; ?>
                        </div>

                        <div class="countdown-section">
                            <p class="label"><?php echo esc_html__('Time remaining:', 'cryptopay-woocommerce'); ?></p>
                            <p class="countdown" id="countdown">--:--</p>
                        </div>

                        <div class="status-section">
                            <p class="status-message" id="status-message"><?php echo esc_html__('Waiting for payment...', 'cryptopay-woocommerce'); ?></p>
                        </div>
                    </div>
                <?php endif; ?>
            <?php else: ?>
                <div class="cryptopay-status error">
                    <p class="status-message"><?php echo esc_html__('Failed to load payment information. Please try again.', 'cryptopay-woocommerce'); ?></p>
                </div>
            <?php endif; ?>
        </div>
    </div>

    <script>
        function copyAddress() {
            const address = document.getElementById('payment-address').textContent;
            navigator.clipboard.writeText(address).then(function() {
                const btn = event.target;
                const originalText = btn.textContent;
                btn.textContent = '<?php echo esc_js(__('Copied!', 'cryptopay-woocommerce')); ?>';
                setTimeout(function() {
                    btn.textContent = originalText;
                }, 2000);
            });
        }
    </script>
    <?php wp_footer(); ?>
</body>
</html>

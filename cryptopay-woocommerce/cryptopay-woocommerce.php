<?php
/**
 * Plugin Name: CryptoPay (USDT)
 * Plugin URI: https://cryptopay.example.com
 * Description: Accept USDT payments via TRC20 and ERC20 networks
 * Version: 1.0.0
 * Author: CryptoPay
 * Author URI: https://cryptopay.example.com
 * Text Domain: cryptopay-woocommerce
 * Domain Path: /languages
 * Requires at least: 5.8
 * Requires PHP: 7.4
 * WC requires at least: 5.0
 * WC tested up to: 8.0
 */

if (!defined('ABSPATH')) {
    exit;
}

// Define plugin constants
define('CRYPTOPAY_VERSION', '1.0.0');
define('CRYPTOPAY_PLUGIN_DIR', plugin_dir_path(__FILE__));
define('CRYPTOPAY_PLUGIN_URL', plugin_dir_url(__FILE__));

// Activation hook - flush rewrite rules
register_activation_hook(__FILE__, 'cryptopay_activate');
function cryptopay_activate()
{
    cryptopay_add_payment_endpoint();
    flush_rewrite_rules();
}

// Deactivation hook
register_deactivation_hook(__FILE__, 'cryptopay_deactivate');
function cryptopay_deactivate()
{
    flush_rewrite_rules();
}

// Add endpoint on init
add_action('init', 'cryptopay_add_payment_endpoint');
function cryptopay_add_payment_endpoint()
{
    add_rewrite_endpoint('cryptopay-payment', EP_ROOT | EP_PAGES);
}

// Check if WooCommerce is active
add_action('plugins_loaded', 'cryptopay_init', 11);

function cryptopay_init()
{
    if (!class_exists('WooCommerce')) {
        add_action('admin_notices', 'cryptopay_woocommerce_missing_notice');
        return;
    }

    require_once CRYPTOPAY_PLUGIN_DIR . 'includes/class-cryptopay-gateway.php';
    require_once CRYPTOPAY_PLUGIN_DIR . 'includes/class-cryptopay-payment-page.php';
    
    add_filter('woocommerce_payment_gateways', 'cryptopay_add_gateway');
    add_action('rest_api_init', 'cryptopay_register_webhook_endpoint');
}

function cryptopay_woocommerce_missing_notice()
{
    echo '<div class="error"><p><strong>CryptoPay</strong> requires WooCommerce to be installed and active.</p></div>';
}

function cryptopay_add_gateway($gateways)
{
    $gateways[] = 'CryptoPay_Gateway';
    return $gateways;
}

function cryptopay_register_webhook_endpoint()
{
    register_rest_route('cryptopay/v1', '/webhook', array(
        'methods' => 'POST',
        'callback' => 'cryptopay_handle_webhook',
        'permission_callback' => '__return_true',
    ));
}

function cryptopay_handle_webhook($request)
{
    $headers = $request->get_headers();
    $signature = $headers['x_cryptopay_signature'][0] ?? '';
    $payload = $request->get_body();
    
    $options = get_option('woocommerce_cryptopay_settings', array());
    $webhook_secret = $options['webhook_secret'] ?? '';
    
    if (empty($webhook_secret)) {
        return new WP_Error('missing_secret', 'Webhook secret not configured', array('status' => 400));
    }
    
    // Verify HMAC signature
    $expected_signature = hash_hmac('sha256', $payload, $webhook_secret);
    
    if (!hash_equals($expected_signature, $signature)) {
        return new WP_Error('invalid_signature', 'Invalid webhook signature', array('status' => 401));
    }
    
    $data = json_decode($payload, true);
    
    if ($data['eventType'] !== 'payment.paid') {
        return new WP_REST_Response(array('message' => 'Event type not handled'), 200);
    }
    
    $intent_id = $data['intentId'] ?? '';
    if (empty($intent_id)) {
        return new WP_Error('missing_intent_id', 'Intent ID missing', array('status' => 400));
    }
    
    // Find order by intent ID
    $orders = wc_get_orders(array(
        'meta_key' => '_cryptopay_intent_id',
        'meta_value' => $intent_id,
        'limit' => 1,
    ));
    
    if (empty($orders)) {
        return new WP_Error('order_not_found', 'Order not found', array('status' => 404));
    }
    
    $order = $orders[0];
    
    // Update order status
    if ($data['status'] === 'PAID') {
        $order->update_meta_data('_cryptopay_tx_hash', $data['txHash'] ?? '');
        $order->update_meta_data('_cryptopay_confirmations', $data['confirmations'] ?? 0);
        $order->payment_complete();
        $order->save();
    }
    
    return new WP_REST_Response(array('message' => 'Webhook processed'), 200);
}

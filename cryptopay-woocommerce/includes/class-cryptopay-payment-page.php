<?php
if (!defined('ABSPATH')) {
    exit;
}

// Handle payment page display
add_action('template_redirect', 'cryptopay_handle_payment_page');

function cryptopay_handle_payment_page()
{
    global $wp_query;

    if (!isset($wp_query->query_vars['cryptopay-payment'])) {
        return;
    }

    $order_id = isset($_GET['order_id']) ? intval($_GET['order_id']) : 0;
    $intent_id = isset($_GET['intent_id']) ? sanitize_text_field($_GET['intent_id']) : '';
    $key = isset($_GET['key']) ? sanitize_text_field($_GET['key']) : '';

    if (empty($order_id) || empty($intent_id) || empty($key)) {
        wp_die('Invalid payment parameters');
    }

    $order = wc_get_order($order_id);
    if (!$order || $order->get_order_key() !== $key) {
        wp_die('Invalid order');
    }

    $gateway = new CryptoPay_Gateway();
    $api_base_url = $gateway->get_option('api_base_url', '');
    $api_key = $gateway->get_option('api_key', '');

    // Enqueue scripts
    wp_enqueue_script('cryptopay-payment', CRYPTOPAY_PLUGIN_URL . 'assets/js/payment-page.js', array('jquery'), CRYPTOPAY_VERSION, true);
    wp_localize_script('cryptopay-payment', 'cryptopayData', array(
        'apiBaseUrl' => $api_base_url,
        'apiKey' => $api_key,
        'intentId' => $intent_id,
        'orderId' => $order_id,
        'orderKey' => $key,
        'returnUrl' => $gateway->get_return_url($order),
        'ajaxUrl' => admin_url('admin-ajax.php'),
        'expiresAt' => $intent_data['expiresAt'] ?? null,
    ));

    wp_enqueue_style('cryptopay-payment', CRYPTOPAY_PLUGIN_URL . 'assets/css/payment-page.css', array(), CRYPTOPAY_VERSION);

    // Get initial intent data
    $intent_response = wp_remote_get(
        trailingslashit($api_base_url) . 'v1/intents/' . $intent_id,
        array(
            'headers' => array(
                'X-API-Key' => $api_key,
            ),
            'timeout' => 10,
        )
    );

    $intent_data = null;
    if (!is_wp_error($intent_response)) {
        $intent_data = json_decode(wp_remote_retrieve_body($intent_response), true);
    }

    // Output payment page
    include CRYPTOPAY_PLUGIN_DIR . 'templates/payment-page.php';
    exit;
}

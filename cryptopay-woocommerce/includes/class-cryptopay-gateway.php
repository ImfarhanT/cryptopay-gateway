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
        $this->has_fields = false;
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
        add_action('woocommerce_thankyou_' . $this->id, array($this, 'thankyou_page'));
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

        // Determine network (for MVP, use TRC20 if enabled, otherwise ERC20)
        $network = 'TRC20';
        if ($this->enable_trc20 !== 'yes' && $this->enable_erc20 === 'yes') {
            $network = 'ERC20';
        } elseif ($this->enable_trc20 === 'yes' && $this->enable_erc20 === 'yes') {
            // Both enabled - could add user selection in future
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

        $intent_data = json_decode(wp_remote_retrieve_body($response), true);

        if (!isset($intent_data['intentId'])) {
            wc_add_notice('Failed to create payment intent', 'error');
            return array(
                'result' => 'fail',
                'redirect' => '',
            );
        }

        // Store intent ID in order meta
        $order->update_meta_data('_cryptopay_intent_id', $intent_data['intentId']);
        $order->update_meta_data('_cryptopay_network', $network);
        $order->update_status('on-hold', __('Awaiting crypto payment', 'cryptopay-woocommerce'));
        $order->save();

        // Redirect to payment page
        $payment_url = add_query_arg(
            array(
                'order_id' => $order_id,
                'intent_id' => $intent_data['intentId'],
                'key' => $order->get_order_key(),
            ),
            wc_get_checkout_url() . 'cryptopay-payment'
        );

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

        echo '<p>' . esc_html__('Your payment is being processed. You will receive a confirmation email once the transaction is confirmed on the blockchain.', 'cryptopay-woocommerce') . '</p>';
    }
}

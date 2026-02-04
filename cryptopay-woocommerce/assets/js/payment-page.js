(function($) {
    'use strict';

    const cryptopay = {
        apiBaseUrl: cryptopayData.apiBaseUrl,
        apiKey: cryptopayData.apiKey,
        intentId: cryptopayData.intentId,
        returnUrl: cryptopayData.returnUrl,
        pollInterval: 10000, // 10 seconds
        pollTimer: null,
        expiresAt: null,

        init: function() {
            this.startPolling();
            this.updateCountdown();
            setInterval(() => this.updateCountdown(), 1000);
        },

        startPolling: function() {
            this.pollTimer = setInterval(() => {
                this.checkPaymentStatus();
            }, this.pollInterval);
        },

        stopPolling: function() {
            if (this.pollTimer) {
                clearInterval(this.pollTimer);
                this.pollTimer = null;
            }
        },

        checkPaymentStatus: function() {
            $.ajax({
                url: this.apiBaseUrl + '/v1/intents/' + this.intentId,
                method: 'GET',
                headers: {
                    'X-API-Key': this.apiKey
                },
                success: (data) => {
                    this.handleStatusUpdate(data);
                },
                error: (xhr) => {
                    console.error('Error checking payment status:', xhr);
                }
            });
        },

        handleStatusUpdate: function(data) {
            const status = data.status?.toUpperCase();
            const statusMessage = $('#status-message');

            if (status === 'PAID') {
                statusMessage.text('Payment confirmed! Redirecting...');
                statusMessage.parent().addClass('paid');
                this.stopPolling();
                
                // Redirect after 2 seconds
                setTimeout(() => {
                    window.location.href = this.returnUrl;
                }, 2000);
            } else if (status === 'EXPIRED') {
                statusMessage.text('Payment expired. Please create a new order.');
                statusMessage.parent().addClass('expired');
                this.stopPolling();
            } else if (status === 'FAILED') {
                statusMessage.text('Payment failed. Please try again.');
                statusMessage.parent().addClass('failed');
                this.stopPolling();
            }

            // Update countdown if expiresAt is provided
            if (data.expiresAt) {
                this.expiresAt = new Date(data.expiresAt);
            }
        },

        updateCountdown: function() {
            if (!this.expiresAt) {
                return;
            }

            const now = new Date();
            const diff = this.expiresAt - now;

            if (diff <= 0) {
                $('#countdown').text('00:00');
                return;
            }

            const minutes = Math.floor(diff / 60000);
            const seconds = Math.floor((diff % 60000) / 1000);
            $('#countdown').text(
                String(minutes).padStart(2, '0') + ':' + 
                String(seconds).padStart(2, '0')
            );
        }
    };

    $(document).ready(function() {
        cryptopay.init();
    });
})(jQuery);

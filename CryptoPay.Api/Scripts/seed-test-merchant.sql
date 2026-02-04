-- Script to create a test merchant
-- Replace the values below with your actual values

-- Generate a new merchant
DECLARE @MerchantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(64) = 'test-api-key-' + CONVERT(NVARCHAR(36), NEWID()); -- Replace with your generated API key
DECLARE @WebhookSecret NVARCHAR(64) = 'test-webhook-secret-' + CONVERT(NVARCHAR(36), NEWID()); -- Replace with your generated webhook secret
DECLARE @WebhookUrl NVARCHAR(500) = 'https://your-wordpress-site.com/wp-json/cryptopay/v1/webhook'; -- Replace with your webhook URL

INSERT INTO Merchants (Id, Name, ApiKey, WebhookUrl, WebhookSecret, IsActive, CreatedAt)
VALUES (
    @MerchantId,
    'Test Merchant',
    @ApiKey,
    @WebhookUrl,
    @WebhookSecret,
    1,
    GETUTCDATE()
);

-- Output the created merchant details
SELECT 
    Id AS MerchantId,
    Name,
    ApiKey,
    WebhookUrl,
    'WebhookSecret: ' + WebhookSecret AS WebhookSecretNote
FROM Merchants
WHERE Id = @MerchantId;

-- Note: Store the ApiKey and WebhookSecret securely - they won't be shown again!

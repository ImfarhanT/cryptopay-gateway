# Deploy CryptoPay API to Google Cloud Platform

This guide will help you deploy the CryptoPay backend API to Google Cloud Platform using Cloud Run and Cloud SQL.

## Prerequisites

1. Google Cloud account with billing enabled
2. Google Cloud SDK installed ([Install Guide](https://cloud.google.com/sdk/docs/install))
3. .NET 8 SDK installed locally

## Step 1: Install and Configure Google Cloud SDK

```bash
# Install Google Cloud SDK (if not already installed)
# macOS:
brew install google-cloud-sdk

# Or download from: https://cloud.google.com/sdk/docs/install

# Initialize and login
gcloud init
gcloud auth login
```

## Step 2: Create Google Cloud Project

```bash
# Create a new project (replace with your preferred project ID)
gcloud projects create cryptopay-backend --name="CryptoPay Backend"

# Set as active project
gcloud config set project cryptopay-backend

# Enable billing (required for Cloud SQL and Cloud Run)
# Do this via: https://console.cloud.google.com/billing

# Enable required APIs
gcloud services enable run.googleapis.com
gcloud services enable sqladmin.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable containerregistry.googleapis.com
```

## Step 3: Set Up Cloud SQL (SQL Server Database)

```bash
# Create Cloud SQL instance (SQL Server)
# Note: db-f1-micro is the smallest/cheapest tier (~$25/month)
# For production, consider db-n1-standard-1 or higher
gcloud sql instances create cryptopay-db \
  --database-version=SQLSERVER_2019_STANDARD \
  --tier=db-f1-micro \
  --region=us-central1 \
  --root-password=YOUR_SECURE_PASSWORD_HERE \
  --storage-type=SSD \
  --storage-size=20GB

# Wait for instance to be created (takes 5-10 minutes)
# Check status:
gcloud sql instances describe cryptopay-db

# Create database
gcloud sql databases create CryptoPayDb --instance=cryptopay-db

# Get connection name (save this - you'll need it)
gcloud sql instances describe cryptopay-db --format="value(connectionName)"
# Output format: PROJECT_ID:REGION:INSTANCE_NAME
```

**Save the connection name** - you'll need it for the connection string.

## Step 4: Configure Connection String

Update the connection string in `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=/cloudsql/PROJECT_ID:us-central1:cryptopay-db;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

Replace:
- `PROJECT_ID` with your actual project ID
- `YOUR_PASSWORD` with the root password you set

## Step 5: Set Up Environment Variables

Create a `.env` file or set environment variables for sensitive data:

```bash
# Set environment variables (optional - can also use Secret Manager)
gcloud run services update cryptopay-api \
  --set-env-vars="ConnectionStrings__DefaultConnection=Server=/cloudsql/PROJECT_ID:us-central1:cryptopay-db;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

**Better approach:** Use Google Secret Manager for sensitive data (see Step 8).

## Step 6: Build and Deploy to Cloud Run

### Option A: Deploy using Cloud Build (Recommended)

```bash
# Navigate to the API directory
cd CryptoPay.Api

# Submit build to Cloud Build
gcloud builds submit --config cloudbuild.yaml

# This will:
# 1. Build the Docker image
# 2. Push to Container Registry
# 3. Deploy to Cloud Run
```

### Option B: Manual Deployment

```bash
# Set project
gcloud config set project cryptopay-backend

# Build Docker image
docker build -t gcr.io/cryptopay-backend/cryptopay-api .

# Push to Container Registry
docker push gcr.io/cryptopay-backend/cryptopay-api

# Deploy to Cloud Run
gcloud run deploy cryptopay-api \
  --image gcr.io/cryptopay-backend/cryptopay-api \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-cloudsql-instances PROJECT_ID:us-central1:cryptopay-db \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --memory 512Mi \
  --cpu 1 \
  --timeout 300 \
  --max-instances 10
```

## Step 7: Run Database Migrations

After deployment, you need to run migrations. You can do this by:

### Option A: Using Cloud Run Job (Recommended)

```bash
# Create a migration job
gcloud run jobs create cryptopay-migrate \
  --image gcr.io/cryptopay-backend/cryptopay-api \
  --region us-central1 \
  --set-cloudsql-instances PROJECT_ID:us-central1:cryptopay-db \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --command dotnet \
  --args "ef,database,update" \
  --memory 512Mi \
  --cpu 1

# Execute the job
gcloud run jobs execute cryptopay-migrate --region us-central1
```

### Option B: Run migrations locally (if you have SQL Server client)

```bash
# Update connection string temporarily to Cloud SQL public IP
# Get public IP:
gcloud sql instances describe cryptopay-db --format="value(ipAddresses[0].ipAddress)"

# Use connection string with public IP:
# Server=YOUR_PUBLIC_IP;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;

# Run migration
cd CryptoPay.Api
dotnet ef database update
```

### Option C: Use Cloud SQL Proxy (Most Secure)

```bash
# Download Cloud SQL Proxy
# macOS:
curl -o cloud-sql-proxy https://storage.googleapis.com/cloud-sql-connectors/cloud-sql-proxy/v2.8.0/cloud-sql-proxy.darwin.arm64
chmod +x cloud-sql-proxy

# Start proxy (in separate terminal)
./cloud-sql-proxy PROJECT_ID:us-central1:cryptopay-db

# Update connection string to: Server=127.0.0.1;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;

# Run migrations
cd CryptoPay.Api
dotnet ef database update
```

## Step 8: Set Up Secrets (Optional but Recommended)

Store sensitive data in Secret Manager:

```bash
# Create secrets
echo -n "YOUR_DB_PASSWORD" | gcloud secrets create db-password --data-file=-
echo -n "YOUR_INFURA_KEY" | gcloud secrets create infura-key --data-file=-
echo -n "YOUR_WEBHOOK_SECRET" | gcloud secrets create webhook-secret --data-file=-

# Grant Cloud Run access to secrets
gcloud secrets add-iam-policy-binding db-password \
  --member="serviceAccount:PROJECT_NUMBER-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

Update `Program.cs` to read from Secret Manager (requires additional code).

## Step 9: Seed Initial Data

### Seed Wallet Addresses

```bash
# Get your Cloud Run service URL
SERVICE_URL=$(gcloud run services describe cryptopay-api --region us-central1 --format="value(status.url)")

# Seed addresses (replace with your actual addresses)
curl -X POST $SERVICE_URL/admin/seed-addresses \
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

### Create Merchant

Use the SQL script or connect to Cloud SQL and run:

```sql
-- Connect to Cloud SQL and run:
DECLARE @MerchantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(64) = 'your-generated-api-key';
DECLARE @WebhookSecret NVARCHAR(64) = 'your-generated-webhook-secret';
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

SELECT Id AS MerchantId, ApiKey, WebhookSecret FROM Merchants WHERE Id = @MerchantId;
```

## Step 10: Get Your API URL

```bash
# Get the service URL
gcloud run services describe cryptopay-api --region us-central1 --format="value(status.url)"

# Example output: https://cryptopay-api-xxxxx-uc.a.run.app
```

Use this URL in your WooCommerce plugin configuration.

## Step 11: Update WooCommerce Plugin

In your WooCommerce plugin settings:
- **Backend API Base URL**: `https://cryptopay-api-xxxxx-uc.a.run.app`
- Use the Merchant ID, API Key, and Webhook Secret from your database

## Step 12: Test the Deployment

```bash
# Health check
curl https://YOUR-SERVICE-URL/health

# Should return: {"status":"healthy"}
```

## Monitoring and Logs

```bash
# View logs
gcloud run services logs read cryptopay-api --region us-central1

# View in console
# https://console.cloud.google.com/run
```

## Cost Estimation

- **Cloud Run**: Free tier (2 million requests/month), then ~$0.40 per million requests
- **Cloud SQL (db-f1-micro)**: ~$25/month
- **Cloud Build**: Free tier (120 build-minutes/day)
- **Container Registry**: Free tier (0.5 GB storage)

**Estimated monthly cost**: ~$25-30 for small to medium traffic

## Troubleshooting

### Service won't start
- Check logs: `gcloud run services logs read cryptopay-api --region us-central1`
- Verify connection string format
- Check Cloud SQL instance is running

### Database connection errors
- Verify Cloud SQL instance name in connection string
- Check that Cloud Run has Cloud SQL connection enabled
- Verify database and user credentials

### Migrations not running
- Ensure migrations are included in the Docker image
- Check that `db.Database.Migrate()` is called in `Program.cs` (it is)

### Background worker not running
- Cloud Run supports background services
- Verify the `BlockchainPollingWorker` is registered in `Program.cs`

## Scaling

Cloud Run automatically scales based on traffic. You can set limits:

```bash
gcloud run services update cryptopay-api \
  --min-instances 1 \
  --max-instances 10 \
  --region us-central1
```

## Security Best Practices

1. **Use Secret Manager** for sensitive data
2. **Enable Cloud Armor** for DDoS protection
3. **Restrict CORS** to your WordPress domain
4. **Use IAM** to control access
5. **Enable VPC** for private Cloud SQL access (advanced)

## Next Steps

1. Set up custom domain (optional)
2. Configure SSL certificate
3. Set up monitoring and alerts
4. Implement blockchain providers
5. Set up CI/CD pipeline

## Useful Commands

```bash
# View service details
gcloud run services describe cryptopay-api --region us-central1

# Update service
gcloud run services update cryptopay-api --region us-central1

# Delete service (if needed)
gcloud run services delete cryptopay-api --region us-central1

# View Cloud SQL instances
gcloud sql instances list

# Connect to Cloud SQL
gcloud sql connect cryptopay-db --user=sqlserver
```

## Support

For issues, check:
- Cloud Run logs
- Cloud SQL logs
- Application logs in Cloud Logging

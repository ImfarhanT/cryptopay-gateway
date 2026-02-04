# Quick Deploy Guide - Google Cloud Platform

This is a simplified guide to get you up and running quickly.

## Prerequisites Checklist

- [ ] Google Cloud account with billing enabled
- [ ] Google Cloud SDK installed (`gcloud`)
- [ ] Docker installed (for building images)
- [ ] Project created in Google Cloud Console

## Step 1: Initial Setup (One-time)

```bash
# Login to Google Cloud
gcloud auth login

# Create or select project
gcloud projects create cryptopay-backend --name="CryptoPay Backend"
gcloud config set project cryptopay-backend

# Enable billing (via console: https://console.cloud.google.com/billing)

# Enable required APIs
gcloud services enable run.googleapis.com
gcloud services enable sqladmin.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable containerregistry.googleapis.com
```

## Step 2: Create Database

```bash
# Create Cloud SQL instance (takes 5-10 minutes)
gcloud sql instances create cryptopay-db \
  --database-version=SQLSERVER_2019_STANDARD \
  --tier=db-f1-micro \
  --region=us-central1 \
  --root-password=YOUR_SECURE_PASSWORD

# Create database
gcloud sql databases create CryptoPayDb --instance=cryptopay-db

# Get connection name (save this!)
gcloud sql instances describe cryptopay-db --format="value(connectionName)"
```

## Step 3: Deploy Using Script

```bash
cd CryptoPay.Api
./deploy.sh
```

The script will:
- Check for Cloud SQL instance
- Build Docker image
- Push to Container Registry
- Deploy to Cloud Run

## Step 4: Update Connection String

After deployment, update the connection string. Get your connection name:

```bash
CONNECTION_NAME=$(gcloud sql instances describe cryptopay-db --format="value(connectionName)")
echo $CONNECTION_NAME
```

Then update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=/cloudsql/PROJECT_ID:us-central1:cryptopay-db;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

Or set as environment variable:

```bash
gcloud run services update cryptopay-api \
  --set-env-vars="ConnectionStrings__DefaultConnection=Server=/cloudsql/$CONNECTION_NAME;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;" \
  --region us-central1
```

## Step 5: Get Your API URL

```bash
gcloud run services describe cryptopay-api --region us-central1 --format="value(status.url)"
```

Use this URL in your WooCommerce plugin settings.

## Step 6: Test

```bash
# Health check
curl https://YOUR-SERVICE-URL/health

# Should return: {"status":"healthy"}
```

## Step 7: Seed Data

```bash
# Seed addresses
curl -X POST https://YOUR-SERVICE-URL/admin/seed-addresses \
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

## Troubleshooting

### "Permission denied" errors
```bash
# Grant yourself necessary permissions
gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="user:YOUR_EMAIL" \
  --role="roles/owner"
```

### Database connection errors
- Verify connection string format
- Check Cloud SQL instance is running
- Ensure Cloud Run has Cloud SQL connection enabled

### Build fails
- Ensure Docker is running
- Check you're in the CryptoPay.Api directory
- Verify Dockerfile exists

## Manual Deployment (Alternative)

If the script doesn't work, deploy manually:

```bash
# Build
docker build -t gcr.io/PROJECT_ID/cryptopay-api:latest .

# Push
docker push gcr.io/PROJECT_ID/cryptopay-api:latest

# Deploy
gcloud run deploy cryptopay-api \
  --image gcr.io/PROJECT_ID/cryptopay-api:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-cloudsql-instances PROJECT_ID:us-central1:cryptopay-db \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --memory 512Mi \
  --cpu 1
```

## Cost Estimate

- Cloud Run: Free tier (2M requests/month)
- Cloud SQL (db-f1-micro): ~$25/month
- **Total: ~$25-30/month**

## Next Steps

1. Set up monitoring and alerts
2. Configure custom domain (optional)
3. Implement blockchain providers
4. Set up CI/CD pipeline

For detailed information, see `DEPLOY_GCP.md`.

# Deploy CryptoPay API to Google Cloud - 100% Online (No Local Tools Required)

This guide will help you deploy the CryptoPay backend API to Google Cloud Platform using **only the web browser** - no command line tools or local installations needed!

## Prerequisites

- ✅ Google Cloud account (you already have this)
- ✅ Web browser
- ✅ Your code uploaded to GitHub (or we'll use Cloud Shell)

## Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the **project dropdown** at the top
3. Click **"New Project"**
4. Enter project name: `cryptopay-backend`
5. Click **"Create"**
6. Wait for project creation, then select it from the dropdown

## Step 2: Enable Billing

1. Go to [Billing](https://console.cloud.google.com/billing)
2. Link a billing account to your project
3. **Note:** You'll get $300 free credit for new accounts!

## Step 3: Enable Required APIs

1. Go to [APIs & Services](https://console.cloud.google.com/apis/library)
2. Search for and enable each of these:
   - **Cloud Run API** - Click "Enable"
   - **Cloud SQL Admin API** - Click "Enable"
   - **Cloud Build API** - Click "Enable"
   - **Container Registry API** - Click "Enable"

## Step 4: Create Cloud SQL Database

1. Go to [Cloud SQL Instances](https://console.cloud.google.com/sql/instances)
2. Click **"Create Instance"**
3. Choose **"SQL Server"**
4. Click **"Next"**
5. Fill in the form:
   - **Instance ID**: `cryptopay-db`
   - **Password**: Create a strong password (save it!)
   - **Database version**: SQL Server 2019 Standard
   - **Region**: Choose closest to you (e.g., `us-central1`)
   - **Zone**: Leave default
6. Click **"Show configuration options"**
   - **Machine type**: `db-f1-micro` (cheapest option)
   - **Storage**: 20 GB SSD
7. Click **"Create Instance"**
8. Wait 5-10 minutes for creation

### Create Database

1. Once instance is created, click on it
2. Go to **"Databases"** tab
3. Click **"Create Database"**
4. Name: `CryptoPayDb`
5. Click **"Create"**

### Get Connection Name

1. In the instance details, find **"Connection name"**
2. It looks like: `PROJECT_ID:REGION:cryptopay-db`
3. **Copy this** - you'll need it later

## Step 5: Upload Code to Cloud Shell

We'll use Google Cloud Shell (browser-based terminal) to deploy:

1. Click the **Cloud Shell icon** (terminal icon) at the top right of Google Cloud Console
2. Wait for Cloud Shell to open (first time takes ~1 minute)
3. In Cloud Shell, run:

```bash
# Clone your project (or upload files)
# Option A: If you have GitHub repo
git clone YOUR_REPO_URL
cd YOUR_REPO_NAME

# Option B: If code is local, we'll create it in Cloud Shell
# (We'll do this in next step)
```

## Step 6: Create Project Files in Cloud Shell

If you don't have the code in a repo, we'll create it in Cloud Shell:

```bash
# Create project structure
mkdir -p CryptoPay.Api
cd CryptoPay.Api

# You'll need to upload your files or create them
# For now, let's prepare for upload
```

**Alternative:** Upload files via Cloud Shell Editor:
1. Click the **pencil icon** (Open Editor) in Cloud Shell
2. Create folders and files
3. Copy-paste your code files

## Step 7: Build and Deploy Using Cloud Build

### Option A: Using Cloud Build (Recommended - Fully Online)

1. Go to [Cloud Build](https://console.cloud.google.com/cloud-build)
2. Click **"Triggers"** → **"Create Trigger"**
3. Connect your repository (GitHub, GitLab, etc.) or use Cloud Source Repositories
4. Configure:
   - **Name**: `cryptopay-deploy`
   - **Event**: Push to branch
   - **Branch**: `main` or `master`
   - **Configuration**: Cloud Build configuration file
   - **Location**: `CryptoPay.Api/cloudbuild.yaml`
5. Click **"Create"**

### Option B: Manual Build via Cloud Shell

In Cloud Shell, run:

```bash
cd CryptoPay.Api

# Set your project
gcloud config set project cryptopay-backend

# Submit build
gcloud builds submit --config cloudbuild.yaml
```

This will:
- Build your Docker image
- Push to Container Registry
- Deploy to Cloud Run

## Step 8: Configure Cloud Run Service

After deployment, configure the service:

1. Go to [Cloud Run](https://console.cloud.google.com/run)
2. Click on **"cryptopay-api"** service
3. Click **"Edit & Deploy New Revision"**
4. Go to **"Variables & Secrets"** tab
5. Add environment variables:
   - **Name**: `ASPNETCORE_ENVIRONMENT`
   - **Value**: `Production`
   
   - **Name**: `ConnectionStrings__DefaultConnection`
   - **Value**: `Server=/cloudsql/PROJECT_ID:REGION:cryptopay-db;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;`
     (Replace PROJECT_ID, REGION, and YOUR_PASSWORD)

6. Go to **"Connections"** tab
7. Under **"Cloud SQL connections"**, select `cryptopay-db`
8. Click **"Deploy"**

## Step 9: Get Your API URL

1. In Cloud Run, click on your service
2. Find the **"URL"** at the top
3. It looks like: `https://cryptopay-api-xxxxx-uc.a.run.app`
4. **Copy this URL** - use it in WooCommerce plugin

## Step 10: Test Your API

1. In Cloud Run, click on your service
2. Copy the URL
3. Open in browser: `https://YOUR-URL/health`
4. Should see: `{"status":"healthy"}`

## Step 11: Seed Wallet Addresses

1. Go to [Cloud Run](https://console.cloud.google.com/run)
2. Click on your service
3. Copy the URL
4. Use any online tool like [Postman Web](https://web.postman.co/) or [HTTPie Web](https://httpie.io/app) to send:

**POST Request:**
- URL: `https://YOUR-URL/admin/seed-addresses`
- Method: POST
- Headers: `Content-Type: application/json`
- Body:
```json
{
  "network": "TRC20",
  "count": 3,
  "addresses": [
    "TYourTronAddress1",
    "TYourTronAddress1",
    "TYourTronAddress3"
  ]
}
```

## Step 12: Create Merchant in Database

1. Go to [Cloud SQL](https://console.cloud.google.com/sql/instances)
2. Click on `cryptopay-db`
3. Click **"Open Cloud Shell"** (or use SQL Server Management Studio if you have it)
4. Connect to database
5. Run this SQL (via Cloud Shell or SQL client):

```sql
DECLARE @MerchantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(64) = 'your-generated-api-key-here';
DECLARE @WebhookSecret NVARCHAR(64) = 'your-generated-webhook-secret-here';
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

**Generate API keys online:**
- Visit: https://www.random.org/strings/ (generate 64 character hex strings)
- Or use: https://generate-random.org/api-key-generator

## Step 13: Update WooCommerce Plugin

In your WooCommerce plugin settings:
- **Backend API Base URL**: `https://YOUR-CLOUD-RUN-URL`
- **Merchant ID**: From database query above
- **API Key**: From database query above
- **Webhook Secret**: From database query above

## Monitoring (All Online)

### View Logs
1. Go to [Cloud Run](https://console.cloud.google.com/run)
2. Click on your service
3. Click **"Logs"** tab
4. See all application logs in real-time

### View Metrics
1. In Cloud Run service page
2. See **"Metrics"** tab
3. View requests, latency, errors

### Database Management
1. Go to [Cloud SQL](https://console.cloud.google.com/sql/instances)
2. Click on your instance
3. Use **"Databases"** tab to manage
4. Use **"Users"** tab for access control

## Troubleshooting (All Online)

### Service Not Starting
1. Go to Cloud Run → Your Service → Logs
2. Check for error messages
3. Verify connection string format

### Database Connection Issues
1. Verify Cloud SQL instance is running
2. Check connection string in environment variables
3. Ensure Cloud SQL connection is enabled in Cloud Run

### Can't Access API
1. Check Cloud Run service is deployed
2. Verify URL is correct
3. Check service logs for errors

## Cost Management

- **Cloud Run**: Free tier (2M requests/month)
- **Cloud SQL (db-f1-micro)**: ~$25/month
- **Cloud Build**: Free tier (120 build-minutes/day)
- **Total**: ~$25-30/month

Monitor costs at: [Billing Dashboard](https://console.cloud.google.com/billing)

## Next Steps

1. ✅ Set up monitoring alerts
2. ✅ Configure custom domain (optional)
3. ✅ Set up CI/CD pipeline
4. ✅ Implement blockchain providers
5. ✅ Add more wallet addresses

## Quick Links

- [Cloud Console](https://console.cloud.google.com/)
- [Cloud Run](https://console.cloud.google.com/run)
- [Cloud SQL](https://console.cloud.google.com/sql/instances)
- [Cloud Build](https://console.cloud.google.com/cloud-build)
- [Cloud Shell](https://shell.cloud.google.com/)

---

**Everything can be done through the web browser!** No local installations needed.

# 🚀 Copy-Paste Deployment - Do It For Me!

This is the **simplest way** to deploy. Just copy and paste these commands into Google Cloud Shell.

## Step 1: Open Google Cloud Shell

1. Go to: https://console.cloud.google.com/
2. Click the **Cloud Shell icon** (terminal icon) at the top right
3. Wait for it to open (~1 minute first time)

## Step 2: Upload Your Code

### Option A: If you have GitHub
```bash
git clone YOUR_REPO_URL
cd YOUR_REPO_NAME
cd CryptoPay.Api
```

### Option B: Upload via Cloud Shell Editor
1. Click the **pencil icon** (Open Editor) in Cloud Shell
2. Click **File** → **Upload**
3. Select all files from your `CryptoPay.Api` folder
4. Upload them

## Step 3: Run This One Command

Copy and paste this entire block into Cloud Shell:

```bash
#!/bin/bash
set -e

echo "🚀 Starting automated deployment..."

# Get or set project
PROJECT_ID=$(gcloud config get-value project 2>/dev/null || echo "")
if [ -z "$PROJECT_ID" ]; then
    read -p "Enter project ID (or create new): " PROJECT_ID
    gcloud projects create $PROJECT_ID --name="CryptoPay Backend" 2>/dev/null || true
    gcloud config set project $PROJECT_ID
fi

echo "📋 Project: $PROJECT_ID"

# Enable APIs
echo "🔌 Enabling APIs..."
gcloud services enable run.googleapis.com sqladmin.googleapis.com cloudbuild.googleapis.com containerregistry.googleapis.com --project=$PROJECT_ID

# Create SQL instance if needed
SQL_INSTANCE="cryptopay-db"
REGION="us-central1"

if ! gcloud sql instances describe $SQL_INSTANCE --project=$PROJECT_ID &> /dev/null; then
    echo "🗄️  Creating database (5-10 minutes)..."
    read -sp "Enter SQL password (min 8 chars): " DB_PASSWORD
    echo ""
    
    gcloud sql instances create $SQL_INSTANCE \
        --database-version=SQLSERVER_2019_STANDARD \
        --tier=db-f1-micro \
        --region=$REGION \
        --root-password=$DB_PASSWORD \
        --storage-type=SSD \
        --storage-size=20GB \
        --project=$PROJECT_ID
    
    gcloud sql databases create CryptoPayDb --instance=$SQL_INSTANCE --project=$PROJECT_ID
    echo "✅ Database created"
else
    echo "✅ Database exists"
    read -sp "Enter SQL password: " DB_PASSWORD
    echo ""
fi

CONNECTION_NAME=$(gcloud sql instances describe $SQL_INSTANCE --project=$PROJECT_ID --format="value(connectionName)")

# Navigate to code
if [ ! -f "CryptoPay.Api.csproj" ]; then
    [ -d "CryptoPay.Api" ] && cd CryptoPay.Api || [ -d "../CryptoPay.Api" ] && cd ../CryptoPay.Api
fi

# Build and deploy
echo "🏗️  Building and deploying (5-10 minutes)..."
BUILD_CONFIG="cloudbuild-online.yaml"
[ ! -f "$BUILD_CONFIG" ] && BUILD_CONFIG="cloudbuild.yaml"

gcloud builds submit --config=$BUILD_CONFIG --project=$PROJECT_ID

# Configure service
echo "⚙️  Configuring service..."
gcloud run services update cryptopay-api \
    --region=$REGION \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,ConnectionStrings__DefaultConnection=Server=/cloudsql/$CONNECTION_NAME;Database=CryptoPayDb;User Id=sqlserver;Password=$DB_PASSWORD;TrustServerCertificate=True;" \
    --add-cloudsql-instances=$CONNECTION_NAME \
    --project=$PROJECT_ID

# Get URL
SERVICE_URL=$(gcloud run services describe cryptopay-api --region=$REGION --project=$PROJECT_ID --format="value(status.url)")

echo ""
echo "✅ DEPLOYMENT COMPLETE!"
echo "🌐 Your API: $SERVICE_URL"
echo "🔗 Connection: $CONNECTION_NAME"
echo ""
echo "Test it: curl $SERVICE_URL/health"
```

**That's it!** The script does everything automatically.

## What It Does

1. ✅ Sets up your project
2. ✅ Enables all required APIs
3. ✅ Creates Cloud SQL database
4. ✅ Builds your Docker image
5. ✅ Deploys to Cloud Run
6. ✅ Configures environment variables
7. ✅ Connects to database
8. ✅ Gives you the API URL

## After Deployment

1. **Test your API:**
   ```bash
   curl YOUR_API_URL/health
   ```

2. **Seed addresses:**
   ```bash
   curl -X POST YOUR_API_URL/admin/seed-addresses \
     -H "Content-Type: application/json" \
     -d '{"network":"TRC20","count":3,"addresses":["addr1","addr2","addr3"]}'
   ```

3. **Create merchant** (in Cloud SQL):
   ```bash
   gcloud sql connect cryptopay-db --user=sqlserver
   # Then run SQL from Scripts/seed-test-merchant.sql
   ```

4. **Update WooCommerce plugin** with your API URL

## Troubleshooting

If something fails:
- Check Cloud Build logs: https://console.cloud.google.com/cloud-build
- Check Cloud Run logs: https://console.cloud.google.com/run
- Make sure billing is enabled

---

**That's the easiest way!** Just copy-paste and it does everything for you! 🎉

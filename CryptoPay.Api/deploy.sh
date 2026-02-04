#!/bin/bash

# CryptoPay API - Google Cloud Deployment Script
# This script automates the deployment process to Google Cloud Platform

set -e  # Exit on error

echo "🚀 CryptoPay API - Google Cloud Deployment"
echo "=========================================="
echo ""

# Check if gcloud is installed
if ! command -v gcloud &> /dev/null; then
    echo "❌ Error: gcloud CLI is not installed"
    echo "Please install from: https://cloud.google.com/sdk/docs/install"
    exit 1
fi

# Get project ID
PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
if [ -z "$PROJECT_ID" ]; then
    echo "❌ Error: No Google Cloud project set"
    echo "Run: gcloud config set project YOUR_PROJECT_ID"
    exit 1
fi

echo "📋 Project ID: $PROJECT_ID"
echo ""

# Prompt for region
read -p "Enter region (default: us-central1): " REGION
REGION=${REGION:-us-central1}
echo "📍 Region: $REGION"
echo ""

# Check if Cloud SQL instance exists
echo "🔍 Checking for Cloud SQL instance..."
SQL_INSTANCE="cryptopay-db"
if ! gcloud sql instances describe $SQL_INSTANCE &> /dev/null; then
    echo "⚠️  Cloud SQL instance '$SQL_INSTANCE' not found"
    echo ""
    read -p "Do you want to create it now? (y/n): " CREATE_DB
    if [ "$CREATE_DB" = "y" ]; then
        read -sp "Enter SQL Server root password: " DB_PASSWORD
        echo ""
        echo "⏳ Creating Cloud SQL instance (this may take 5-10 minutes)..."
        gcloud sql instances create $SQL_INSTANCE \
            --database-version=SQLSERVER_2019_STANDARD \
            --tier=db-f1-micro \
            --region=$REGION \
            --root-password=$DB_PASSWORD \
            --storage-type=SSD \
            --storage-size=20GB
        
        echo "✅ Cloud SQL instance created"
        echo "⏳ Creating database..."
        gcloud sql databases create CryptoPayDb --instance=$SQL_INSTANCE
        echo "✅ Database created"
    else
        echo "❌ Cannot proceed without database. Exiting."
        exit 1
    fi
else
    echo "✅ Cloud SQL instance found"
fi

# Get connection name
CONNECTION_NAME=$(gcloud sql instances describe $SQL_INSTANCE --format="value(connectionName)")
echo "🔗 Connection name: $CONNECTION_NAME"
echo ""

# Build and deploy
echo "🏗️  Building Docker image..."
docker build -t gcr.io/$PROJECT_ID/cryptopay-api:latest .

echo "📤 Pushing to Container Registry..."
docker push gcr.io/$PROJECT_ID/cryptopay-api:latest

echo "🚀 Deploying to Cloud Run..."
gcloud run deploy cryptopay-api \
    --image gcr.io/$PROJECT_ID/cryptopay-api:latest \
    --platform managed \
    --region $REGION \
    --allow-unauthenticated \
    --set-cloudsql-instances $CONNECTION_NAME \
    --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
    --memory 512Mi \
    --cpu 1 \
    --timeout 300 \
    --max-instances 10 \
    --port 8080

# Get service URL
SERVICE_URL=$(gcloud run services describe cryptopay-api --region $REGION --format="value(status.url)")
echo ""
echo "✅ Deployment complete!"
echo "🌐 Service URL: $SERVICE_URL"
echo ""
echo "📝 Next steps:"
echo "1. Update connection string in appsettings.Production.json with:"
echo "   Server=/cloudsql/$CONNECTION_NAME;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
echo ""
echo "2. Test health endpoint:"
echo "   curl $SERVICE_URL/health"
echo ""
echo "3. Seed wallet addresses:"
echo "   curl -X POST $SERVICE_URL/admin/seed-addresses \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"network\":\"TRC20\",\"count\":3,\"addresses\":[\"addr1\",\"addr2\",\"addr3\"]}'"
echo ""
echo "4. Update WooCommerce plugin with this URL: $SERVICE_URL"

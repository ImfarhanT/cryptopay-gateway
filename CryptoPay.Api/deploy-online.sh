#!/bin/bash

# CryptoPay API - Fully Automated Google Cloud Deployment
# Run this script in Google Cloud Shell (browser-based terminal)
# No local installations needed!

set -e  # Exit on error

echo "🚀 CryptoPay API - Automated Google Cloud Deployment"
echo "===================================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Get or create project
echo -e "${BLUE}📋 Step 1: Setting up project...${NC}"
PROJECT_ID=$(gcloud config get-value project 2>/dev/null)

if [ -z "$PROJECT_ID" ]; then
    echo -e "${YELLOW}No project set. Creating new project...${NC}"
    read -p "Enter project name (default: cryptopay-backend): " PROJECT_NAME
    PROJECT_NAME=${PROJECT_NAME:-cryptopay-backend}
    
    # Create project
    gcloud projects create $PROJECT_NAME --name="CryptoPay Backend"
    gcloud config set project $PROJECT_NAME
    PROJECT_ID=$PROJECT_NAME
    echo -e "${GREEN}✅ Project created: $PROJECT_ID${NC}"
else
    echo -e "${GREEN}✅ Using existing project: $PROJECT_ID${NC}"
fi

# Step 2: Enable billing check
echo ""
echo -e "${BLUE}💰 Step 2: Checking billing...${NC}"
BILLING_ACCOUNT=$(gcloud beta billing projects describe $PROJECT_ID --format="value(billingAccountName)" 2>/dev/null || echo "")

if [ -z "$BILLING_ACCOUNT" ]; then
    echo -e "${YELLOW}⚠️  Billing not enabled. Please enable billing:${NC}"
    echo "   https://console.cloud.google.com/billing?project=$PROJECT_ID"
    read -p "Press Enter after enabling billing..."
else
    echo -e "${GREEN}✅ Billing enabled${NC}"
fi

# Step 3: Enable required APIs
echo ""
echo -e "${BLUE}🔌 Step 3: Enabling required APIs...${NC}"
gcloud services enable run.googleapis.com --project=$PROJECT_ID
gcloud services enable sqladmin.googleapis.com --project=$PROJECT_ID
gcloud services enable cloudbuild.googleapis.com --project=$PROJECT_ID
gcloud services enable containerregistry.googleapis.com --project=$PROJECT_ID
echo -e "${GREEN}✅ APIs enabled${NC}"

# Step 4: Create or check Cloud SQL instance
echo ""
echo -e "${BLUE}🗄️  Step 4: Setting up Cloud SQL database...${NC}"
SQL_INSTANCE="cryptopay-db"
REGION="us-central1"

if ! gcloud sql instances describe $SQL_INSTANCE --project=$PROJECT_ID &> /dev/null; then
    echo -e "${YELLOW}Creating Cloud SQL instance (this takes 5-10 minutes)...${NC}"
    
    # Prompt for database password
    read -sp "Enter SQL Server root password (min 8 chars): " DB_PASSWORD
    echo ""
    
    if [ ${#DB_PASSWORD} -lt 8 ]; then
        echo -e "${YELLOW}Password too short, using generated password...${NC}"
        DB_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-16)
        echo -e "${GREEN}Generated password: $DB_PASSWORD${NC}"
        echo -e "${YELLOW}⚠️  SAVE THIS PASSWORD!${NC}"
    fi
    
    gcloud sql instances create $SQL_INSTANCE \
        --database-version=SQLSERVER_2019_STANDARD \
        --tier=db-f1-micro \
        --region=$REGION \
        --root-password=$DB_PASSWORD \
        --storage-type=SSD \
        --storage-size=20GB \
        --project=$PROJECT_ID
    
    echo -e "${GREEN}✅ Cloud SQL instance created${NC}"
    
    # Create database
    echo "Creating database..."
    gcloud sql databases create CryptoPayDb --instance=$SQL_INSTANCE --project=$PROJECT_ID
    echo -e "${GREEN}✅ Database created${NC}"
else
    echo -e "${GREEN}✅ Cloud SQL instance already exists${NC}"
    read -sp "Enter SQL Server root password: " DB_PASSWORD
    echo ""
fi

# Get connection name
CONNECTION_NAME=$(gcloud sql instances describe $SQL_INSTANCE --project=$PROJECT_ID --format="value(connectionName)")
echo -e "${GREEN}Connection name: $CONNECTION_NAME${NC}"

# Step 5: Prepare code
echo ""
echo -e "${BLUE}📁 Step 5: Preparing code...${NC}"

# Check if we're in the right directory
if [ ! -f "CryptoPay.Api.csproj" ]; then
    echo "Current directory: $(pwd)"
    echo "Looking for CryptoPay.Api directory..."
    
    if [ -d "CryptoPay.Api" ]; then
        cd CryptoPay.Api
    elif [ -d "../CryptoPay.Api" ]; then
        cd ../CryptoPay.Api
    else
        echo -e "${YELLOW}⚠️  CryptoPay.Api directory not found in current location${NC}"
        echo "Please ensure you're in the project root or CryptoPay.Api directory"
        echo "Or upload your files to Cloud Shell first"
        exit 1
    fi
fi

echo -e "${GREEN}✅ Code directory found${NC}"

# Step 6: Build and deploy
echo ""
echo -e "${BLUE}🏗️  Step 6: Building and deploying to Cloud Run...${NC}"
echo "This will take 5-10 minutes..."

# Use cloudbuild-online.yaml if it exists, otherwise use cloudbuild.yaml
BUILD_CONFIG="cloudbuild-online.yaml"
if [ ! -f "$BUILD_CONFIG" ]; then
    BUILD_CONFIG="cloudbuild.yaml"
fi

echo "Using build config: $BUILD_CONFIG"

# Submit build
gcloud builds submit --config=$BUILD_CONFIG --project=$PROJECT_ID

echo -e "${GREEN}✅ Build and deployment complete!${NC}"

# Step 7: Configure Cloud Run service
echo ""
echo -e "${BLUE}⚙️  Step 7: Configuring Cloud Run service...${NC}"

# Update service with environment variables and Cloud SQL connection
gcloud run services update cryptopay-api \
    --region=$REGION \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,ConnectionStrings__DefaultConnection=Server=/cloudsql/$CONNECTION_NAME;Database=CryptoPayDb;User Id=sqlserver;Password=$DB_PASSWORD;TrustServerCertificate=True;" \
    --add-cloudsql-instances=$CONNECTION_NAME \
    --project=$PROJECT_ID

echo -e "${GREEN}✅ Service configured${NC}"

# Step 8: Get service URL
echo ""
echo -e "${BLUE}🌐 Step 8: Getting service URL...${NC}"
SERVICE_URL=$(gcloud run services describe cryptopay-api --region=$REGION --project=$PROJECT_ID --format="value(status.url)")

echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ DEPLOYMENT COMPLETE!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Your API URL:${NC} $SERVICE_URL"
echo ""
echo -e "${BLUE}Connection Details:${NC}"
echo "  Connection Name: $CONNECTION_NAME"
echo "  Database: CryptoPayDb"
echo "  Password: $DB_PASSWORD"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Test your API: curl $SERVICE_URL/health"
echo "2. Seed wallet addresses (see instructions below)"
echo "3. Create merchant in database"
echo "4. Update WooCommerce plugin with: $SERVICE_URL"
echo ""
echo -e "${YELLOW}⚠️  IMPORTANT: Save your database password!${NC}"
echo ""

# Step 9: Test the deployment
echo -e "${BLUE}🧪 Step 9: Testing deployment...${NC}"
sleep 5  # Wait for service to be ready

HEALTH_CHECK=$(curl -s "$SERVICE_URL/health" || echo "failed")
if [[ $HEALTH_CHECK == *"healthy"* ]]; then
    echo -e "${GREEN}✅ API is healthy and responding!${NC}"
else
    echo -e "${YELLOW}⚠️  Health check failed. Service may still be starting...${NC}"
    echo "   Try again in a minute: curl $SERVICE_URL/health"
fi

echo ""
echo -e "${GREEN}🎉 All done! Your API is live at: $SERVICE_URL${NC}"

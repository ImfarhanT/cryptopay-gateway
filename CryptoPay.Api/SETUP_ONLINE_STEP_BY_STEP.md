# Complete Online Setup - Step by Step

Follow these steps **entirely in your web browser** - no downloads needed!

## 🎯 Goal
Deploy CryptoPay API to Google Cloud Platform using only web browser.

---

## Step 1: Open Google Cloud Console

1. Go to: https://console.cloud.google.com/
2. Sign in with your Google account
3. Accept terms if prompted

---

## Step 2: Create New Project

1. Click the **project dropdown** at the top (shows current project name)
2. Click **"New Project"**
3. Enter:
   - **Project name**: `cryptopay-backend`
   - **Organization**: (leave default)
   - **Location**: (leave default)
4. Click **"Create"**
5. Wait ~10 seconds, then click **"Select Project"**

---

## Step 3: Enable Billing

1. Click the **hamburger menu** (☰) at top left
2. Go to **"Billing"**
3. Click **"Link a billing account"**
4. Create new billing account or link existing
5. **Note:** New accounts get $300 free credit!

---

## Step 4: Enable Required APIs

1. Click **hamburger menu** (☰) → **"APIs & Services"** → **"Library"**
2. Search for **"Cloud Run API"** → Click it → Click **"Enable"**
3. Search for **"Cloud SQL Admin API"** → Click it → Click **"Enable"**
4. Search for **"Cloud Build API"** → Click it → Click **"Enable"**
5. Search for **"Container Registry API"** → Click it → Click **"Enable"**

Wait for all to enable (takes ~30 seconds each).

---

## Step 5: Create Cloud SQL Database

1. Click **hamburger menu** (☰) → **"SQL"**
2. Click **"Create Instance"**
3. Choose **"SQL Server"** → Click **"Next"**
4. Fill in:
   - **Instance ID**: `cryptopay-db`
   - **Root password**: Create a strong password (save it!)
   - **Confirm password**: Re-enter
   - **Database version**: SQL Server 2019 Standard
   - **Region**: Choose closest (e.g., `us-central1`)
5. Click **"Show configuration options"**
6. Under **"Machine type"**:
   - Select **"db-f1-micro"** (cheapest)
7. Under **"Storage"**:
   - **Storage type**: SSD
   - **Storage capacity**: 20 GB
8. Click **"Create Instance"**
9. ⏳ **Wait 5-10 minutes** for creation

### Create Database

1. Once created, click on **"cryptopay-db"**
2. Click **"Databases"** tab
3. Click **"Create Database"**
4. **Database name**: `CryptoPayDb`
5. Click **"Create"**

### Get Connection Name

1. Still in instance details
2. Find **"Connection name"** (looks like: `PROJECT_ID:us-central1:cryptopay-db`)
3. **Copy this** - you'll need it!

---

## Step 6: Open Cloud Shell

1. Click the **Cloud Shell icon** (terminal icon) at top right
2. ⏳ Wait for Cloud Shell to open (first time takes ~1 minute)
3. You'll see a terminal at the bottom of the page

---

## Step 7: Upload Your Code

### Option A: If Code is on GitHub

```bash
# In Cloud Shell, run:
git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git
cd YOUR_REPO
cd CryptoPay.Api
```

### Option B: Create Files in Cloud Shell

1. Click the **pencil icon** (Open Editor) in Cloud Shell
2. This opens a file editor
3. Create folder structure:
   - Right-click → New Folder → `CryptoPay.Api`
4. Upload your files or create them manually

**Or use the upload feature:**
1. In Cloud Shell Editor, click **File** → **Upload**
2. Select all files from your local `CryptoPay.Api` folder
3. Upload them

---

## Step 8: Build and Deploy

In Cloud Shell terminal, run:

```bash
# Navigate to project
cd CryptoPay.Api

# Set your project
gcloud config set project cryptopay-backend

# Build and deploy (this takes 5-10 minutes)
gcloud builds submit --config cloudbuild-online.yaml
```

This will:
- ✅ Build Docker image
- ✅ Push to Container Registry  
- ✅ Deploy to Cloud Run

---

## Step 9: Configure Cloud Run Service

1. Go to [Cloud Run](https://console.cloud.google.com/run)
2. Click on **"cryptopay-api"** service
3. Click **"Edit & Deploy New Revision"**
4. Scroll to **"Variables & Secrets"** section
5. Click **"Add Variable"**
6. Add these environment variables:

   **Variable 1:**
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`

   **Variable 2:**
   - Name: `ConnectionStrings__DefaultConnection`
   - Value: `Server=/cloudsql/PROJECT_ID:us-central1:cryptopay-db;Database=CryptoPayDb;User Id=sqlserver;Password=YOUR_PASSWORD;TrustServerCertificate=True;`
   
   (Replace `PROJECT_ID` with your actual project ID and `YOUR_PASSWORD` with your SQL password)

7. Scroll to **"Connections"** section
8. Under **"Cloud SQL connections"**, check **"cryptopay-db"**
9. Click **"Deploy"**
10. ⏳ Wait 2-3 minutes for deployment

---

## Step 10: Get Your API URL

1. In Cloud Run, your service page
2. At the top, find **"URL"**
3. It looks like: `https://cryptopay-api-xxxxx-uc.a.run.app`
4. **Copy this URL** - this is your API endpoint!

---

## Step 11: Test Your API

1. Open a new browser tab
2. Go to: `https://YOUR-URL/health`
3. You should see: `{"status":"healthy"}`

✅ **Your API is live!**

---

## Step 12: Seed Wallet Addresses

Use any online API testing tool:

1. Go to: https://web.postman.co/ (or any API tester)
2. Create a **POST** request
3. URL: `https://YOUR-URL/admin/seed-addresses`
4. Headers: `Content-Type: application/json`
5. Body (raw JSON):
```json
{
  "network": "TRC20",
  "count": 3,
  "addresses": [
    "TYourTronAddress1",
    "TYourTronAddress2",
    "TYourTronAddress3"
  ]
}
```
6. Send request

---

## Step 13: Create Merchant

1. Go to [Cloud SQL](https://console.cloud.google.com/sql/instances)
2. Click **"cryptopay-db"**
3. Click **"Open Cloud Shell"** button
4. In Cloud Shell, run:

```bash
# Connect to SQL Server
gcloud sql connect cryptopay-db --user=sqlserver
```

5. Enter your root password when prompted
6. Run this SQL:

```sql
USE CryptoPayDb;
GO

DECLARE @MerchantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(64) = 'test-api-key-' + CONVERT(NVARCHAR(36), NEWID());
DECLARE @WebhookSecret NVARCHAR(64) = 'test-secret-' + CONVERT(NVARCHAR(36), NEWID());
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

SELECT Id AS MerchantId, Name, ApiKey, WebhookSecret FROM Merchants WHERE Id = @MerchantId;
GO
```

7. **Save the output** - you'll need MerchantId, ApiKey, and WebhookSecret!

---

## Step 14: Update WooCommerce Plugin

In your WordPress WooCommerce plugin settings:

- **Backend API Base URL**: `https://YOUR-CLOUD-RUN-URL`
- **Merchant ID**: (from SQL query above)
- **API Key**: (from SQL query above)
- **Webhook Secret**: (from SQL query above)

---

## ✅ You're Done!

Your API is now live on Google Cloud Platform!

### Quick Links:
- **API URL**: Check Cloud Run dashboard
- **Logs**: Cloud Run → Your Service → Logs tab
- **Database**: Cloud SQL dashboard
- **Monitoring**: Cloud Run → Your Service → Metrics tab

### Cost:
- ~$25-30/month (mostly Cloud SQL)
- Free tier available for Cloud Run (2M requests/month)

---

## Need Help?

- Check logs: Cloud Run → Your Service → Logs
- View errors: Cloud Run → Your Service → Metrics
- Database issues: Cloud SQL → Your Instance → Logs

Everything is accessible through the web console! 🎉

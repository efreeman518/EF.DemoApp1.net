#!/bin/bash
set -euo pipefail

# Debugging info
echo "=== Environment Variables ==="
env
echo "============================"

echo "Current working directory: $(pwd)"
echo "Contents of current directory:"
ls -la

# Check required env vars
: "${STORAGE_ACCOUNT:?Environment variable STORAGE_ACCOUNT not set}"
: "${ZIP_NAME:?Environment variable ZIP_NAME not set}"
: "${RESOURCE_GROUP:?Environment variable RESOURCE_GROUP not set}"
: "${APP_NAME:?Environment variable APP_NAME not set}"

echo "📦 Step 1: Downloading artifact from Azure Blob..."
if curl -f -O "https://${STORAGE_ACCOUNT}.blob.core.windows.net/deploy/${ZIP_NAME}"; then
  echo "✅ Artifact downloaded successfully."
else
  echo "❌ Failed to download artifact."
  exit 1
fi

echo
echo "📂 Step 2: Unzipping artifact..."
unzip -q "${ZIP_NAME}" -d app
echo "✅ Unzipped to 'app/' directory."

echo
echo "🚀 Step 3: Deploying to Azure App Service..."
if az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --src-path app \
  --type zip \
  --restart true; then
  echo "✅ Deployment command executed successfully."
else
  echo "❌ Deployment failed."
  exit 1
fi

echo
echo "🔍 Step 4: Fetching App Service details..."
APP_URL=$(az webapp show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "defaultHostName" \
  -o tsv 2>/dev/null || echo "N/A")

echo
echo "=== ✅ Deployment Summary ==="
echo "App Name      : $APP_NAME"
echo "Resource Group: $RESOURCE_GROUP"
echo "Artifact Zip  : $ZIP_NAME"
echo "App URL       : https://${APP_URL}"
echo "============================="
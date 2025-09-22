#!/bin/bash

# Riverty BNPL Azure Functions Deployment Script
# Deploys payment processing functions to Azure with Norwegian market configuration

set -e

# Configuration
RESOURCE_GROUP="riverty-bnpl-prod"
LOCATION="norwayeast"
FUNCTION_APP_NAME="riverty-payment-processor"
STORAGE_ACCOUNT="rivertyfunctionsstorage"
APP_INSIGHTS_NAME="riverty-app-insights"
SERVICE_BUS_NAMESPACE="riverty-servicebus"
KEY_VAULT_NAME="riverty-keyvault"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🇳🇴 Riverty BNPL Azure Functions Deployment${NC}"
echo "=================================================="

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}⚠️  Not logged in to Azure. Please login first.${NC}"
    az login
fi

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK is not installed. Please install .NET 8.0 SDK.${NC}"
    exit 1
fi

echo -e "${BLUE}📋 Current Azure subscription:${NC}"
az account show --query "{subscriptionId:id, name:name, user:user.name}" --output table

read -p "Continue with deployment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}⏹️  Deployment cancelled.${NC}"
    exit 0
fi

echo -e "${BLUE}🏗️  Creating Azure resources...${NC}"

# Create resource group
echo -e "${YELLOW}📦 Creating resource group: $RESOURCE_GROUP${NC}"
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --tags Environment=Production Application=RivertyBNPL Country=Norway

# Create storage account for functions
echo -e "${YELLOW}💾 Creating storage account: $STORAGE_ACCOUNT${NC}"
az storage account create \
    --name $STORAGE_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --kind StorageV2 \
    --access-tier Hot \
    --tags Environment=Production Application=RivertyBNPL

# Create Application Insights
echo -e "${YELLOW}📊 Creating Application Insights: $APP_INSIGHTS_NAME${NC}"
az monitor app-insights component create \
    --app $APP_INSIGHTS_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --kind web \
    --application-type web \
    --tags Environment=Production Application=RivertyBNPL

# Create Service Bus namespace
echo -e "${YELLOW}🚌 Creating Service Bus namespace: $SERVICE_BUS_NAMESPACE${NC}"
az servicebus namespace create \
    --name $SERVICE_BUS_NAMESPACE \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard \
    --tags Environment=Production Application=RivertyBNPL

# Create Service Bus topics
echo -e "${YELLOW}📢 Creating Service Bus topics...${NC}"
TOPICS=("payment-confirmations" "customer-welcome" "payment-plan-updates" "instant-settlements" "settlement-failures" "installment-paid" "installment-retry" "collections-escalation")

for topic in "${TOPICS[@]}"; do
    echo "Creating topic: $topic"
    az servicebus topic create \
        --name $topic \
        --namespace-name $SERVICE_BUS_NAMESPACE \
        --resource-group $RESOURCE_GROUP \
        --max-size 1024 \
        --default-message-time-to-live P14D
done

# Create Key Vault
echo -e "${YELLOW}🔐 Creating Key Vault: $KEY_VAULT_NAME${NC}"
az keyvault create \
    --name $KEY_VAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku standard \
    --enabled-for-deployment true \
    --enabled-for-template-deployment true \
    --tags Environment=Production Application=RivertyBNPL

# Create Function App
echo -e "${YELLOW}⚡ Creating Function App: $FUNCTION_APP_NAME${NC}"
az functionapp create \
    --name $FUNCTION_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --storage-account $STORAGE_ACCOUNT \
    --consumption-plan-location $LOCATION \
    --runtime dotnet-isolated \
    --runtime-version 8 \
    --functions-version 4 \
    --app-insights $APP_INSIGHTS_NAME \
    --tags Environment=Production Application=RivertyBNPL Country=Norway

# Configure Function App settings
echo -e "${YELLOW}⚙️  Configuring Function App settings...${NC}"

# Get connection strings
STORAGE_CONNECTION=$(az storage account show-connection-string --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP --query connectionString --output tsv)
SERVICEBUS_CONNECTION=$(az servicebus namespace authorization-rule keys list --name RootManageSharedAccessKey --namespace-name $SERVICE_BUS_NAMESPACE --resource-group $RESOURCE_GROUP --query primaryConnectionString --output tsv)
APPINSIGHTS_CONNECTION=$(az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP --query connectionString --output tsv)

# Set application settings
az functionapp config appsettings set \
    --name $FUNCTION_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "AzureWebJobsStorage=$STORAGE_CONNECTION" \
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
        "FUNCTIONS_EXTENSION_VERSION=~4" \
        "AZURE_FUNCTIONS_ENVIRONMENT=Production" \
        "ServiceBusConnection=$SERVICEBUS_CONNECTION" \
        "ApplicationInsights__ConnectionString=$APPINSIGHTS_CONNECTION" \
        "AZURE_KEYVAULT_URI=https://$KEY_VAULT_NAME.vault.azure.net/" \
        "PaymentProcessor__InstallmentBatchSize=100" \
        "PaymentProcessor__BatchDelayMilliseconds=500" \
        "PaymentProcessor__MaxRetryAttempts=3" \
        "PaymentProcessor__RetryDelayHours=4" \
        "PaymentProcessor__OverdueDays=7" \
        "PaymentProcessor__CollectionEscalationDays=30" \
        "PaymentProcessor__OverdueAmountAlertThreshold=100000" \
        "Notifications__OverdueNoticeAlertThreshold=50" \
        "Settlements__FailureAlertThreshold=5" \
        "Settlements__VarianceAlertThreshold=1000" \
        "Settlements__InstantSettlementEnabled=true"

# Enable system-assigned managed identity
echo -e "${YELLOW}🔑 Enabling managed identity...${NC}"
az functionapp identity assign \
    --name $FUNCTION_APP_NAME \
    --resource-group $RESOURCE_GROUP

# Get the managed identity principal ID
PRINCIPAL_ID=$(az functionapp identity show --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP --query principalId --output tsv)

# Grant Key Vault access to the managed identity
echo -e "${YELLOW}🔐 Granting Key Vault access...${NC}"
az keyvault set-policy \
    --name $KEY_VAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --object-id $PRINCIPAL_ID \
    --secret-permissions get list

# Build and deploy the function
echo -e "${BLUE}🔨 Building and deploying function...${NC}"

# Navigate to function directory
cd "$(dirname "$0")/../src/Functions/PaymentProcessor"

# Restore packages
echo -e "${YELLOW}📦 Restoring NuGet packages...${NC}"
dotnet restore

# Build the project
echo -e "${YELLOW}🔨 Building project...${NC}"
dotnet build --configuration Release --no-restore

# Publish the project
echo -e "${YELLOW}📦 Publishing project...${NC}"
dotnet publish --configuration Release --no-build --output ./publish

# Deploy to Azure
echo -e "${YELLOW}🚀 Deploying to Azure...${NC}"
cd publish
zip -r ../deploy.zip .
cd ..

az functionapp deployment source config-zip \
    --name $FUNCTION_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --src deploy.zip

# Clean up
rm -f deploy.zip
rm -rf publish

echo -e "${GREEN}✅ Deployment completed successfully!${NC}"
echo ""
echo -e "${BLUE}📋 Deployment Summary:${NC}"
echo "=================================================="
echo -e "Resource Group: ${YELLOW}$RESOURCE_GROUP${NC}"
echo -e "Function App: ${YELLOW}$FUNCTION_APP_NAME${NC}"
echo -e "Location: ${YELLOW}$LOCATION${NC}"
echo -e "Storage Account: ${YELLOW}$STORAGE_ACCOUNT${NC}"
echo -e "Service Bus: ${YELLOW}$SERVICE_BUS_NAMESPACE${NC}"
echo -e "Key Vault: ${YELLOW}$KEY_VAULT_NAME${NC}"
echo -e "App Insights: ${YELLOW}$APP_INSIGHTS_NAME${NC}"
echo ""
echo -e "${BLUE}🔗 Useful URLs:${NC}"
echo -e "Function App: ${YELLOW}https://$FUNCTION_APP_NAME.azurewebsites.net${NC}"
echo -e "Azure Portal: ${YELLOW}https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id --output tsv)/resourceGroups/$RESOURCE_GROUP/overview${NC}"
echo ""
echo -e "${BLUE}🛠️  Next Steps:${NC}"
echo "1. Configure Norwegian banking API credentials in Key Vault"
echo "2. Set up database connection string"
echo "3. Configure payment gateway credentials"
echo "4. Set up monitoring alerts"
echo "5. Test the deployed functions"
echo ""
echo -e "${GREEN}🇳🇴 Norwegian BNPL Functions are now running in Azure!${NC}"

# Test the deployment
echo -e "${BLUE}🧪 Testing deployment...${NC}"
FUNCTION_URL="https://$FUNCTION_APP_NAME.azurewebsites.net/api/installments/health"

echo -e "${YELLOW}Testing health endpoint: $FUNCTION_URL${NC}"
sleep 30 # Wait for function to warm up

if curl -f -s "$FUNCTION_URL" > /dev/null; then
    echo -e "${GREEN}✅ Health check passed!${NC}"
else
    echo -e "${YELLOW}⚠️  Health check failed. Function may still be starting up.${NC}"
    echo -e "${YELLOW}   Check the Azure portal for logs and status.${NC}"
fi

echo -e "${GREEN}🎉 Deployment script completed!${NC}"
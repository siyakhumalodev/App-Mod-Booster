#!/bin/bash
set -e

echo "================================================"
echo "Expense Management System Deployment with GenAI"
echo "================================================"

# Get current user info
CURRENT_USER=$(az account show --query user.name -o tsv)
CURRENT_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo "Deploying as: $CURRENT_USER"
echo "Object ID: $CURRENT_OBJECT_ID"

# Set resource group name
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Create resource group if it doesn't exist
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure with GenAI
echo "Deploying infrastructure with GenAI resources..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId=$CURRENT_OBJECT_ID adminLogin=$CURRENT_USER deployGenAI=true \
    --query properties.outputs \
    --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo ""
echo "Deployment completed successfully!"
echo "App Service: $APP_SERVICE_NAME"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Database: $DATABASE_NAME"
echo "Managed Identity: $MANAGED_IDENTITY_NAME"
echo "OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "OpenAI Model: $OPENAI_MODEL_NAME"
echo "Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Wait for SQL Server to be fully ready
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Install required Python packages if not already installed
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server name
echo "Updating Python scripts with server details..."
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo "Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo "Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Configure App Service settings
echo "Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:$SQL_SERVER_FQDN;Database=$DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --connection-string-type SQLAzure \
    --settings DefaultConnection="$CONNECTION_STRING" \
    --output none

# Configure OpenAI and Search settings
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --settings \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
        "Search__Endpoint=$SEARCH_ENDPOINT" \
    --output none

# Deploy application code
echo "Deploying application code..."
if [ -f "app.zip" ]; then
    az webapp deploy \
        --resource-group $RESOURCE_GROUP \
        --name $APP_SERVICE_NAME \
        --src-path ./app.zip \
        --type zip \
        --output none
    
    echo ""
    echo "====================================="
    echo "Deployment Complete!"
    echo "====================================="
    echo "Application URL: ${APP_SERVICE_URL}/Index"
    echo "Chat UI URL: ${APP_SERVICE_URL}/Chat"
    echo ""
    echo "Note: Navigate to ${APP_SERVICE_URL}/Index to view the application"
    echo "      Navigate to ${APP_SERVICE_URL}/Chat to use the AI assistant"
    echo ""
else
    echo ""
    echo "WARNING: app.zip not found. Please build and deploy the application code separately."
    echo ""
    echo "To build the application:"
    echo "  cd app"
    echo "  dotnet publish -c Release -o ../publish"
    echo "  cd ../publish"
    echo "  zip -r ../app.zip ."
    echo "  cd .."
    echo ""
    echo "Then run this script again to deploy."
    echo ""
fi

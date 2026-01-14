# Deployment Guide

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Appropriate Azure subscription permissions
- Python 3.x with pip
- .NET 8 SDK (for local development)
- ODBC Driver 18 for SQL Server (for database scripts)

## Quick Start

### Option 1: Deploy App + Database Only

```bash
bash deploy.sh
```

This deploys:
- Azure App Service (S1 tier)
- Azure SQL Database (Basic tier)
- User-Assigned Managed Identity
- Database schema and stored procedures

**Time estimate**: 5-10 minutes

**Access the app**: Navigate to the URL shown at the end + `/Index`  
Example: `https://app-expensemgmt-xxxxx.azurewebsites.net/Index`

### Option 2: Deploy App + Database + AI Chat

```bash
bash deploy-with-chat.sh
```

This deploys everything from Option 1 plus:
- Azure OpenAI with GPT-4o model
- Azure AI Search (Basic tier)
- AI-powered chat functionality

**Time estimate**: 10-15 minutes

**Access the app**: 
- Main app: URL shown + `/Index`
- AI Chat: URL shown + `/Chat`
- API Docs: URL shown + `/swagger`

## What Gets Deployed

### Azure Resources Created

1. **Resource Group**: `rg-expensemgmt-demo`
2. **App Service**: `app-expensemgmt-{uniqueid}`
   - Standard S1 SKU (always on, no cold start)
   - .NET 8 runtime
   - Linux
3. **App Service Plan**: `plan-expensemgmt-{uniqueid}`
4. **Managed Identity**: `mid-appmodassist-{uniqueid}`
5. **SQL Server**: `sql-expensemgmt-{uniqueid}`
   - Entra ID (Azure AD) authentication only
   - No SQL authentication
6. **SQL Database**: `ExpenseManagement`
   - Basic tier (2GB)

#### Optional (with deploy-with-chat.sh):
7. **Azure OpenAI**: `aoai-expensemgmt-{uniqueid}`
   - Location: swedencentral
   - Model: GPT-4o (2024-08-06)
   - S0 tier, capacity 8
8. **AI Search**: `search-expensemgmt-{uniqueid}`
   - Basic tier
   - Location: uksouth

### Database Schema

The deployment automatically creates:
- **Tables**: Expenses, ExpenseStatus, ExpenseCategories, Users, Roles
- **Stored Procedures**: All CRUD operations
- **Sample Data**: Demo users and expenses
- **Managed Identity Permissions**: db_datareader, db_datawriter, EXECUTE

## Features

### Web Application
- **Add Expenses**: Create new expense entries with amount, date, category, and description
- **View Expenses**: Browse all expenses with filtering by text and status
- **Approve Expenses**: Managers can review and approve/reject submitted expenses
- **Modern UI**: Clean, responsive interface with Bootstrap 5

### REST API
- Full CRUD operations for expenses
- Category management
- User management
- Status filtering
- Swagger documentation at `/swagger`

### AI Chat Assistant (Optional)
- Natural language expense queries
- Automatic expense creation via chat
- Expense approval/rejection
- Context-aware responses
- Function calling for database operations

## Application URLs

After deployment, you can access:

1. **Main Application**: `{APP_URL}/Index`
   - Expense management interface
   - Add, view, and manage expenses

2. **API Documentation**: `{APP_URL}/swagger`
   - Interactive API explorer
   - Test endpoints directly

3. **AI Chat**: `{APP_URL}/Chat`
   - AI-powered expense assistant
   - Only available if deployed with deploy-with-chat.sh

## Local Development

### Running Locally

1. Update connection string in `app/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:YOUR-SERVER.database.windows.net;Database=ExpenseManagement;Authentication=Active Directory Default;"
  }
}
```

2. Login to Azure CLI:
```bash
az login
```

3. Run the application:
```bash
cd app
dotnet run
```

4. Access at: `http://localhost:5000/Index`

### Building the Deployment Package

```bash
cd app
dotnet publish -c Release -o ../publish
cd ../publish
zip -r ../app.zip .
cd ..
```

## Troubleshooting

### Database Connection Issues

**Error**: "Cannot connect to database"

**Solution**: 
1. Ensure firewall rules are configured (the script does this automatically)
2. Check managed identity has proper database permissions
3. Verify the connection string includes the managed identity client ID

### AI Chat Not Working

**Error**: "GenAI services are not deployed"

**Solution**: Run `bash deploy-with-chat.sh` instead of `deploy.sh`

### Deployment Fails

**Common causes**:
1. Not logged into Azure CLI - run `az login`
2. Insufficient permissions - ensure you have Contributor role
3. Region quota limits - try a different region or request quota increase

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagrams and component descriptions.

## Security

### Authentication & Authorization
- ✅ Azure AD-only authentication for SQL Database
- ✅ Managed Identity for all Azure service connections
- ✅ No passwords or connection strings with credentials
- ✅ HTTPS only (enforced)

### Data Access
- ✅ All database operations via stored procedures
- ✅ No direct table access from application code
- ✅ Parameterized queries prevent SQL injection
- ✅ Least privilege access (db_datareader, db_datawriter, EXECUTE only)

### Network Security
- ✅ Firewall rules limit database access
- ✅ Azure service-to-service communication via managed identity
- ✅ No public IPs exposed for internal services

## Cost Estimate

### Basic Deployment (deploy.sh)
- App Service S1: ~£50/month
- SQL Database Basic: ~£4/month
- **Total**: ~£54/month

### Full Deployment with AI (deploy-with-chat.sh)
- App Service S1: ~£50/month
- SQL Database Basic: ~£4/month
- Azure OpenAI S0: ~£15/month (based on usage)
- AI Search Basic: ~£60/month
- **Total**: ~£129/month

*Prices are estimates and may vary by region*

## Clean Up

To delete all resources:

```bash
az group delete --name rg-expensemgmt-demo --yes --no-wait
```

## Support

For issues or questions:
1. Check [ARCHITECTURE.md](ARCHITECTURE.md) for system design
2. Review deployment logs for specific error messages
3. Ensure all prerequisites are met
4. Check Azure Portal for resource status

## Next Steps

After deployment:
1. Access the application at the URL shown
2. Try adding some expenses
3. Test the approval workflow
4. Explore the API at `/swagger`
5. If GenAI is deployed, try the AI chat at `/Chat`

Example questions for AI chat:
- "Show me all my expenses"
- "Create an expense for £45 for travel on 2026-01-15"
- "What expenses are pending approval?"
- "Show me all approved expenses"

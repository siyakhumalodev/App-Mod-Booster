![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## 🚀 Quick Start

### Deploy the Expense Management System

This repository contains a complete, modernized Expense Management System ready to deploy to Azure.

**Basic Deployment (App + Database)**:
```bash
az login
bash deploy.sh
```

**Full Deployment with AI Chat**:
```bash
az login
bash deploy-with-chat.sh
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

## 📋 What's Included

- **Modern Web Application**: ASP.NET Core 8 Razor Pages with responsive Bootstrap UI
- **REST APIs**: Full CRUD operations with Swagger documentation
- **AI Chat Assistant**: GPT-4o powered expense management (optional)
- **Azure SQL Database**: Secure, serverless database with Entra ID authentication
- **Infrastructure as Code**: Complete Bicep templates for one-click deployment
- **Security First**: Managed Identity, no secrets, Azure AD-only authentication

## 📚 Documentation

- [DEPLOYMENT.md](DEPLOYMENT.md) - Complete deployment guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and design

## 🎯 Features

### Expense Management
- ✅ Add new expenses with amount, date, category, and description
- ✅ View and filter expenses by status (Draft, Submitted, Approved, Rejected)
- ✅ Submit expenses for manager approval
- ✅ Approve or reject pending expenses
- ✅ Modern, responsive UI

### REST API
- ✅ Full CRUD operations for expenses
- ✅ Category and user management
- ✅ Status filtering and search
- ✅ Interactive Swagger documentation

### AI Chat (Optional)
- ✅ Natural language expense queries
- ✅ Create expenses via conversation
- ✅ Approve/reject expenses through chat
- ✅ Function calling for real database operations

## 🛠️ Technology Stack

- **Frontend**: ASP.NET Core Razor Pages, Bootstrap 5, jQuery
- **Backend**: .NET 8, C#
- **Database**: Azure SQL Database
- **AI**: Azure OpenAI (GPT-4o), Azure AI Search
- **Infrastructure**: Azure App Service, Bicep
- **Authentication**: Azure Managed Identity, Entra ID

## 📖 How to Use This Template

### Steps to modernise your own app:

1. **Fork this repo**
2. **Replace the sample data**:
   - Put your legacy app screenshots in `Legacy-Screenshots/`
   - Update `Database-Schema/database_schema.sql` with your schema
3. **Run the coding agent**:
   - Open GitHub Copilot
   - Use the app-mod-booster agent
   - Tell it "modernise my app"
4. **Review and merge** the generated pull request (takes ~30 minutes)
5. **Deploy to Azure**:
   ```bash
   az login
   bash deploy.sh
   ```

## 💰 Cost Estimate

- **Basic**: ~£54/month (App Service + SQL Database)
- **With AI**: ~£129/month (includes Azure OpenAI + AI Search)

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed pricing.

## 🔒 Security

- Azure AD-only authentication (no SQL credentials)
- Managed Identity for all Azure connections
- Stored procedures for all database operations
- HTTPS enforced
- No secrets in code or configuration

## 🎓 Learning Resources

Supporting slides for Microsoft Employees:
[Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

## 📝 Sample Application

This template includes a complete Expense Management System as a working example:

- **Database**: Northwind-style expense tracking
- **UI**: Modern expense entry and approval workflows
- **APIs**: RESTful endpoints for all operations
- **Screenshots**: Legacy UI mockups in `Legacy-Screenshots/`

## 🧹 Clean Up

To remove all deployed resources:

```bash
az group delete --name rg-expensemgmt-demo --yes
```

## 📄 License

See LICENSE file for details.

## 🤝 Contributing

See [Guiding-Principles](Guiding-Principles) for contribution guidelines.

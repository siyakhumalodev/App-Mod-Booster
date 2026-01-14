# Azure Services Architecture

## Expense Management System - Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Internet / User                              │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               │ HTTPS
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     Azure App Service (S1)                            │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  .NET 8 Web Application                                       │   │
│  │  - Razor Pages UI                                             │   │
│  │  - REST APIs (Swagger)                                        │   │
│  │  - AI Chat Interface                                          │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  Identity: User-Assigned Managed Identity (mid-appmodassist-xxxxx)  │
└──────┬────────────────────┬─────────────────┬────────────────────────┘
       │                    │                 │
       │ Managed            │ Managed         │ Managed
       │ Identity           │ Identity        │ Identity
       │ Auth               │ Auth            │ Auth
       ▼                    ▼                 ▼
┌─────────────────┐  ┌──────────────────┐  ┌─────────────────────────┐
│  Azure SQL DB   │  │ Azure OpenAI     │  │ Azure AI Search         │
│                 │  │                  │  │                         │
│  - Northwind    │  │  - GPT-4o        │  │  - Basic Tier           │
│  - Basic Tier   │  │  - S0 Tier       │  │  - Document Search      │
│  - Entra ID     │  │  - swedencentral │  │  - uksouth              │
│    Only Auth    │  │  - Capacity: 8   │  │                         │
└─────────────────┘  └──────────────────┘  └─────────────────────────┘
       │
       │ Stored Procedures
       │ Read/Write
       ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     Database Schema                                   │
│  - Expenses          - Users              - Categories               │
│  - ExpenseStatus     - Roles                                         │
└──────────────────────────────────────────────────────────────────────┘
```

## Component Descriptions

### App Service
- **SKU**: Standard S1 (no cold start)
- **Runtime**: .NET 8 (LTS)
- **Features**:
  - Web UI for expense management
  - REST APIs with Swagger documentation
  - AI-powered chat assistant
- **Authentication**: User-assigned Managed Identity for Azure service connections

### Azure SQL Database
- **Tier**: Basic (development)
- **Database**: ExpenseManagement
- **Authentication**: Entra ID (Azure AD) only - no SQL auth
- **Access**: Managed Identity with db_datareader, db_datawriter, and EXECUTE permissions
- **Data Access**: All operations via stored procedures (no direct table access from app)

### Azure OpenAI (Optional - deployed with deploy-with-chat.sh)
- **Model**: GPT-4o (2024-08-06)
- **Location**: swedencentral (to avoid quota issues)
- **SKU**: S0
- **Capacity**: 8 units
- **Features**:
  - Function calling for database operations
  - Natural language expense management
  - RAG (Retrieval-Augmented Generation) support

### Azure AI Search (Optional - deployed with deploy-with-chat.sh)
- **Tier**: Basic
- **Location**: uksouth
- **Purpose**: Document search and RAG for chat functionality

### Managed Identity
- **Type**: User-assigned
- **Name**: mid-appmodassist-{uniquestring}
- **Permissions**:
  - SQL Database: db_datareader, db_datawriter, EXECUTE
  - Azure OpenAI: Cognitive Services OpenAI User
  - AI Search: Search Index Data Contributor

## Data Flow

### Standard Web Operations
1. User accesses App Service URL
2. App Service authenticates to SQL using Managed Identity
3. App calls stored procedures to read/write data
4. Results returned to user via web UI or API

### AI Chat Operations
1. User sends message to chat interface
2. Chat service uses Managed Identity to authenticate to Azure OpenAI
3. AI model processes request and may invoke function tools
4. Function tools call stored procedures via Managed Identity
5. Results aggregated and returned as natural language response
6. AI Search provides RAG context when needed

## Security Features

- ✅ No SQL authentication - Entra ID only
- ✅ Managed Identity for all Azure service connections
- ✅ No secrets in code or configuration
- ✅ HTTPS only
- ✅ Stored procedures prevent SQL injection
- ✅ Minimal privilege access (least privilege principle)

## Deployment Options

### Option 1: Basic Deployment (App + Database)
```bash
bash deploy.sh
```
Deploys:
- App Service
- Azure SQL Database
- Managed Identity

### Option 2: Full Deployment with AI (App + Database + GenAI)
```bash
bash deploy-with-chat.sh
```
Deploys everything from Option 1 plus:
- Azure OpenAI with GPT-4o
- Azure AI Search
- AI Chat functionality

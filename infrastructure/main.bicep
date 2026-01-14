// Main Bicep template for Expense Management System
param location string = 'uksouth'
param deployGenAI bool = false
param adminObjectId string
param adminLogin string

var uniqueSuffix = uniqueString(resourceGroup().id)
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var databaseName = 'ExpenseManagement'

// Deploy App Service with Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
  }
}

// Deploy Azure SQL Database
module sqlDatabase 'azure-sql.bicep' = {
  name: 'sqlDatabaseDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: databaseName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityClientId: appService.outputs.managedIdentityClientId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityName string = appService.outputs.managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn
output databaseName string = sqlDatabase.outputs.databaseName
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''

// GenAI Resources - Azure OpenAI and AI Search
param location string
param managedIdentityPrincipalId string
param managedIdentityClientId string

var uniqueSuffix = uniqueString(resourceGroup().id)
var openAIName = 'aoai-expensemgmt-${uniqueSuffix}'
var searchName = 'search-expensemgmt-${uniqueSuffix}'
var deploymentName = 'gpt-4o'

// Create Azure OpenAI resource in swedencentral
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: 'swedencentral'
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower(openAIName)
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: deploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Create AI Search resource
resource search 'Microsoft.Search/searchServices@2021-04-01-preview' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Assign Cognitive Services OpenAI User role to managed identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'CognitiveServicesOpenAIUser')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Assign Search Index Data Contributor role to managed identity
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, managedIdentityPrincipalId, 'SearchIndexDataContributor')
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = deploymentName
output openAIName string = openAI.name
output searchEndpoint string = 'https://${search.name}.search.windows.net'
output searchName string = search.name

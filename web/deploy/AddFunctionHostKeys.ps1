function Get-AzureRmCachedAccessToken()
{
  $ErrorActionPreference = 'Stop'
  
  if(-not (Get-Module AzureRm.Profile)) {
    Import-Module AzureRm.Profile
  }
  $azureRmProfileModuleVersion = (Get-Module AzureRm.Profile).Version
  # refactoring performed in AzureRm.Profile v3.0 or later
  if($azureRmProfileModuleVersion.Major -ge 3) {
    $azureRmProfile = [Microsoft.Azure.Commands.Common.Authentication.Abstractions.AzureRmProfileProvider]::Instance.Profile
    if(-not $azureRmProfile.Accounts.Count) {
      Write-Error "Ensure you have logged in before calling this function."    
    }
  } else {
    # AzureRm.Profile < v3.0
    $azureRmProfile = [Microsoft.WindowsAzure.Commands.Common.AzureRmProfileProvider]::Instance.Profile
    if(-not $azureRmProfile.Context.Account.Count) {
      Write-Error "Ensure you have logged in before calling this function."    
    }
  }
  
  $currentAzureContext = Get-AzureRmContext
  $profileClient = New-Object Microsoft.Azure.Commands.ResourceManager.Common.RMProfileClient($azureRmProfile)
  Write-Debug ("Getting access token for tenant" + $currentAzureContext.Subscription.TenantId)
  $token = $profileClient.AcquireAccessToken($currentAzureContext.Subscription.TenantId)
  $token.AccessToken
}

function Get-AzureFunctionHostNameAndKey([string]$SubscriptionId, [string]$ResourceGroupName, [string]$FunctionAppName, [string]$AccessToken)
{
    Write-Debug 'Retrieving Kudu publishing profile...'
    $publishData = Invoke-RestMethod -Uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$FunctionAppName/publishxml?api-version=2016-08-01" -Method Post -Headers @{"Authorization"="Bearer $AccessToken"}
    $kuduUsername = $publishData.publishData.publishProfile[0].userName
    $kuduPassword = $publishData.publishData.publishProfile[0].userPWD
    $kuduAuthHeader = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($kuduUsername):$($kuduPassword)"))
    
    # Get the function app's host key from Kudu
    $apiBaseUrl = "https://$FunctionAppName.scm.azurewebsites.net/api"
    $siteBaseUrl = "https://$FunctionAppName.azurewebsites.net"
    
    Write-Debug 'Retrieving JWT for Kudu...'
    $kuduJwt = Invoke-RestMethod -Uri "$apiBaseUrl/functions/admin/token" -Headers @{ Authorization = "Basic $kuduAuthHeader" } -Method GET
    
    Write-Debug 'Retrieving function host key...'
    $hostKeys = Invoke-RestMethod -Uri "$siteBaseUrl/admin/host/keys" -Headers @{ Authorization = "Bearer $kuduJwt" } -Method GET
    $hostKey = $hostKeys.keys[0].value

    $hostKey
    $siteBaseUrl
}

$accessToken = Get-AzureRmCachedAccessToken
$subscriptionId = (Get-AzureRmContext).Subscription.Id

$functionsApps = @(
    @{ Name = 'crcatfunc'; ApiUrlVariableName = 'CategoriesMicroserviceApiKey'; ApiKeyVariableName = 'CategoriesMicroserviceApiUrl' },
    @{ Name = 'crimgfunc'; ApiUrlVariableName = 'ImagesMicroserviceApiKey'; ApiKeyVariableName = 'ImagesMicroserviceApiUrl' },
    @{ Name = 'crtxtfunc'; ApiUrlVariableName = 'TextMicroserviceApiKey'; ApiKeyVariableName = 'TextMicroserviceApiUrl' }
)

foreach ($functionApp in $functionsApps)
{
    $functionsDetails = Get-AzureFunctionHostNameAndKey -SubscriptionId $subscriptionId -ResourceGroupName ContentReactor -FunctionAppName $functionApp.Name -AccessToken $accessToken
    $functionsHostKey = $functionsDetails[0]
    $functionsUrl = $functionsDetails[1]

    Write-Host "##vso[task.setvariable variable=$($functionApp.ApiUrlVariableName)]$functionsHostKey"
    Write-Host "##vso[task.setvariable variable=$($functionApp.ApiKeyVariableName)]$functionsUrl"
}
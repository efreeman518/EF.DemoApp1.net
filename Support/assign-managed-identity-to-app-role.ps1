# https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-assign-app-role-managed-identity?pivots=identity-mi-app-role-powershell#complete-example-script
# Install the module.
# Install-Module Microsoft.Graph -Scope AllUsers/CurrentUser -Force -AllowClobber
# Install-Module -Name Az -Scope AllUsers/CurrentUser -Force -AllowClobber

# $tenantID - ID (in the Azure portal, under Azure Active Directory > Overview).
# $webAppName - The name of your web app, which has a managed identity that should be assigned to the server app's app role.
# $serverAppRegName - The name of the EntrID app registration that exposes the app role.
# $appRoleName - The name of the app role that the managed identity should be assigned to.

# Login with MFA
# Connect-AzAccount -TenantId "fde76964-9431-4b5f-877e-4108da4a9c23" -UseDeviceAuthentication
# cd to the script folder
# ./assign-managed-identity-to-app-role.ps1 -tenantId "fde76964-9431-4b5f-877e-4108da4a9c23" -webAppName "gateway-dev1" -resourceGroupName "rg-dev1" -serverAppRegName "Secure-sampleapp-api-dev1" -appRoleName "StandardAccess"

param (
    [string]$tenantId,
    [string]$webAppName,
    [string]$resourceGroupName,
    [string]$serverAppRegName,
    [string]$appRoleName
)

# Check if the required parameters are provided
if (-not $tenantId -or -not $webAppName -or -not $resourceGroupName -or -not $serverAppRegName -or -not $appRoleName) {
    Write-Host "Missing required parameters. Please provide all parameters."
    exit 1
}

# Now, you can use these parameters in your script
Write-Host "Tenant ID: $tenantId"
Write-Host "Web App Name: $webAppName"
Write-Host "Resource Group Name: $resourceGroupName"
Write-Host "Server Application Name: $serverAppRegName"
Write-Host "App Role Name: $appRoleName"

# Look up the web app's managed identity's object ID.
$managedIdentityObjectId = (Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $webAppName).identity.principalid

Connect-MgGraph -TenantId $tenantId -Scopes 'Application.Read.All','Application.ReadWrite.All','AppRoleAssignment.ReadWrite.All','Directory.AccessAsUser.All','Directory.Read.All','Directory.ReadWrite.All'

# Look up the details about the server app's service principal and app role.
$serverServicePrincipal = (Get-MgServicePrincipal -Filter "DisplayName eq '$serverAppRegName'")
$serverServicePrincipalObjectId = $serverServicePrincipal.Id
$appRoleId = ($serverServicePrincipal.AppRoles | Where-Object {$_.Value -eq $appRoleName }).Id

# Assign the managed identity access to the app role.
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $serverServicePrincipalObjectId -PrincipalId $managedIdentityObjectId -ResourceId $serverServicePrincipalObjectId -AppRoleId $appRoleId
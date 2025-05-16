# https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-assign-app-role-managed-identity?pivots=identity-mi-app-role-powershell#complete-example-script
# Install the module.
# Install-Module Microsoft.Graph -Scope AllUsers/CurrentUser -Force -AllowClobber
# Install-Module -Name Az -Scope AllUsers/CurrentUser -Force -AllowClobber
# Install-Module -Name Az.App -Force #for ACA

# $tenantID - ID (in the Azure portal, under Azure Active Directory > Overview).
# $webAppName - The name of your web app/container app, which has a managed identity that should be assigned to the server app's app role.
# $resourceGroupName - The name of the resource group that contains the web app/container app.
# $serverAppRegName - The name of the EntrID app registration that exposes the app role.
# $appRoleName - The name of the app role that the managed identity should be assigned to.
# $resourceType - The type of resource. Either "WebApp" or "ContainerApp".
# $managedIdentityObjectIdOverride - Optional. If you know the managed identity's object ID, you can provide it here to skip the lookup.

# Login with MFA
# Connect-AzAccount -TenantId "fde76964-9431-4b5f-877e-4108da4a9c23" -UseDeviceAuthentication
# cd to the script folder
# ./assign-managed-identity-to-app-role.ps1 -tenantId "fde76964-9431-4b5f-877e-4108da4a9c23" -webAppName "gateway-dev1" -resourceGroupName "rg-dev1" -serverAppRegName "Secure-sampleapp-api-dev1" -appRoleName "StandardAccess" -resourceType "WebApp"
# ./assign-managed-identity-to-app-role.ps1 -tenantId "fde76964-9431-4b5f-877e-4108da4a9c23" -webAppName "aca-gateway-dev1" -resourceGroupName "rg-dev1" -serverAppRegName "Secure-sampleapp-api-dev1" -appRoleName "StandardAccess" -resourceType "ContainerApp"
param (
    [string]$tenantId,
    [string]$webAppName,
    [string]$resourceGroupName,
    [string]$serverAppRegName,
    [string]$appRoleName,
    [string]$resourceType = "WebApp", # "WebApp" or "ContainerApp"
    [string]$managedIdentityObjectIdOverride = "" # Optional direct override
)

# Check if the required parameters are provided
if (-not $tenantId -or -not $webAppName -or -not $resourceGroupName -or -not $serverAppRegName -or -not $appRoleName) {
    Write-Host "Missing required parameters. Please provide all parameters."
    exit 1
}

# Now, you can use these parameters in your script
Write-Host "Tenant ID: $tenantId"
Write-Host "App Name: $webAppName"
Write-Host "Resource Group Name: $resourceGroupName"
Write-Host "Server Application Name: $serverAppRegName"
Write-Host "App Role Name: $appRoleName"
Write-Host "Resource Type: $resourceType"

# Look up the web app's managed identity's object ID.
if (-not [string]::IsNullOrEmpty($managedIdentityObjectIdOverride)) {
    $managedIdentityObjectId = $managedIdentityObjectIdOverride
    Write-Host "Using provided managed identity object ID."
}
elseif ($resourceType -eq "ContainerApp") {
    try {
        $containerApp = Get-AzContainerApp -ResourceGroupName $resourceGroupName -Name $webAppName
        # Extract the principalId from the identity property
        # Container App returns a different format than Web App
        if ($containerApp.Identity.PrincipalId) {
            $managedIdentityObjectId = $containerApp.Identity.PrincipalId
        }
        elseif ($containerApp.Identity) {
            # Try to convert the identity property to a string and extract the GUID
            $identityString = $containerApp.Identity.ToString()
            if ($identityString -match '([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})') {
                $managedIdentityObjectId = $matches[1]
            }
        }
    }
    catch {
        Write-Host "Error getting Container App identity: $_"
    }
}
else {
    $managedIdentityObjectId = (Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $webAppName).identity.principalid
}

Write-Host "Managed Identity Object ID: $managedIdentityObjectId"

# Make sure we actually got an ID
if ([string]::IsNullOrEmpty($managedIdentityObjectId)) {
    Write-Host "Failed to get managed identity object ID. Ensure that system-assigned managed identity is enabled on your Container App, this crap normallyu doesn't work, so grab it manually and use the override param.'"
    exit 1
}

Write-Host "Managed Identity Object ID: $managedIdentityObjectId"

Connect-MgGraph -TenantId $tenantId -Scopes 'Application.Read.All','Application.ReadWrite.All','AppRoleAssignment.ReadWrite.All','Directory.AccessAsUser.All','Directory.Read.All','Directory.ReadWrite.All'

# Look up the details about the server app's service principal and app role.
$serverServicePrincipal = (Get-MgServicePrincipal -Filter "DisplayName eq '$serverAppRegName'")
$serverServicePrincipalObjectId = $serverServicePrincipal.Id
$appRoleId = ($serverServicePrincipal.AppRoles | Where-Object {$_.Value -eq $appRoleName }).Id

# Assign the managed identity access to the app role.
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $serverServicePrincipalObjectId -PrincipalId $managedIdentityObjectId -ResourceId $serverServicePrincipalObjectId -AppRoleId $appRoleId
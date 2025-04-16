$clientAppId = "96aa4138-7f97-4502-914b-67a0e8e447b9"

$federatedCredential = @{
    name        = "github-wildcard"
    issuer      = "https://token.actions.githubusercontent.com"
    subject     = "repo:efreeman518/EF.DemoApp1.net:*"
    description = "Wildcard for all branches, tags, environments"
    audiences   = @("api://AzureADTokenExchange")
}

# Convert to JSON and write to a temporary file
$jsonFile = [System.IO.Path]::GetTempFileName()
$federatedCredential | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonFile -Encoding utf8

Write-Host "Creating federated credential from $jsonFile"
az ad app federated-credential create --id $clientAppId --parameters @$jsonFile

# Optional cleanup
Remove-Item $jsonFile

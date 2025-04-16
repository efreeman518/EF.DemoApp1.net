$clientAppId = "96aa4138-7f97-4502-914b-67a0e8e447b9"

$federatedCredentials = @(
    @{
        name        = "github-any-ref"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:*"
        description = "Wildcard for any branch, tag, or environment"
        audiences   = @("api://AzureADTokenExchange")
    },
    @{
        name        = "github-any-branch"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:ref:refs/heads/*"
        description = "Covers all branches"
        audiences   = @("api://AzureADTokenExchange")
    },
    @{
        name        = "github-any-tag"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:ref:refs/tags/*"
        description = "Covers all tags"
        audiences   = @("api://AzureADTokenExchange")
    },
    @{
        name        = "github-any-environment"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:environment:*"
        description = "Covers all GitHub Actions environments"
        audiences   = @("api://AzureADTokenExchange")
    },
    @{
        name        = "github-main-branch"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:ref:refs/heads/main"
        description = "Only for main branch deployments"
        audiences   = @("api://AzureADTokenExchange")
    }
)

foreach ($cred in $federatedCredentials) {
    $json = $cred | ConvertTo-Json -Depth 10 -Compress
    $tmpFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tmpFile -Value $json -Encoding UTF8

    Write-Host "Creating federated credential: $($cred.name)"
    az ad app federated-credential create --id $clientAppId --parameters "@$tmpFile"

    Remove-Item $tmpFile
}

#environment specific (enables deployment to Azure App Services)
$environments = @("dev", "test", "stage", "prod")

foreach ($env in $environments) {
    $cred = @{
        name        = "github-env-$env"
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = "repo:efreeman518/EF.DemoApp1.net:environment:$env"
        description = "GitHub Actions environment: $env"
        audiences   = @("api://AzureADTokenExchange")
    }

    $json = $cred | ConvertTo-Json -Depth 10 -Compress
    $tmpFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tmpFile -Value $json -Encoding UTF8

    Write-Host "Creating federated credential for environment: $env"
    az ad app federated-credential create --id $clientAppId --parameters "@$tmpFile"

    Remove-Item $tmpFile
}

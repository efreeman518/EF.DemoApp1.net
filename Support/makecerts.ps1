#https://docs.microsoft.com/en-us/powershell/module/pkiclient/New-SelfSignedCertificate?view=win10-ps
#Open local powershell
#In order to run in local powershell scripts, enable temporary running scripts:
#Set-ExecutionPolicy ByPass -Scope Process
#.\makecerts.ps1
#C:\s\git\AI\ServiceTemplate\Support

Write-Host "Creating Certificates for Self-Signed Testing"

#Root cert 
Write-Host "Creating Root Certificate"
$rootCert = New-SelfSignedCertificate -Type Custom -KeySpec Signature `
-Subject "CN=dev-root" `
-FriendlyName "dev-cert-root" `
-KeyExportPolicy Exportable `
-HashAlgorithm sha256 -KeyLength 4096 `
-CertStoreLocation "cert://currentuser/My" `
-KeyUsageProperty All `
-KeyUsage KeyEncipherment, DataEncipherment, CertSign `
-TextExtension @("2.5.29.19={text}ca=1&pathlength=3") `
-NotAfter (Get-Date).AddYears(10)

#move the cert from personal /My to Trusted /ROOT 
#$rootCert = Get-ChildItem -Path cert:\currentuser\My\b36b21386d0eedde30be285434f6be6e9b91a1f3

#Intermediate cert
Write-Host "Creating Intermediate Auth Certificate"
$intermediateCert = New-SelfSignedCertificate -Type Custom -KeySpec Signature `
-Subject "CN=dev-intermediate" `
-FriendlyName "dev-cert-intermediate" `
-KeyExportPolicy Exportable `
-HashAlgorithm sha256 -KeyLength 2048 `
-NotAfter (Get-Date).AddYears(10) `
-CertStoreLocation "cert://currentuser/My" `
-KeyUsageProperty Sign `
-KeyUsage CertSign `
-TextExtension @("2.5.29.19={text}ca=1&pathlength=0") `
-Signer $rootCert

#Server TLS/SSL cert (TextExtension = Server Auth & code signing to create the client cert)
#-KeyUsageProperty All `
#-KeyUsage KeyEncipherment, DataEncipherment, CertSign `
#-Type SSLServerAuthentication
Write-Host "Creating Server TLS/Signing Certificate"
$serverCert = New-SelfSignedCertificate -Type Custom -KeySpec KeyExchange `
-Subject "CN=dev-server" `
-FriendlyName "dev-cert-server" `
-DNSName "localhost" `
-KeyExportPolicy Exportable `
-HashAlgorithm sha256 -KeyLength 2048 `
-NotAfter (Get-Date).AddYears(10) `
-CertStoreLocation "cert://currentuser/My" `
-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") `
-Signer $intermediateCert 

#Client cert (TextExtension = Client Auth)
#-KeyUsage KeyEncipherment, DataEncipherment `
Write-Host "Creating Client Auth Certificate"
$clientCert = New-SelfSignedCertificate -Type Custom -KeySpec KeyExchange `
-Subject "CN=dev-client" `
-FriendlyName "dev-cert-client" `
-KeyExportPolicy Exportable `
-HashAlgorithm sha256 -KeyLength 2048 `
-NotAfter (Get-Date).AddYears(10) `
-CertStoreLocation "cert://currentuser/My" `
-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
-Signer $intermediateCert 


#https://docs.microsoft.com/en-us/powershell/module/pkiclient/export-pfxcertificate?view=win10-ps
$PFXPass = ConvertTo-SecureString -String "Rbs#Q->u^+{39*?dg9" -Force -AsPlainText

Write-Host "Exporting Certificates to file"

Export-PfxCertificate -Cert $clientCert -Force `
-ChainOption BuildChain `
-Password $PFXPass `
-FilePath dev-cert-client.pfx


Export-PfxCertificate -Cert $serverCert -Force `
-ChainOption BuildChain `
-Password $PFXPass `
-FilePath dev-cert-server.pfx




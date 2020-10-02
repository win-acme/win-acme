<#
.SYNOPSIS
Imports a cert from win-acme (WACS) renewal into Azure AD Application Proxy for all applications that are using it. You likely want to use a wildcard certificate for this purpose.

.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme (WACS) via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER PfxPath
The absolute path to the pfx file that will be uploaded to Azure. Typically use '{CacheFile}'

.PARAMETER CertPass
The password for the pfx file. Typically use '{CachePassword}'

.PARAMETER Username
Username of account to login with Connect-AzureAD. This account must have the "Application administrator" role to allow it to change the proxy certificate.

.PARAMETER Password
Password for the azure account

.EXAMPLE 

ImportAzureApplicationGateway.ps1 <PfxPath> <CertPass>

.NOTES
Wanted to use a service principal instead of an account for this, but since there is a bug with the cmdlets used, we can't. Instead a regular account must be specified.
https://github.com/Azure/azure-docs-powershell-azuread/issues/200


#>

param(
    [Parameter(Position=0,Mandatory=$false)][string]$PfxPath,
    [Parameter(Position=1,Mandatory=$true)][string]$CertPass,
    [Parameter(Position=2,Mandatory=$true)][string]$Username,
    [Parameter(Position=3,Mandatory=$true)][string]$Password
    
    
)

# Convert the password for the certificate to a secure string
$SecureCertPass = ConvertTo-SecureString -String $CertPass -AsPlainText -Force


if (!(Get-Command "Set-AzureAdApplicationProxyApplicationCustomDomainCertificate" -erroraction SilentlyContinue)) {
    Throw "Missing AzureAD module, install with 'Install-Module -name AzureAD -Scope AllUsers'"
} 

# Connect to Azure
$Pass = ConvertTo-SecureString -String $Password -AsPlainText -Force
$Credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $Username, $Pass
$null = Connect-AzureAD -Credential $Credential


# It's easier, apparently, to search for the service principals that are tagged with WindowsAzureActiveDirectoryOnPremApp,
# then match them to the Get-AzureADApplication output by AppId.
# Get-AzureADApplication doesn't have any way to filter only for ones using the application proxy, and 
# Get-AzureADApplicationProxyApplication requires an ObjectId, there's no way to just list them all.
$aadapServPrinc = Get-AzureADServicePrincipal -Top 100000 | where-object {$_.Tags -Contains "WindowsAzureActiveDirectoryOnPremApp"}

# Now we get a list of all Azure AD Applications
$aadapps = Get-AzureADApplication -All $true

# The AppId between $aadapServPrinc and $aadapps is the same for each of the applications using Azure AD Application Proxy.
# What we need to get is the ObjectId from $aadapps for each application that was in $aadapServPrinc
$aadproxyapps = $aadapServPrinc | Foreach-Object { $aadapps -match $_.AppId}
# Now $aadaproxyapps has just the Get-AzureADApplication objects for applications that use the Azure AD Application Proxy.

"Found $($aadproxyapps.count) applications to update"

# Get the matching objects from Get-AzureADApplicationProxyApplication and show the certificate being used
#$aadproxyapps | Foreach-Object { 
#    $proxyapp = Get-AzureADApplicationProxyApplication -ObjectId $_.ObjectId
#    Write-Host "Checking $($proxyapp.ExternalUrl)"
#    Write-Host "Existing certificate is: $($proxyapp.VerifiedCustomDomainCertificatesMetadata)"
#    
#}

# The documentation says "If you have one certificate that includes many of your applications, you only need to upload it with one application and it will also be assigned to the other relevant applications."
# That does not seem to be the case. Updating the certificate for one application only updated that single application, the rest keep using the old certificate.
# Perhaps it just takes a bit to update, but I thought it safer to just update all of them.

$aadproxyapps | Foreach-Object {
    "Updating certificate for $($_.DisplayName)"
    Set-AzureADApplicationProxyApplicationCustomDomainCertificate -ObjectId $_.ObjectId -PfxFilePath $PfxPath -Password $SecureCertPass
}


Disconnect-AzureAD
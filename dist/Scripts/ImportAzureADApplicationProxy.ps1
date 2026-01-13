<#
.SYNOPSIS
Imports a cert from win-acme (WACS) renewal into Azure AD Application Proxy for all applications that are using it. You likely want to use a wildcard certificate for this purpose.

.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme (WACS) via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Uses Microsoft Graph API instead of deprecated AzureAD module.

.PARAMETER PfxPath
The absolute path to the pfx file that will be uploaded to Azure. Typically use '{CacheFile}'

.PARAMETER CertPass
The password for the pfx file. Typically use '{CachePassword}'

.PARAMETER Username
Username of account to login with Connect-MgGraph. This account must have the "Application administrator" role.

.PARAMETER Password
Password for the account

.EXAMPLE 
ImportAzureApplicationGateway.ps1 <PfxPath> <CertPass> <Username> <Password>

.NOTES
This uses the Microsoft Graph API instead of the deprecated Azure AD module which no longer works.
Unfortunately, the graph API doesn't have good (or any really) documentation about managing certificates.
#>

param(
    [Parameter(Position=0,Mandatory=$true)][string]$PfxPath,
    [Parameter(Position=1,Mandatory=$true)][string]$CertPass,
    [Parameter(Position=2,Mandatory=$true)][string]$Username,
    [Parameter(Position=3,Mandatory=$true)][string]$Password
)

if (!(Get-Command "Get-MGBetaApplication" -ErrorAction SilentlyContinue)) {
    Throw "Missing Microsoft.Graph.Beta module, install with 'Install-Module -Name Microsoft.Graph.Beta -Scope AllUsers'"
} 

# Connect to Microsoft Graph using username/password
$SecurePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force
$ClientId = "1950a258-227b-4e31-a9cf-717495945fc2" # Microsoft Azure PowerShell app ID

# Get access token
$body = @{
    grant_type = "password"
    client_id = $ClientId
    username = $Username
    password = $Password
    scope = "https://graph.microsoft.com/.default"
}

$tokenResponse = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/organizations/oauth2/v2.0/token" -Body $body
$accessToken = $tokenResponse.access_token | ConvertTo-SecureString -AsPlainText -Force

Connect-MgGraph -AccessToken $accessToken -NoWelcome





# Get service principals tagged with WindowsAzureActiveDirectoryOnPremApp
$aadapServPrinc = Get-MgBetaServicePrincipal -All | Where-Object {$_.Tags -Contains "WindowsAzureActiveDirectoryOnPremApp"}

# Get all applications
$aadapps = Get-MgBetaApplication -All

# Match by AppId to get proxy applications
$aadproxyapps = $aadapServPrinc | ForEach-Object { $aadapps | Where-Object AppId -eq $_.AppId }

"Found $($aadproxyapps.count) applications to update"

# Read certificate and convert to Base64
$certBytes = [System.IO.File]::ReadAllBytes($PfxPath)
$certBase64 = [System.Convert]::ToBase64String($certBytes)

# Update each application
$aadproxyapps | ForEach-Object {
    "Updating certificate for $($_.DisplayName)"
    
    $body = @{
        onPremisesPublishing = @{
            verifiedCustomDomainKeyCredential = @{
                type="X509CertAndPassword";
                value = $certBase64
            };

            verifiedCustomDomainPasswordCredential = @{ value = $CertPass };
        }
    } | ConvertTo-Json -Depth 10
    
    Update-MgBetaApplication -applicationid $_.Id -BodyParameter $body
}

$null = Disconnect-MgGraph

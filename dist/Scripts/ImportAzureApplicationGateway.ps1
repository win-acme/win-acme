<#
.SYNOPSIS
Imports a cert from win-acme (WACS) renewal into an Azure Application Gateway instance.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme (WACS) via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER ResourceGroupName
The name of the resource group the application gateway was created in.

.PARAMETER ApplicationGatewayName
The name of the application gateway resource.

.PARAMETER CertName
The name of the Azure certificate entry.

.PARAMETER CertPass
The password of the Azure certificate entry.

.PARAMETER PfxPath
The absolute path to the pfx file that will be uploaded to Azure.

.PARAMETER AppGatewayFrontendPortName
Optional. Name of the frontend port entry that will be created in Application Gateway. Will be appGatewayFrontendPortSsl by default.

.PARAMETER AppGatewayFrontendPortPort
Optional. Port that will be used when creating the frontend port entry in Application Gateway. Will be port 443 by default.

.PARAMETER AppGatewayBackendHttpSettingsName
Optional. Name of the HTTP settings entry that will be created in Application Gateway. Will be appGatewayBackendHttpSettings by default.

.PARAMETER AppGatewayHttpsListenerName
Optional. Name of the Listener that will be created in Application Gateway. Will be appGatewayHttpsListener by default.

.PARAMETER AppGatewayHttpsRuleName
Optional. Name of the rule connecting listener, backend pool and HTTP setting that will be created in Application Gateway. Will be httpsRule by default.

.EXAMPLE 

ImportAzureApplicationGateway.ps1 <resourceGroupName> <appGatewayName> <certName> <certPass> <pfxPath>

.NOTES

This script requires an active Azure context that can be created by using "Connect-AzureRmAccount". This must be followed by an "Enable-AzureRmContextAutosave" to persist it across sessions to allow the background job to succeed. Make sure the context is persisted for the correct Windows user. This must be the one that is used for the schedules task.

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$ResourceGroupName,
    [Parameter(Position=1,Mandatory=$true)]
    [string]$AppGatewayName,
    [Parameter(Position=2,Mandatory=$true)]
    [string]$CertName,
    [Parameter(Position=3,Mandatory=$false)]
    [string]$CertPass,
    [Parameter(Position=4,Mandatory=$true)]
    [string]$PfxPath,
    [Parameter(Position=5,Mandatory=$false)]
    [string]$AppGatewayFrontendPortName = 'appGatewayFrontendPortSsl',
    [Parameter(Position=6,Mandatory=$false)]
    [int]$AppGatewayFrontendPortPort = 443,
    [Parameter(Position=7,Mandatory=$false)]
    [string]$AppGatewayBackendHttpSettingsName = 'appGatewayBackendHttpSettings',
    [Parameter(Position=8,Mandatory=$false)]
    [string]$AppGatewayHttpsListenerName = 'appGatewayHttpsListener',
    [Parameter(Position=9,Mandatory=$false)]
    [string]$AppGatewayHttpsRuleName = 'httpsRule'
)

if ($CertPass -ne "") {
    $CertPassSecure = ConvertTo-SecureString -String $CertPass
} else {
    $CertPassSecure = New-Object System.Security.SecureString
}

if (!(Get-Command "Get-AzureRmApplicationGateway" -errorAction SilentlyContinue)) {
    "Missing Azure RM PowerShell extension. Install with 'Install-Module -Name AzureRM -AllowClobber'"
    exit
}

try {
    Get-AzureRmContext
} catch {
    "Missing Azure RM Context. Use Connect-AzureRmAccount and Enable-AzureRmContextAutosave to login to your Azure tenant and save the context between sessions."
    exit
}

"Deploying certificate to the Application Gateway $AppGatewayName in resource group $ResourceGroupName"
$appGateway = Get-AzureRmApplicationGateway -ResourceGroupName $ResourceGroupName -Name $AppGatewayName

try {
    # Check if listener already exists and needs updating or create everything (catch clause)
    Get-AzureRmApplicationGatewaySslCertificate -ApplicationGateway $appGateway -Name $CertName -ErrorAction Stop | Out-Null
    
    "Certificate already installed... updating"
    Set-AzureRmApplicationGatewaySslCertificate -ApplicationGateway $appGateway -Name $CertName -CertificateFile $PfxPath -Password $CertPassSecure | Out-Null

} catch [System.InvalidOperationException] {
    "Adding Frontend Port for SSL on TCP $AppGatewayFrontendPortPort"
    Add-AzureRmApplicationGatewayFrontendPort -ApplicationGateway $appGateway -Name $AppGatewayFrontendPortName -Port $AppGatewayFrontendPortPort | Out-Null

    "Adding SSL certificate from $PfxPath"
    Add-AzureRmApplicationGatewaySslCertificate -ApplicationGateway $appGateway -Name $CertName -CertificateFile $PfxPath -Password $CertPassSecure | Out-Null

    "Adding HTTPS Listener..."
    $frontendIpConfig = Get-AzureRmApplicationGatewayFrontendIPConfig -ApplicationGateway $appGateway
    $frontendPort = Get-AzureRmApplicationGatewayFrontendPort -ApplicationGateway $appGateway -name $AppGatewayFrontendPortName
    $cert = Get-AzureRmApplicationGatewaySslCertificate -ApplicationGateway $appGateway -Name $CertName
    Add-AzureRmApplicationGatewayHttpListener -ApplicationGateway $appGateway -Name $AppGatewayHttpsListenerName -Protocol Https -FrontendIPConfiguration $frontendIpConfig -FrontendPort $frontendPort -SslCertificate $cert | Out-Null

    "Adding Routing Rule for new HTTPS Listener..."
    $poolSetting = Get-AzureRmApplicationGatewayBackendHttpSettings -ApplicationGateway $appGateway -name $AppGatewayBackendHttpSettingsName
    $listener = Get-AzureRmApplicationGatewayHttpListener -ApplicationGateway $appGateway -Name $AppGatewayHttpsListenerName
    $backendPool = Get-AzureRmApplicationGatewayBackendAddressPool -ApplicationGateway $appGateway
    Add-AzureRmApplicationGatewayRequestRoutingRule -ApplicationGateway $appGateway -Name $AppGatewayHttpsRuleName -RuleType Basic -BackendHttpSettings $poolSetting -HttpListener $listener -BackendAddressPool $backendPool | Out-Null
}

"Saving changes..."
Set-AzureRmApplicationGateway -ApplicationGateway $appGateway | Out-Null

  <#
.SYNOPSIS
Imports a cert from WACS renewal into the RD Gateway, RD Listener, RD WebAccess, RD Redirector and RD Connection Broker

.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there.

.PARAMETER RDCB
This parameter specifies the Remote Desktop Connection Broker (RD Connection Broker) server for a Remote Desktop deployment.
If you don't specify a value, the script uses the local computer's fully qualified domain name (FQDN).

.EXAMPLE 

ImportRDS.ps1 <certThumbprint> <ConnectionBroker.contoso.com>

.NOTES
The private key of the letsencrypt certificate needs to be exportable. Set "PrivateKeyExportable" in settings.json to true.

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
    [Parameter(Position=1,Mandatory=$false)]
    [string]$RDCB
)
if (-not $PSBoundParameters.ContainsKey('RDCB')) {$RDCB = (Get-WmiObject win32_computersystem).DNSHostName+"."+(Get-WmiObject win32_computersystem).Domain} 
try 
{
	Import-Module RemoteDesktopServices
}
catch 
{
	"Cert thumbprint was not set successfully to RDP listener"
	"Error: $($Error[0])"
	return
}
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object { $_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if ($CertInStore) 
{
    try 
	{
        Set-Item -Path RDS:\GatewayServer\SSLCertificate\Thumbprint -Value $CertInStore.Thumbprint -ErrorAction Stop
        Restart-Service TSGateway -Force -ErrorAction Stop
        "Cert thumbprint set to RD Gateway listener and service restarted"
		wmic /namespace:\\root\cimv2\TerminalServices PATH Win32_TSGeneralSetting Set SSLCertificateSHA1Hash="$($CertInStore.Thumbprint)"
    } 
	catch 
	{
        "Cert thumbprint was not set successfully to RD Gateway"
        "Error: $($Error[0])"
		return
    }
    try 
	{
		wmic /namespace:\\root\cimv2\TerminalServices PATH Win32_TSGeneralSetting Set SSLCertificateSHA1Hash="$($CertInStore.Thumbprint)"
        # This method might work, but wmi method is more reliable
        #Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' -Name SSLCertificateSHA1Hash -Value $CertInStore.Thumbprint -ErrorAction Stop
        "Cert thumbprint set to RDP listener"
    } 
	catch 
	{
        "Cert thumbprint was not set successfully to RDP listener"
        "Error: $($Error[0])"
		return
    }
    try
	{
        Add-Type -AssemblyName 'System.Web'
        $tempPasswordPfx = [System.Web.Security.Membership]::GeneratePassword(10, 5) | ConvertTo-SecureString -Force -AsPlainText
        $tempPfxPath = New-TemporaryFile | Rename-Item -PassThru -NewName { $_.name -Replace '\.tmp$','.pfx' } 
        (Export-PfxCertificate -Cert $CertInStore -FilePath $tempPfxPath -Force -NoProperties -Password $tempPasswordPfx) | out-null
    }
	catch 
	{
        "Could not export temporary Certificte. RD Gateway, RD WebAccess, RD Redirector and RD Connection Broker certificates not set."
        "Error: $($Error[0])"
		return
    }
    try 
	{
        # Configure RDPublishing Certificate for RDS
        set-RDCertificate -Role RDPublishing `
           -ImportPath $tempPfxPath `
           -Password $tempPasswordPfx `
           -ConnectionBroker $RDCB -Force
        "RDPublishing Certificate for RDS was set"
    } 
	catch 
	{
        "RDPublishing Certificate for RDS was not set"
        "Error: $($Error[0])"
		return
    }
    try 
	{
        # Configure RDWebAccess Certificate for RDS
        set-RDCertificate -Role RDWebAccess `
           -ImportPath $tempPfxPath `
           -Password $tempPasswordPfx `
           -ConnectionBroker $RDCB -Force
       "RDWebAccess Certificate for RDS was set" 
    } 
	catch 
	{
        "RDWebAccess Certificate for RDS was not set"
        "Error: $($Error[0])"
		return
    }
    try 
	{
        # Configure RDRedirector Certificate for RDS
        set-RDCertificate -Role RDRedirector `
           -ImportPath $tempPfxPath `
           -Password $tempPasswordPfx `
           -ConnectionBroker $RDCB -force
        "RDRedirector Certificate for RDS was set"
    } 
	catch 
	{
        "RDRedirector Certificate for RDS was not set"
        "Error: $($Error[0])"
		return
    }
    try 
	{
        # Configure RDGateway Certificate for RDS
        set-RDCertificate -Role RDGateway `
           -ImportPath $tempPfxPath `
           -Password $tempPasswordPfx `
           -ConnectionBroker $RDCB -force
        "RDGateway Certificate for RDS was set"
    } 
	catch 
	{
        "RDGateway Certificate for RDS was not set"
        "Error: $($Error[0])"
		return
    }
    # Cleanup the temporary PFX file
    Remove-Item -Path $tempPfxPath
} 
else 
{
    "Cert thumbprint not found in the My cert store... have you specified --certificatestore My?"
}
 

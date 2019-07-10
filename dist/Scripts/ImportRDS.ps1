<#
.SYNOPSIS
Imports a cert from WACS renewal into the RD Gateway and RD Listener

.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there. 

.EXAMPLE 

ImportRDS.ps1 <certThumbprint>

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

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
} 
else 
{
    "Cert thumbprint not found in the My cert store... have you specified --certificatestore My?"
}

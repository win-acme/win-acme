<#
.SYNOPSIS
Imports a cert from WASC renewal into Exchange services, this script also runs commands needed to enable the certificate for use on Hybrid Exchange.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from WASC via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

THIS SCRIPT IS INCOMPLETE AND *mostly* UNTESTED (some modifications have come in from people using it successfully)
This modification of the original Exchange script is designed to run on a single Exchange server in a hybrid environment - it's not been tested against multiple servers (the import process would need run on each server)
Documentation referenced from https://technet.microsoft.com/en-us/library/aa997231(v=exchg.160).aspx

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there. 

.PARAMETER ExchangeServices
a comma-separated string (no spaces) of all exchange services to import the cert into. Full list of possibilities can be found here: 

https://technet.microsoft.com/en-us/library/aa997231(v=exchg.160).aspx

.PARAMETER LeaveOldExchangeCerts
A bool (as an int, since bools are difficult to pass through as parameters) to determine if old exchange certs with the same CN should be deleted.

1 - Leaves old Exchange certs
0 - Deletes old Exchange certs

.PARAMETER RenewalId
(Central Certificate Store) Id of the WASC renewal, used to determine the file name in the CertificatePath.

.PARAMETER CertificatePath
(Central Certificate Store) Path to the WACS certificate directory. The certificate that is imported will be "$(RenewalId)-all.pfx" from this directory.

.PARAMETER PfxPassword
(Central Certificate Store) Password of the .pfx file.

.PARAMETER FriendlyName
(Central Certificate Store) Friendly name to use when importing the .pfx file.

.PARAMETER DebugOn
Include this switch parameter to write debug outputs for troubleshooting

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP

If not using central certificate store, the script can be executed as either

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP 0

to remove old certs

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP 1 <renewalId> <certificatePath> <pfxPassword> <friendlyName>

If using central certificate store, WASC will place the certificate in that path named after the id

.NOTES

#>

param(
	[Parameter(Position=0,Mandatory=$true)]
	[string]
	$NewCertThumbprint,
	
	[Parameter(Position=1,Mandatory=$true)]
	[string]
	$ExchangeServices,
	
	[Parameter(Position=2,Mandatory=$false)]
	[int]
	$LeaveOldExchangeCerts = 1,
	
	[Parameter(Position=4,Mandatory=$false)]
	[string]
	$CacheFile,
	
	[Parameter(Position=5,Mandatory=$false)]
	[string]
	$PfxPassword,
	
	[Parameter(Position=6,Mandatory=$false)]
	[string]
	$FriendlyName,
	
	[switch]$DebugOn
)

if($DebugOn){
	$DebugPreference = "Continue"
}

If($OSVersion -eq "Windows Server 2008 R2 Standard" -and $PSVersionTable.PSVersion.Major -lt 5)
{
	Write-Error "Please upgrade Powershell version. See this URL for details: https://github.com/PKISharp/win-acme/issues/1104"
	exit
}

# Print debugging info to make sure the parameters arrived
Write-Host "NewCertThumbprint: $NewCertThumbprint"
Write-Host "ExchangeServices: $ExchangeServices"
Write-Host "LeaveOldExchangeCerts: $LeaveOldExchangeCerts"
Write-Host "RenewalId: $RenewalId"
Write-Host "CacheFile: $CacheFile"
Write-Host "FriendlyName: $FriendlyName"

# Load Exchange Management shell
# https://github.com/win-acme/win-acme/issues/1372 
$ServerFQDN = ([System.Net.Dns]::GetHostByName(($env:computerName))).Hostname
$ConnectionUri = "http://$ServerFQDN/PowerShell/"
$Session = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri $ConnectionUri
Import-PSSession $Session

# Test if the Cmdlet is there now
$Command = Get-Command "Enable-ExchangeCertificate" -errorAction SilentlyContinue
if ($Command -eq $null) 
{
	Write-Error "Exchange Management Tools for Powershell not installed"
	return
}
	
try 
{
    # Load certificate
    Write-Host "Checking if certificate can be found in the right store..."
    $Certificate = `
	    Get-ChildItem -Path Cert:\LocalMachine -Recurse `
	    | Where-Object {$_.thumbprint -eq $NewCertThumbprint} `
	    | Sort-Object -Descending `
	    | Select-Object -f 1
            
	# Make sure variable is defined
	Get-ChildItem $Certificate.PSPath -ErrorAction Stop | Out-Null
	
	# This command actually updates Exchange
	try 
	{
		Write-Host "Updating Exchange services..."
		Enable-ExchangeCertificate -Services $ExchangeServices -Thumbprint $Certificate.Thumbprint -Force -ErrorAction Stop
        $TLSCert = Get-ExchangeCertificate -Thumbprint $Certificate.Thumbprint
        $TLSCertName = "<I>$($TLSCert.Issuer)<S>$($TLSCert.Subject)"
		Write-Host "Updating 'Outbound to Office 365' send connector"
        Get-SendConnector '*Outbound to Office 365*' | Set-SendConnector -TlsCertificateName $TLSCertName
		Write-Host "Updating 'Default Frontend' receive connector for the current server"
        Get-ReceiveConnector "*Default Frontend $(hostname)*" | Set-ReceiveConnector -TlsCertificateName $TLSCertName
		Write-Host "Updating Set-HybridConfiguration certificate field"
        Set-HybridConfiguration -TlsCertificateName $TLSCertName
		Write-Host "Certificate set"
	}
	catch 
	{
		Write-Error "Error occurred when applying certificate to Exchange"
		throw
	}
	
	if ($LeaveOldExchangeCerts -ne 1)
	{
		Write-Host "Old Exchange certificates being cleaned up"
		try 
		{
			Get-ExchangeCertificate -DomainName $Certificate.Subject.split(",")[0].split("=")[1] `
				| Where-Object -FilterScript {
					$_.Thumbprint -ne $NewCertThumbprint
				} `
			| Remove-ExchangeCertificate -Confirm:$false
		} 
		catch 
		{
			Write-Error "Error cleaning up old certificates Get-ExchangeCertificate/Remove-ExchangeCertificate"
		}
	}
} 
catch 
{
    Write-Error "Script hasn't completed."
}

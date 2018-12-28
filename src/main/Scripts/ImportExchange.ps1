<#
.SYNOPSIS
Imports a cert from WASC renewal into Exchange services.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from WASC via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

THIS SCRIPT IS INCOMPLETE AND *mostly* UNTESTED (some modifications have come in from people using it successfully)
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
.PARAMETER TargetHost
Primary FQDN of certificate to import (only relevant if using central certificate store)
.PARAMETER StorePath
Path of certificate to import. Requires a valid TargetHost parameter. The certificate that is imported will be "$(TargetHost).pfx" from this directory.

.PARAMETER DebugOn
Include this switch parameter to write debug outputs for troubleshooting

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP

If not using central certificate store, the script can be executed as either

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP 0

to remove old certs

.EXAMPLE 

ImportExchange.ps1 <certThumbprint> IIS,SMTP,IMAP 1 "mail.example.com" "c:\win-acme\certs"

If using central certificate store, WASC will place the certificate in that path with a name of the primary FQDN. 

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
    [Parameter(Position=1,Mandatory=$true)]
    [string]$ExchangeServices,
    [Parameter(Position=2,Mandatory=$false)]
    [int]$LeaveOldExchangeCerts = 1,
    [Parameter(Position=3,Mandatory=$false)]
    [string]$TargetHost,
    [Parameter(Position=4,Mandatory=$false)]
    [string]$StorePath,
    [switch]$DebugOn
)

if($DebugOn){
    $DebugPreference = "Continue"
}

Write-Debug -Message ('NewCertThumbprint: {0}' -f $NewCertThumbprint)
Write-Debug -Message ('ExchangeServices: {0}' -f $ExchangeServices)
Write-Debug -Message ('LeaveOldExchangeCerts: {0}' -f $LeaveOldExchangeCerts)
Write-Debug -Message ('TargetHost: {0}' -f $TargetHost)
Write-Debug -Message ('StorePath: {0}' -f $StorePath)

# Should work with exchange 2007 and higher
Get-PSSnapin -registered | Where-Object {$_.Name -match "Microsoft.Exchange.Management.PowerShell" -and ($_.Name -match "Admin" -or $_.Name -match "E2010" -or $_.Name -match "SnapIn")} | Add-PSSnapin -ErrorAction SilentlyContinue

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
try{
    if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
        "Cert thumbprint not found in the cert store... which means we should load it. This means TargetHost and StorePath must be specified"
        
        "Try to load certificate from store"
        $importExchangeCertificateParameters = @{
        FileName = (Join-Path -Path $StorePath -ChildPath "$TargetHost.pfx")
        FriendlyName = $TargetHost
        PrivateKeyExportable = $true
        }
        
        $null = Import-ExchangeCertificate @importExchangeCertificateParameters -ErrorAction Stop

       
    }
    # attempt to get cert again directly from Personal Store
    $CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My\ -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Select-Object -f 1
            
    # Make sure variable is defined
    $null = Get-ChildItem $CertInStore.PSPath -ErrorAction Stop

    # Cert must exist in the personal store of machine to bind to exchange services
    if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
        $SourceStoreScope = 'LocalMachine'
        $SourceStorename = $CertInStore.PSParentPath.split("\")[-1]

        $SourceStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $SourceStorename, $SourceStoreScope
        $SourceStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)

        $cert = $SourceStore.Certificates | Where-Object {$_.thumbprint -eq $CertInStore.Thumbprint}



        $DestStoreScope = 'LocalMachine'
        $DestStoreName = 'My'

        $DestStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $DestStoreName, $DestStoreScope
        $DestStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $DestStore.Add($cert)


        $SourceStore.Close()
        $DestStore.Close()

        $CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Select-Object -f 1
    }
    Enable-ExchangeCertificate -Services $ExchangeServices -Thumbprint $CertInStore.Thumbprint -Force -ErrorAction Stop
    "Cert thumbprint set to the following exchange services: $ExchangeServices"

    
    if($LeaveOldExchangeCerts -ne 1){
        "Old Exchange certificates being cleaned up"
        Get-ExchangeCertificate -DomainName $CertInStore.Subject.split("=")[1] | Where-Object -FilterScript {
            $_.Thumbprint -ne $NewCertThumbprint
        } | Remove-ExchangeCertificate -Confirm:$false
    }

}catch{
    "Cert thumbprint was not set successfully"
    "Error: $($Error[0])"
}



<#
.SYNOPSIS
Imports a cert from WACS renewal into any WinRM HTTPS listeners
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the
batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the
cmd line.

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal
store if not already there.

.EXAMPLE

ImportWinRM.ps1 <certThumbprint>

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.Thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -First 1
if($CertInStore){
    try{
        $winrm = 'winrm/config/listener'

        Get-WSManInstance -ResourceURI $winrm -Enumerate | Where-Object {$CertInStore.DnsNameList -contains $_.Hostname} | ForEach-Object {Set-WSManInstance -ResourceURI $winrm -SelectorSet @{Address=$_.Address; Transport=$_.Transport} -ValueSet @{CertificateThumbprint=$CertInStore.Thumbprint}}

        Restart-Service WinRM -Force -ErrorAction Stop
        "Cert thumbprint set to WinRM public HTTPS listener and service restarted"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


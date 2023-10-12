<#
.SYNOPSIS
Imports a cert from WACS renewal into SQL Server
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/win-acme/win-acme

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported.


.EXAMPLE 

ImportSQL.ps1 <certThumbprint>

./wacs.exe --target manual --host hostname.example.com --installation script --script ".\Scripts\ImportSQL.ps1" --scriptparameters "'{CertThumbprint}'" --certificatestore My --acl-read "NT Service\MSSQLSERVER"

.NOTES

#>
param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

#inspired by https://blogs.infosupport.com/configuring-sql-server-encrypted-connections-using-powershell/
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{
        # 1. Configure the Certificate that SQL Server should use
        # Locate the "SuperSocketNetLib" registry key that contains the encryption settings; highest 
        # first in case there are multiple versions.
        $regKey = (ls "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server" -Recurse | 
            Where-Object { $_.Name -like '*SuperSocketNetLib' } | Sort-Object -Property Name -Descending)
        if($regKey -is [Array])
        {
            $regKey = $regKey[0]
            Write-Warning "Multiple SQL instances found in the registry, using ""$($regKey.Name)""."
        }
        # The thumbprint must be in all lowercase, otherwise SQL server doesn't seem to accept it?!
        Set-ItemProperty $regKey.PSPath -Name "Certificate" -Value $NewCertThumbprint.ToLowerInvariant()
        Restart-Service MSSQLSERVER -Force -ErrorAction Stop
        "Cert thumbprint set to SQL and service restarted"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
} else {
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}
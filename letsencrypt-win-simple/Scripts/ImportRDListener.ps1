<#
.SYNOPSIS
Imports a cert from win-acme (WACS) renewal into the RDP service listener on localhost.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme (WACS) via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there. 


.EXAMPLE 

ImportRDListener.ps1 <certThumbprint>

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Select-Object -f 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine to bind to RD Gateway
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
        wmic /namespace:\\root\cimv2\TerminalServices PATH Win32_TSGeneralSetting Set SSLCertificateSHA1Hash="$($CertInStore.Thumbprint)"
        # This method might work, but wmi method is more reliable
        #Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' -Name SSLCertificateSHA1Hash -Value $CertInStore.Thumbprint -ErrorAction Stop
        "Cert thumbprint set to RDP listener"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


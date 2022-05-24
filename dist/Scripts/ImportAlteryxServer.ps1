 <#
.SYNOPSIS
Imports a cert from WACS renewal into Alteryx Server.
.DESCRIPTION
It is assumed that Alteryx System Settings has been used to enable HTTPS and set the correct base URL for Gallery. 

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

ImportAlteryxServer.ps1 <certThumbprint>

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)


$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.Thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -First 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine
        if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
            $SourceStoreScope = 'LocalMachine'
            $SourceStorename = $CertInStore.PSParentPath.split("\")[-1]

            $SourceStore = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Store -ArgumentList $SourceStorename, $SourceStoreScope
            $SourceStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)

            $cert = $SourceStore.Certificates | Where-Object {$_.thumbprint -eq $CertInStore.Thumbprint}

            $DestStoreScope = 'LocalMachine'
            $DestStoreName = 'My'

            $DestStore = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Store -ArgumentList $DestStoreName, $DestStoreScope
            $DestStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $DestStore.Add($cert)

            $SourceStore.Close()
            $DestStore.Close()

            $CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
        }
        ## Alteryx Server specific steps from https://help.alteryx.com/20212/server/configure-gallery-ssltls
        # Stop Alteryx service
        Stop-Service AlteryxService -Force -ErrorAction Stop
        # Delete old cert
        netsh http delete sslcert ipport=0.0.0.0:443  
        ## Add new cert
        netsh http add sslcert ipport=0.0.0.0:443 certhash="$($CertInStore.Thumbprint)" appid='{eea9431a-a3d4-4c9b-9f9a-b83916c11c67}'
        # Start service
        Start-Service AlteryxService

        "Alteryx Server was been reconfigured with the new certificate, and the service was restarted."
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}

 

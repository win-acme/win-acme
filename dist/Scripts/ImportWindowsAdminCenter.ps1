<#
.SYNOPSIS
Imports a cert from WACS renewal into Windows Admin Center.
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

ImportWindowsAdminCenter.ps1 <certThumbprint>

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
        
        # Get the Object GUID for use with msiexec
        $installedWmiObject=Get-WmiObject Win32_Product | Where-Object Name -eq "Windows Admin Center"
        $productCodeGUID=$installedWmiObject.IdentifyingNumber

        $msiParams = @{
            "FilePath" = "$Env:SystemRoot\system32\msiexec.exe"
            "ArgumentList" = @(
            "/i"
            "$($productCodeGUID)"
            "/qn"
            "SME_THUMBPRINT=$($CertInStore.Thumbprint)"
            "SSL_CERTIFICATE_OPTION=installed"
            )
            "Verb" = "runas"
            "PassThru" = $true
        }

        # Run as a process and wait for exit
        $msiProcess = Start-Process @msiParams
        $msiProcess.WaitForExit()

        Restart-Service ServerManagementGateway -Force -ErrorAction Stop

        "Windows Admin Center was been reconfigured with the new certificate, and the service was restarted."
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


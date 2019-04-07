<#
.SYNOPSIS
Imports a cert from WACS renewal into Web Management Service (Web Deploy) HTTPS listeners
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
        # Cert must exist in the personal store of machine to bind to RD Gateway
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

        "Attempting to stop WMSVC..."
        Stop-Service wmsvc

        "Removing unassigned addresses SSl bindings... (ignore errors)"
        netsh http delete sslcert ipport=0.0.0.0:8172

        "Assigning certificate..."
        netsh http add sslcert ipport=0.0.0.0:8172 certhash=$NewCertThumbprint appid="{d7d72267-fcf9-4424-9eec-7e1d8dcec9a9}" certstorename="MY"

        "Updating Registry pointing WMSVC to new binding"
        $bytes = for($i = 0; $i -lt $NewCertThumbprint.Length; $i += 2) { [convert]::ToByte($NewCertThumbprint.SubString($i, 2), 16) }
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\WebManagement\Server' -Name SslCertificateHash -Value $bytes

        "Attempting start of WMSVC..."
        Start-Service wmsvc
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}

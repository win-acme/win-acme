<#
.SYNOPSIS
Imports a cert from WACS renewal into the Veeam SSL binding
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper.

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. 


.EXAMPLE 

ImportVBRCloudGateway.ps1 <certThumbprint>

.NOTES

#>


param(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$NewCertThumbprint
)

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object { $_.thumbprint -eq $NewCertThumbprint } | Sort-Object -Descending | Select-Object -f 1

if ($CertInStore) {
    try {
        Connect-VBRServer -Server localhost

        $certificate = Get-VBRCloudGatewayCertificate -FromStore | Where { $_.Thumbprint -eq $NewCertThumbprint }

        Add-VBRCloudGatewayCertificate -Certificate $certificate

        Disconnect-VBRServer

        "Cert thumbprint was set successfully"
    }
    catch {
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}
else {
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


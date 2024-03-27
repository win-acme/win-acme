  <#
.SYNOPSIS
Imports a cert from WACS renewal into the NTDS store

.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there.

.PARAMETER OldCertThumbprint
The exact thumbprint of the cert to be replaced. The script will delete this cert from the Personal store of the NTDS upon successful completion.

If you don't specify this value, the replaced cert will remain in the store.

.EXAMPLE 

ImportNTDS.ps1 <certThumbprint> <ConnectionBroker.contoso.com> <oldCertThumbprint>

.NOTES
The private key of the letsencrypt certificate needs to be exportable. Set "PrivateKeyExportable" in settings.json to true.

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
    [Parameter(Position=2,Mandatory=$false)]
    [string]$OldCertThumbprint

)
$Destination = "HKLM:\SOFTWARE\Microsoft\Cryptography\Services\NTDS\SystemCertificates\MY\Certificates\"
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object { $_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if ($CertInStore) 
{
    try 
	{
        if ( !( Test-Path $Destination ) ) {
            New-Item -Path $Destination -Force
        }
    } 
	catch 
	{
        "Unable to create registry key at $Destination"
        "Error: $($Error[0])"
		return
    }
    try
	{
        if ( ( Test-Path $destination ) -and $CertInStore ) {
            Move-Item "HKLM:\SOFTWARE\Microsoft\SystemCertificates\MY\Certificates\$NewCertThumbprint" $destination
        }
    }
	catch 
	{
        "Could not move certificate into NDTS store location."
        "Error: $($Error[0])"
		return
    }
    try
    {
        if ( $OldCertThumbprint ) {
            $oldCert = Get-ChildItem "$destination\$OldCertThumbprint"
            if ( $oldCert ) {
                Remove-Item $oldCert.PSPath
            }
        }
    }
    catch
    {
        "Unable to remove old cert with thumbprint $OldCertThumbprint"
        "Error: $($Error[0])"
		return

    }
}
else 
{
    "Cert thumbprint not found in the My cert store... have you specified --certificatestore My?"
}

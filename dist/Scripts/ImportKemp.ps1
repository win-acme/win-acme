<#
.SYNOPSIS
Imports a cert from WASC renewal into KEMP Loadmaster.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from WASC via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

THIS SCRIPT IS INCOMPLETE AND *mostly* UNTESTED (some modifications have come in from people using it successfully)
Proper information should be available here

https://github.com/PKISharp/win-acme/wiki/Install-Script

or more generally, here

https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER CertName
The exact ID of the cert to be imported.

.PARAMETER PfxFile
File name in the CertificatePath.

.PARAMETER PfxPassword
(Central Certificate Store) Password of the .pfx file.

.PARAMETER KempUserName
Username for KEMP PowerShell module

.PARAMETER KempUserPass
Password for KEMP PowerShell module

.PARAMETER KempIP
KEMP IP address

.EXAMPLE 

./Scripts/ImportKemp.ps1 "'{RenewalId}' '{CacheFile}' '{CachePassword}' 'bal' 'pass' '10.10.10.10'"


.NOTES
KEMP PowerShell module installation help:
https://support.kemptechnologies.com/hc/en-us/articles/203863385-PowerShell#MadCap_TOC_8_2

Download site, Tools --> General --> LoadMaster PowerShell API Wrapper: https://kemptechnologies.com/loadmaster-documentation/

#>

param(
	[Parameter(Position=0,Mandatory=$true)]
	[string]
	$CertName,
		
	[Parameter(Position=1,Mandatory=$false)]
	[string]
	$PfxFile,
	
	[Parameter(Position=2,Mandatory=$false)]
	[string]
	$PfxPassword,
	
	[Parameter(Position=3,Mandatory=$false)]
	[string]
	$KempUserName,

	[Parameter(Position=4,Mandatory=$false)]
	[string]
	$KempUserPass,

    [Parameter(Position=5,Mandatory=$false)]
	[string]
	$KempIP
)

Import-Module Kemp.LoadBalancer.Powershell

#Get-Module Kemp.LoadBalancer.Powershell
#Test-LmServerConnection -ComputerName $KempIP -Port 443 -Verbose

$password = ConvertTo-SecureString $KempUserPass -AsPlainText -Force
$psCred = New-Object System.Management.Automation.PSCredential -ArgumentList ($KempUserName, $password)
$arrLMConnectResult = Initialize-LmConnectionParameters -Address $KempIP -LBPort 443 -Credential $psCred

#Get-Command -Module Kemp.LoadBalancer.Powershell | Out-GridView
#(Get-TlsCertificate).Data.cert

$NewTlsCertificateResult = New-TlsCertificate -Name $CertName -Password $PfxPassword -Replace -Path $PfxFile
if($NewTlsCertificateResult.returncode -eq "422"){
    $NewTlsCertificateResult = New-TlsCertificate -Name $CertName -Password $PfxPassword -Path $PfxFile
}

$NewTlsCertificateResult

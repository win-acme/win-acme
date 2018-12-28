<#
.SYNOPSIS
	Imports a cert from WACS renewal into the IIS 7.5 FTP Server settings, 
    please note that for IIS 8 and higher WACS can do that without any scripts

.DESCRIPTION
	Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

	Proper information should be available here

	https://github.com/PKISharp/win-acme/wiki/Install-Script

	or more generally, here

	https://github.com/PKISharp/win-acme/wiki/Example-Scripts

.PARAMETER NewCertThumbprint
	The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there. 

.PARAMETER SitePath
	Path to the IIS Site of the FTP Server

.EXAMPLE 
	ImportFTPServer.ps1 <certThumbprint> IIS:\Sites\DefaultFTP

.NOTES

#>

param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
	
	[Parameter(Position=1,Mandatory=$true)]
    [string]$SitePath
)

if (!(Get-Module -ListAvailable -Name WebAdministration)) {
	throw "WebAdministration Module not available, please install IIS Powershell Snap-in, https://www.microsoft.com/en-us/download/details.aspx?id=7436 or https://www.microsoft.com/en-us/download/details.aspx?id=15488"
} 

if (!(Get-Module WebAdministration))
{
    ## Load it nested, and we'll automatically remove it during clean up.
    Import-Module WebAdministration -ErrorAction Stop
	Sleep 2 #see http://stackoverflow.com/questions/14862854/powershell-command-get-childitem-iis-sites-causes-an-error
}

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{        
		Set-ItemProperty -Path $SitePath -Name ftpServer.security.ssl.serverCertHash -Value $NewCertThumbprint
		
        "Cert thumbprint set to FTP Server"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}

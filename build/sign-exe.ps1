<#
# Use the following command to create a self-signed cert to build a signed version of the WACS executable 
New-SelfSignedCertificate `
    -CertStoreLocation cert:\currentuser\my `
    -Subject "CN=WACS" `
    -KeyUsage DigitalSignature `
    -Type CodeSigning
#>

param (
	[Parameter(Mandatory=$true)]
	[string]
	$Path,
	
	[Parameter(Mandatory=$true)]
	[string]
	$Pfx,
	
	[Parameter()]
	[string]
	$Password
)

$SignTool = "C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe"
if (Test-Path $SignTool) 
{
	if ($Password -eq "" -or $Password -eq $null) 
	{
		$Password = Read-Host -Prompt "Input password for $Pfx or press enter to skip code signing"
	}	
	if ($Password -ne "") 
	{
		& $SignTool sign /fd SHA256 /f "$Pfx" /p "$Password" "$Path"
	}
} 
else 
{
	Write-Host "$SignTool not found"
}
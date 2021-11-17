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

$paths = @(
	"C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe",
	"C:\Program Files (x86)\Windows Kits\10\bin\10.0.18362.0\x64\signtool.exe",
	"C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
)
foreach ($possiblePath in $paths) 
{
	if (Test-Path $possiblePath) 
	{
		$SignTool = $possiblePath
	}
}
if ($null -ne $SignTool) 
{
	if ($Password -eq "" -or $Password -eq $null) 
	{
		$Password = Read-Host -Prompt "Input password for $Pfx or press enter to skip code signing"
	}	
	if ($Password -ne "") 
	{
		& $SignTool sign /debug /fd SHA256 /f "$Pfx" /p "$Password" "$Path"
	}
} 
else 
{
	Write-Host "SignTool not found"
}
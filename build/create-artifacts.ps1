param (
	[Parameter(Mandatory=$true)]
	[string]
	$Root,
	
	[Parameter(Mandatory=$true)]
	[string]
	$Version,
	
	[Parameter()]
	[string]
	$Password
)

Add-Type -Assembly "system.io.compression.filesystem"
$Temp = "$Root\build\temp\"
$Out = "$Root\build\artifacts\"
if (Test-Path $Temp) 
{
    Remove-Item $Temp -Recurse
}
New-Item $Temp -Type Directory

if (Test-Path $Out) 
{
    Remove-Item $Out -Recurse
}
New-Item $Out -Type Directory

function PlatformRelease
{
	param($Platform)

	Remove-Item $Temp\* -recurse
	$PlatformShort = $Platform -Replace "win-", ""
	$MainZip = "win-acme.v$Version.$PlatformShort.zip"
	$MainZipPath = "$Out\$MainZip"
	$MainBin = "$Root\src\main\bin\Release\netcoreapp3.0\$Platform"
	if (!(Test-Path $MainBin)) 
	{
		$MainBin = "$Root\src\main\bin\Any CPU\Release\netcoreapp3.0\$Platform"
	}
	if (Test-Path $MainBin) 
	{
		./sign-exe.ps1 "$MainBin\publish\wacs.exe" "$Root\build\codesigning.pfx" $Password
		Copy-Item "$MainBin\publish\wacs.exe" $Temp
		Copy-Item "$MainBin\settings.config" "$Temp\settings_default.config"
		Copy-Item "$Root\dist\*" $Temp -Recurse
		Set-Content -Path "$Temp\version.txt" -Value "v$Version ($PlatformShort)"
		[io.compression.zipfile]::CreateFromDirectory($Temp, $MainZipPath)
	}
}

function PluginRelease
{
	param($Short, $Dir, $Files)

	Remove-Item $Temp\* -recurse
	$PlugZip = "win-acme.$Short.v$Version.zip"
	$PlugZipPath = "$Out\$PlugZip"
	$PlugBin = "$Root\src\$Dir\bin\Release\netstandard2.1\publish"
	foreach ($file in $files) {
		Copy-Item "$PlugBin\$file" $Temp
	}
	[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)
}

PlatformRelease win-x64
PlatformRelease win-x86
#PluginRelease dreamhost plugin.validation.dns.dreamhost @(
#	"PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost.dll"
#)
#PluginRelease azure plugin.validation.dns.azure @(
#	"Microsoft.Azure.Management.Dns.dll", 
#	"Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
#	"Microsoft.IdentityModel.Logging.dll",
#	"Microsoft.IdentityModel.Tokens.dll",
#	"Microsoft.Rest.ClientRuntime.Azure.Authentication.dll",
#	"Microsoft.Rest.ClientRuntime.Azure.dll",
#	"Microsoft.Rest.ClientRuntime.dll",
#	"PKISharp.WACS.Plugins.ValidationPlugins.Azure.dll"
#)
#PluginRelease route53 plugin.validation.dns.route53 @(
#	"AWSSDK.Core.dll", 
#	"AWSSDK.Route53.dll",
#	"PKISharp.WACS.Plugins.ValidationPlugins.Route53.dll"
#)

"Created artifacts:"
dir $Out
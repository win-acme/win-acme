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
	param($Version,$Root, $Password, $Temp, $Platform)

	Remove-Item $Temp\* -recurse
	$PlatformShort = $Platform -Replace "win-", ""
	$MainZip = "win-acme.v$Version.$PlatformShort.zip"
	$MainZipPath = "$Out\$MainZip"
	$MainBin = "$Root\src\main\bin\Release\netcoreapp3.0\$Platform"
	if (!(Test-Path $MainBin)) 
	{
		$MainBin = "$Root\src\main\bin\Release\Any CPU\netcoreapp3.0\$Platform"
	}
	./sign-exe.ps1 "$MainBin\publish\wacs.exe" "$Root\build\codesigning.pfx" $Password
	Copy-Item "$MainBin\publish\wacs.exe" $Temp
	Copy-Item "$MainBin\settings.config" "$Temp\settings_default.config"
	Copy-Item "$Root\dist\*" $Temp -Recurse
	Set-Content -Path "$Temp\version.txt" -Value "v$Version (64 bit)"
	[io.compression.zipfile]::CreateFromDirectory($Temp, $MainZipPath)
}

PlatformRelease $Version $Root $Password $Temp win-x64
PlatformRelease $Version $Root $Password $Temp win-x86

#Remove-Item $Temp\* -recurse
#$PlugZip = "win-acme.dreamhost.v$Version.zip"
#$PlugZipPath = "$Out\$PlugZip"
#$PlugBin = "$Root\src\plugin.validation.dns.dreamhost\bin\Release\net472"
#Copy-Item "$PlugBin\PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost.dll" $Temp
#[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)

#Remove-Item $Temp\* -recurse
#$PlugZip = "win-acme.azure.v$Version.zip"
#$PlugZipPath = "$Out\$PlugZip"
#$PlugBin = "$Root\src\plugin.validation.dns.azure\bin\Release\net472"
#Copy-Item "$PlugBin\Microsoft.Azure.Management.Dns.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.IdentityModel.Logging.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.IdentityModel.Tokens.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.Azure.Authentication.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.Azure.dll" $Temp
#Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.dll" $Temp
#Copy-Item "$PlugBin\PKISharp.WACS.Plugins.ValidationPlugins.Azure.dll" $Temp
#[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)

#Remove-Item $Temp\* -recurse
#$PlugZip = "win-acme.route53.v$Version.zip"
#$PlugZipPath = "$Out\$PlugZip"
#$PlugBin = "$Root\src\plugin.validation.dns.route53\bin\Release\net472"
#Copy-Item "$PlugBin\AWSSDK.Core.dll" $Temp
#Copy-Item "$PlugBin\AWSSDK.Route53.dll" $Temp
#Copy-Item "$PlugBin\PKISharp.WACS.Plugins.ValidationPlugins.Route53.dll" $Temp
#[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)

"Created artifacts:"
dir $Out
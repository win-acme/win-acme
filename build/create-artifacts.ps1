param (
	[Parameter(Mandatory=$true)]
	[string]
	$Root,
	
	[Parameter(Mandatory=$true)]
	[string]
	$Version,
	
	[Parameter]
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

$MainZip = "win-acme.v$Version.zip"
$MainZipPath = "$Out\$MainZip"
$MainBin = "$Root\src\main\bin\Release"

./sign-exe.ps1 "$MainBin\wacs.exe" "$Root\build\codesigning.pfx" $Password

Copy-Item "$MainBin\wacs.exe" $Temp
Copy-Item "$MainBin\Web_Config.xml" $Temp
Copy-Item "$MainBin\settings_default.config" $Temp
Copy-Item "$MainBin\wacs.exe.config" $Temp
Copy-Item "$Root\dist\*" $Temp -Recurse
Set-Content -Path "$Temp\version.txt" -Value "v$Version"
[io.compression.zipfile]::CreateFromDirectory($Temp, $MainZipPath)

Remove-Item $Temp\* -recurse
$PlugZip = "win-acme.dreamhost.v$Version.zip"
$PlugZipPath = "$Out\$PlugZip"
$PlugBin = "$Root\src\plugin.validation.dns.dreamhost\bin\Release\"
Copy-Item "$PlugBin\PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost.dll" $Temp
[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)
Remove-Item $Temp\* -recurse

$PlugZip = "win-acme.azure.v$Version.zip"
$PlugZipPath = "$Out\$PlugZip"
$PlugBin = "$Root\src\plugin.validation.dns.azure\bin\Release\"
Copy-Item "$PlugBin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll" $Temp
Copy-Item "$PlugBin\Microsoft.IdentityModel.Clients.ActiveDirectory.Platform.dll" $Temp
Copy-Item "$PlugBin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll" $Temp
Copy-Item "$PlugBin\Microsoft.IdentityModel.Tokens.dll" $Temp
Copy-Item "$PlugBin\Microsoft.IdentityModel.Logging.dll" $Temp
Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.Azure.dll" $Temp
Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.dll" $Temp
Copy-Item "$PlugBin\Microsoft.Rest.ClientRuntime.Azure.Authentication.dll" $Temp
Copy-Item "$PlugBin\Microsoft.Azure.Management.Dns.dll" $Temp
Copy-Item "$PlugBin\PKISharp.WACS.Plugins.ValidationPlugins.Azure.dll" $Temp
[io.compression.zipfile]::CreateFromDirectory($Temp, $PlugZipPath)
Remove-Item $Temp -recurse
param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^\d+\.\d+.\d+.\d+")]
	[string]
	$ReleaseVersionNumber
)

$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
$BuildFolder = Join-Path -Path $RepoRoot "build"

# Restore NuGet packages
& dotnet restore $RepoRoot\src\main\wacs.csproj

# Clean solution
foreach ($release in @("Release", "ReleaseTrimmed")) {
	foreach ($arch in @("win-x64", "win-x86", "win-arm64")) {
		Write-Host "Clean $arch $release..."
		& dotnet clean $RepoRoot\src\main\wacs.csproj -c $release -r $arch /p:SelfContained=true
	}
}

# Build main
& dotnet pack $RepoRoot\src\main\wacs.csproj -c "Release" /p:PublishSingleFile=false /p:PublishReadyToRun=false
foreach ($release in @("Release", "ReleaseTrimmed")) {
	foreach ($arch in @("win-x64", "win-x86", "win-arm64")) {
		Write-Host "Publish $arch $release..."
		$extra = ""
		if ($release.EndsWith("Trimmed")) {
			$extra = "/p:warninglevel=0"
		}
		& dotnet publish $RepoRoot\src\main\wacs.csproj -c $release -r $arch --self-contained $extra
	}
}

# Build plugins
& dotnet publish $RepoRoot\src\plugin.store.keyvault\wacs.store.keyvault.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.store.userstore\wacs.store.userstore.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.aliyun\wacs.validation.dns.aliyun.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.azure\wacs.validation.dns.azure.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.cloudflare\wacs.validation.dns.cloudflare.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.digitalocean\wacs.validation.dns.digitalocean.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.dnsexit\wacs.validation.dns.dnsexit.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.dnsmadeeasy\wacs.validation.dns.dnsmadeeasy.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.domeneshop\wacs.validation.dns.domeneshop.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.dreamhost\wacs.validation.dns.dreamhost.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.godaddy\wacs.validation.dns.godaddy.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.googledns\wacs.validation.dns.googledns.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.hetzner\wacs.validation.dns.hetzner.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.infomaniak\wacs.validation.dns.infomaniak.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.linode\wacs.validation.dns.linode.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.luadns\wacs.validation.dns.luadns.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.ns1\wacs.validation.dns.ns1.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.rfc2136\wacs.validation.dns.rfc2136.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.route53\wacs.validation.dns.route53.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.simply\wacs.validation.dns.simply.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.transip\wacs.validation.dns.transip.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.dns.tencent\wacs.validation.dns.tencent.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.http.ftp\wacs.validation.http.ftp.csproj -c "Release"
& dotnet publish $RepoRoot\src\plugin.validation.http.rest\wacs.validation.http.rest.csproj -c "Release"

if (-not $?)
{
	throw "The dotnet publish process returned an error code."
}

./create-artifacts.ps1 $RepoRoot $ReleaseVersionNumber
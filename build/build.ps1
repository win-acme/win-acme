param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^\d+\.\d+.\d+.\d+")]
	[string]
	$ReleaseVersionNumber
)

$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
$BuildFolder = Join-Path -Path $RepoRoot "build"
$SolutionPath = Join-Path -Path $RepoRoot -ChildPath "src\wacs.sln"
$ProjectRoot = Join-Path -Path $RepoRoot "src\main"
$Configuration = "Release"

# Restore NuGet packages
& dotnet restore $ProjectRoot/wacs.csproj

# Set the version number in SolutionInfo.cs
$SolutionInfoPath = Join-Path -Path $ProjectRoot -ChildPath "wacs.csproj"
(gc -Path $SolutionInfoPath) `
	-replace '<AssemblyVersion>[0-9\.*]+</AssemblyVersion>', "<AssemblyVersion>$ReleaseVersionNumber</AssemblyVersion>" |
	sc -Path $SolutionInfoPath -Encoding UTF8
(gc -Path $SolutionInfoPath) `
	-replace '<Version>[0-9\.*]+</Version>', "<Version>$ReleaseVersionNumber</Version>" |
	sc -Path $SolutionInfoPath -Encoding UTF8
(gc -Path $SolutionInfoPath) `
	-replace '<FileVersion>[0-9\.*]+</FileVersion>', "<FileVersion>$ReleaseVersionNumber</FileVersion>" |
	sc -Path $SolutionInfoPath -Encoding UTF8	

# Clean solution
& dotnet clean $ProjectRoot/wacs.csproj -c $Configuration -r win-x64
& dotnet clean $ProjectRoot/wacs.csproj -c $Configuration -r win-x86

# Build solution
& dotnet publish $ProjectRoot/wacs.csproj -c $Configuration -r win-x64
& dotnet publish $ProjectRoot/wacs.csproj -c $Configuration -r win-x86

if (-not $?)
{
	throw "The dotnet publish process returned an error code."
}

./create-artifacts.ps1 $RepoRoot $ReleaseVersionNumber
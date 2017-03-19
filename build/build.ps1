param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^\d\.\d\.(?:\d\.\d$|\d$)")]
	[string]
	$ReleaseVersionNumber
)

$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
$NuGetFolder = Join-Path -Path $RepoRoot "packages"
$SolutionPath = Join-Path -Path $RepoRoot -ChildPath "letsencrypt-win-simple.sln"
$BuildFolder = Join-Path -Path $RepoRoot -ChildPath "build"
$ProjectRoot = Join-Path -Path $RepoRoot "letsencrypt-win-simple"
$CoreProjectRoot = Join-Path -Path $RepoRoot "letsencrypt-win-simple-core"
$TempFolder = Join-Path -Path $BuildFolder -ChildPath "temp"
$Configuration = "Release"
$ReleaseOutputFolder = Join-Path -Path $ProjectRoot -ChildPath "bin/$Configuration"
$CoreReleaseOutputFolder = Join-Path -Path $CoreProjectRoot -ChildPath "bin/$Configuration"
$MSBuild = "${Env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MsBuild.exe"

# Go get nuget.exe if we don't have it
$NuGet = "$BuildFolder\nuget.exe"
$FileExists = Test-Path $NuGet 
If ($FileExists -eq $False) {
	$SourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
	Invoke-WebRequest $SourceNugetExe -OutFile $NuGet
}

# Restore NuGet packages
cmd.exe /c "$NuGet restore $SolutionPath -NonInteractive -PackagesDirectory $NuGetFolder"

# Set the version number in SolutionInfo.cs
$NewVersion = 'AssemblyVersion("' + $ReleaseVersionNumber + '.*")'
$NewFileVersion = 'AssemblyFileVersion("' + $ReleaseVersionNumber + '.0")'

$ProjectInfoPath = Join-Path -Path $ProjectRoot -ChildPath "Properties/AssemblyInfo.cs"
(gc -Path $ProjectInfoPath) `
	-replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewVersion |
	sc -Path $ProjectInfoPath -Encoding UTF8
(gc -Path $ProjectInfoPath) `
	-replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "$NewFileVersion" |
	sc -Path $ProjectInfoPath -Encoding UTF8

$CoreProjectInfoPath = Join-Path -Path $CoreProjectRoot -ChildPath "Properties/AssemblyInfo.cs"
(gc -Path $CoreProjectInfoPath) `
	-replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewVersion |
	sc -Path $CoreProjectInfoPath -Encoding UTF8
(gc -Path $CoreProjectInfoPath) `
	-replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "$NewFileVersion" |
	sc -Path $CoreProjectInfoPath -Encoding UTF8

# Build the solution in release mode

# Clean solution
& $MSBuild "$SolutionPath" /p:Configuration=$Configuration /maxcpucount /t:Clean
if (-not $?)
{
	throw "The MSBuild process returned an error code."
}

# Build
& $MSBuild "$SolutionPath" /p:Configuration=$Configuration /maxcpucount
if (-not $?)
{
	throw "The MSBuild process returned an error code."
}

if (Test-Path $TempFolder) 
{
    Remove-Item $TempFolder -Recurse
}

# Copy release files
if (Test-Path $TempFolder) 
{
    Remove-Item $TempFolder -Recurse
}

$DestinationZipFile = "$BuildFolder\letsencrypt-win-simple.V$ReleaseVersionNumber.zip" 
if (Test-Path $DestinationZipFile) 
{
    Remove-Item $DestinationZipFile
}

Copy-Item (Join-Path -Path $ReleaseOutputFolder -ChildPath "x64") (Join-Path -Path $TempFolder -ChildPath "x64") -Recurse
Copy-Item (Join-Path -Path $ReleaseOutputFolder -ChildPath "x86") (Join-Path -Path $TempFolder -ChildPath "x86") -Recurse
Copy-Item (Join-Path -Path $ReleaseOutputFolder "letsencrypt.exe") $TempFolder
Copy-Item (Join-Path -Path $ReleaseOutputFolder "Web_Config.xml") $TempFolder
Copy-Item (Join-Path -Path $CoreReleaseOutputFolder "letsencrypt-core.dll") $TempFolder
Copy-Item -Path (Join-Path -Path $CoreReleaseOutputFolder "letsencrypt-core.dll.config") -Destination (Join-Path -Path $TempFolder "letsencrypt.exe.config")

# Zip the package
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($TempFolder, $DestinationZipFile) 
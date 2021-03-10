$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
& dotnet pack $RepoRoot\src\main\wacs.csproj -c "ReleasePluggable"
& dotnet tool uninstall win-acme --global
& dotnet tool install --global --add-source $RepoRoot\src\main\nupkg\ win-acme --version 2.1.0

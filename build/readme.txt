Run the build with a specific version number like so:

	powershell .\build.ps1 -ReleaseVersionNumber 1.9.3.0

To create a new code signing certificate:

	New-SelfSignedCertificate -DnsName win.acme.simple@gmail.com -Type CodeSigning -CertStoreLocation cert:\CurrentUser\My


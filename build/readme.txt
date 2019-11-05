Run the build with a specific version number like so:

	powershell .\build.ps1 -ReleaseVersionNumber 1.9.3.0

To create a new code signing certificate:

	$cert = New-SelfSignedCertificate -DnsName email@example.com -Type CodeSigning -CertStoreLocation cert:\CurrentUser\My
	$pwd = ConvertTo-SecureString -String "supersecret1234" -Force –AsPlainText
	Export-PfxCertificate -Cert "cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath "C:\Git\Repos\win-acme\build\codesigning.pfx" -Password $pwd


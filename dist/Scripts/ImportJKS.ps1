<#
.SYNOPSIS
Add a win-acme renewal to a Java keystore

Sample usage:

wacs.exe 
    --target manual 
    --host example.com
    --store none 
    --installation script 
    --script "Scripts\ImportJKS.ps1" 
    --scriptparameters "-pfxfile \"{CacheFile}\" -pfxpassword {CachePassword} -keystorefile \"C:\key store.jks\" -keystorepassword **** -keystorekeypassword ****"

#>

param(
	[Parameter(Mandatory=$true)]
	[string]
	$PfxFile,
	
	[Parameter(Mandatory=$true)]
	[string]
	$PfxPassword,

	[Parameter(Mandatory=$true)]
	[string]
	$KeyStoreFile,

	[Parameter(Mandatory=$true)]
	[string]
	$KeyStorePassword,
	
	[Parameter(Mandatory=$false)]
	[string]
	$KeyStoreKeyPassword
)

$keytoolpath = Join-Path -Path $env:JAVA_HOME -ChildPath bin\keytool.exe

Set-Alias keytool $keytoolpath

if ([string]::IsNullOrEmpty($KeyStoreKeyPassword)) 
{
    keytool `
        -v `
        -noprompt `
        -importkeystore `
        -srckeystore "$PfxFile" `
        -srcstoretype PKCS12 `
        -srcstorepass "$PfxPassword" `
        -destkeystore "$KeyStoreFile" `
        -deststorepass "$KeyStorePassword"
} 
else 
{
    keytool `
        -v `
        -noprompt `
        -importkeystore `
        -srckeystore "$PfxFile" `
        -srcstoretype PKCS12 `
        -srcstorepass "$PfxPassword" `
        -destkeystore "$KeyStoreFile" `
        -deststorepass "$KeyStorePassword" `
        -destkeypass "$KeyStoreKeyPassword"
}

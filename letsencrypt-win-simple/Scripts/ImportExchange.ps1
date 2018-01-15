param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,         # should be {5} from letsencrypt.exe
    [Parameter(Position=1,Mandatory=$true)]
    [string]$ExchangeServices,          # comma-separated list of exchange services to import for
    [Parameter(Position=2,Mandatory=$false)]
    [bool]$LeaveOldExchangeCerts = 1,   # 0 for false, 1 for true. If set to 1, will skip removal of old exchange certs
    [Parameter(Position=3,Mandatory=$false)]
    [string]$TargetHost,                # primary fqdn of cert. shouldn't be necessary if thumbprint was included and letsencrypt places cert in the store
    [Parameter(Position=4,Mandatory=$false)]
    [string]$StorePath                  # required if $TargetHost specified. Cert pfx file will be stored here.
)

Write-Debug -Message ('NewCertThumbprint: {0}' -f $NewCertThumbprint)
Write-Debug -Message ('ExchangeServices: {0}' -f $ExchangeServices)
Write-Debug -Message ('TargetHost: {0}' -f $TargetHost)
Write-Debug -Message ('StorePath: {0}' -f $StorePath)


## Imports new cert thumbprint into the Exchange roles defined in the $ExchangeServices comma-separate list
## example: $ExchangeServices = IMAP,POP,IIS
## That will import the certificate for IMAP, POP, and IIS roles in exchange
## Valid services:  IMAP | POP | UM | IIS | SMTP | Federation | UMCallRouter

## THIS SCRIPT IS INCOMPLETE AND *mostly* UNTESTED (some modifications have come in from people using it successfully)
## Documentation referenced from https://technet.microsoft.com/en-us/library/aa997231(v=exchg.160).aspx

# Should work with exchange 2007 and higher
Get-PSSnapin -registered | Where-Object {$_.Name -match "Microsoft.Exchange.Management.PowerShell" -and ($_.Name -match "Admin" -or $_.Name -match "E2010" -or $_.Name -match "SnapIn")} | Add-PSSnapin -ErrorAction SilentlyContinue

#$OldThumbprint = (Get-Item -Path RDS:\GatewayServer\SSLCertificate\Thumbprint).CurrentValue
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
try{
    if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
        "Cert thumbprint not found in the cert store... which means we should load it. This means TargetHost and StorePath must be specified"
        
        "Try to load certificate from store"
        $importExchangeCertificateParameters = @{
        FileName = (Join-Path -Path $StorePath -ChildPath "$TargetHost.pfx")
        FriendlyName = $TargetHost
        PrivateKeyExportable = $true
        }
        
        $null = Import-ExchangeCertificate @importExchangeCertificateParameters -ErrorAction Stop


        # Now try to find it again...
        $CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
        
    }

    # Make sure variable is defined
    $null = Get-ChildItem $CertInStore.PSPath -ErrorAction Stop
    # Cert must exist in the personal store of machine to bind to exchange services
    if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
        $SourceStoreScope = 'LocalMachine'
        $SourceStorename = $CertInStore.PSParentPath.split("\")[-1]

        $SourceStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $SourceStorename, $SourceStoreScope
        $SourceStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)

        $cert = $SourceStore.Certificates | Where-Object {$_.thumbprint -eq $CertInStore.Thumbprint}



        $DestStoreScope = 'LocalMachine'
        $DestStoreName = 'My'

        $DestStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $DestStoreName, $DestStoreScope
        $DestStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $DestStore.Add($cert)


        $SourceStore.Close()
        $DestStore.Close()

        $CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
    }
    Enable-ExchangeCertificate -Services $ExchangeServices -Thumbprint $CertInStore.Thumbprint -Force -ErrorAction Stop
    "Cert thumbprint set to the following exchange services: $ExchangeServices"

    
    if(-not $LeaveOldExchangeCerts){
        "Old Exchange certificates being cleaned up"
        Get-ExchangeCertificate -DomainName $CertInStore.Subject.split("=")[1] | Where-Object -FilterScript {
            $_.Thumbprint -ne $newCertThumbprint
        } | Remove-ExchangeCertificate -Confirm:$false
    }

}catch{
    "Cert thumbprint was not set successfully"
    "Error: $($Error[0])"
}



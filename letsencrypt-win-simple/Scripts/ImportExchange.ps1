param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
    [Parameter(Position=1,Mandatory=$true)]
    [string]$ExchangeServices
)

## Imports new cert thumbprint into the Exchange roles defined in the $ExchangeServices comma-separate list
## example: $ExchangeServices = IMAP,POP,IIS
## That will import the certificate for IMAP, POP, and IIS roles in exchange
## Valid services:  IMAP | POP | UM | IIS | SMTP | Federation | UMCallRouter

## THIS SCRIPT IS INCOMPLETE AND UNTESTED
## Documentation referenced from https://technet.microsoft.com/en-us/library/aa997231(v=exchg.160).aspx 

# Only works with exchange 2013 and higher
Add-PSSnapin Microsoft.Exchange.Management.PowerShell.SnapIn

#$OldThumbprint = (Get-Item -Path RDS:\GatewayServer\SSLCertificate\Thumbprint).CurrentValue
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object thumbprint -eq $NewCertThumbprint | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine to bind to RD Gateway
        if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
            $SourceStoreScope = 'LocalMachine'
            $SourceStorename = $CertInStore.PSParentPath.split("\")[-1]

            $SourceStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $SourceStorename, $SourceStoreScope
            $SourceStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
            
            $cert = $SourceStore.Certificates | Where-Object thumbprint -eq $CertInStore.Thumbprint
            
            
            
            $DestStoreScope = 'LocalMachine'
            $DestStoreName = 'My'
            
            $DestStore = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $DestStoreName, $DestStoreScope
            $DestStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $DestStore.Add($cert)
            
            
            $SourceStore.Close()
            $DestStore.Close()

            $CertInStore = Get-ChildItem -Path Cert:\LocalMachine\My -Recurse | Where-Object thumbprint -eq $NewCertThumbprint | Sort-Object -Descending | Select-Object -f 1
        }
        Enable-ExchangeCertificate -Services $ExchangeServices -Thumbprint $CertInStore.Thumbprint -ErrorAction Stop
        "Cert thumbprint set to the following exchange services: $ExchangeServices"  
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


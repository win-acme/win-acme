param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

Import-Module RemoteDesktopServices

#$OldThumbprint = (Get-Item -Path RDS:\GatewayServer\SSLCertificate\Thumbprint).CurrentValue
$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object thumbprint -eq $NewCertThumbprint | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine to bind to RD Gateway
        if($CertInStore.PSPath -notlike "*LocalMachine\My\*"){
            $CertInStore = Copy-Item $CertInStore -Destination Cert:\LocalMachine\My -PassThru -ErrorAction Stop
        }
        Set-Item -Path RDS:\GatewayServer\SSLCertificate\Thumbprint -Value $NewCertThumbprint -ErrorAction Stop
        "Cert thumbprint set to RD Gateway listener"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}


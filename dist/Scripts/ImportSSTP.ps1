param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint,
    [Parameter(Position=1,Mandatory=$false)]
    [int]$RecreateDefaultBindings = 1
)

Import-Module RemoteAccess

$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine to bind to RRAS
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
        "Stopping RemoteAccess service to prevent errors..."
        Stop-Service RemoteAccess
        if($RecreateDefaultBindings -eq 1)
        {
            "Checking if we need to replace default binding..."
            $replace = $false;
            $binds = Get-WebBinding -Name "Default Web Site" -Protocol https;
            for ($i=0; $i -lt $binds.length; $i++)
            {
                if(($binds[$i] | Select-Object -ExpandProperty bindingInformation) -eq "*:443:")
                {
                    "Default binding detected. Deleting..."
                    $binds[$i] | Remove-WebBinding;
                    $replace = $true;
                    break;
                }
            }
            if($replace -eq $true)
            {
                "Creating new default binding..."
                $binding = New-WebBinding -Name "Default Web Site" -Protocol https -IPAddress * -Port 443 -Force;
                $binds = Get-WebBinding -Name "Default Web Site" -Protocol https;
                for ($i=0; $i -lt $binds.length; $i++)
                {
                    if(($binds[$i] | Select-Object -ExpandProperty bindingInformation) -eq "*:443:")
                    {
                        $binding = $binds[$i];
                        break;
                    }
                }
                "Assigning certificate to new default binding..."
                $binding.AddSslCertificate($NewCertThumbprint, "my");
            }
        }
        "Assigning certificate to RRAS..."
        Set-RemoteAccess -SslCertificate $CertInStore
        "SSTP SSL certificate has been applied, restarting RemoteAccess..."
        Start-Service RemoteAccess
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
}else{
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}
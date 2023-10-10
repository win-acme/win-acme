<#
.SYNOPSIS
Imports a cert from WACS renewal into SQL Server
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

Proper information should be available here

https://github.com/win-acme/win-acme

.PARAMETER NewCertThumbprint
The exact thumbprint of the cert to be imported. The script will copy this cert to the Personal store if not already there and assign read permission to the SQL Server serviceaccount. 


.EXAMPLE 

ImportSQL.ps1 <certThumbprint>

./wacs.exe --target manual --host hostname.example.com --installation script --script ".\Scripts\ImportSQL.ps1" --scriptparameters "'{CertThumbprint}'" --certificatestore My

.NOTES

#>
param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$NewCertThumbprint
)

#stolen from https://blogs.infosupport.com/configuring-sql-server-encrypted-connections-using-powershell/
function Set-SqlConnectionEncryption
{
   [CmdletBinding()]
   param (
      # See https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/certificate-requirements?view=sql-server-ver16
      [Parameter(Mandatory=$false, Position = 1, HelpMessage = "The certificate to use (or `$null " + 
         "to clear it), must be present in the cert:\Local Machine\My store.")]
      [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
      
      [Parameter(Mandatory=$false, HelpMessage = "The SQL Server serviceaccount that should be " +
         "granted access to the certificate's private key. Optional, defaults to the MSSQLSERVER " + 
         "login account, usually ""NT Service\MSSQLSERVER"".")]
      [string]$ServiceAccount,
      
      # See https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-sql-server-encryption?view=sql-server-ver16#step-2-configure-encryption-settings-in-sql-server
      [Parameter(HelpMessage = "Wether SQL Server should accept only encrypted connections. " +
         "Defaults to false.")]
      [switch]$ForceEncryption,
      
      # See https://learn.microsoft.com/en-us/sql/relational-databases/security/networking/tds-8
      [Parameter(HelpMessage = "Wether SQL Server requires clients to specify HostNameInCertificate " + 
         "rather than TrustServerCertificate=true. Defaults to false.")]
      [switch]$ForceStrict
   )
 
   $ErrorActionPreference = "Stop"
 
   # 1. Configure the Certificate, ForceEncryption and ForceStrict values that SQL Server should use
   # Locate the "SuperSocketNetLib" registry key that contains the encryption settings; highest 
   # first in case there are multiple versions.
   $regKey = (ls "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server" -Recurse | 
      Where-Object { $_.Name -like '*SuperSocketNetLib' } | Sort-Object -Property Name -Descending)
   if($regKey -is [Array])
   {
      $regKey = $regKey[0]
      Write-Warning "Multiple SQL instances found in the registry, using ""$($regKey.Name)""."
   }
 
   if($Certificate)
   {
      # The thumbprint must be in all lowercase, otherwise SQL server doesn't seem to accept it?!
      Set-ItemProperty $regKey.PSPath -Name "Certificate" -Value $Certificate.Thumbprint.ToLowerInvariant()
   }
   else
   {
      Set-ItemProperty $regKey.PSPath -Name "Certificate" -Value $null
   }
       
   # Note that ForceStrict is only available from SQL2022 onwards
   Set-ItemProperty $regKey.PSPath -Name "ForceEncryption" `
      -Value $(if($ForceEncryption) { 1 } else { 0 })
   Set-ItemProperty $regKey.PSPath -Name "ForceStrict" `
      -Value $(if($ForceStrict) { 1 } else { 0 })
     
   # 2. Grant READ access to the $Certificate's private key
   if($Certificate)
   {
      # If no -ServiceAccount is specified, determine under which identity the MSSQLSERVER service 
      # runs...
      if(!$ServiceAccount)
      {
         $ServiceAccount = (Get-WmiObject Win32_Service -Filter "Name='MSSQLSERVER'").StartName
      }
       
      # The private keys are stored on the file system, determine the path for $Certificate
      # See https://stackoverflow.com/questions/40046916/how-to-grant-permission-to-user-on-certificate-private-key-using-powershell
      if (!$Certificate.PrivateKey)
      {
          $privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($Certificate)
          $privateKeyPath = "$env:ProgramData\Microsoft\Crypto\Keys\$($privateKey.Key.UniqueName)"
      }
      else
      {
          $containerName = $Certificate.PrivateKey.CspKeyContainerInfo.UniqueKeyContainerName
          $privateKeyPath = "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys\$containerName"
      }
       
      # Now grant the $ServiceAccount read permissions to the private key file
      $acl = Get-Acl -Path $privateKeyPath
      $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule `
         @($serviceaccount, "Read", "Allow")
      [void]$acl.AddAccessRule($accessRule)
      Set-Acl -Path $privateKeyPath -AclObject $acl
   }
}


$CertInStore = Get-ChildItem -Path Cert:\LocalMachine -Recurse | Where-Object {$_.thumbprint -eq $NewCertThumbprint} | Sort-Object -Descending | Select-Object -f 1
if($CertInStore){
    try{
        # Cert must exist in the personal store of machine to bind to SQL
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

        Set-SqlConnectionEncryption -Certificate $CertInStore
        Restart-Service MSSQLSERVER -Force -ErrorAction Stop
        "Cert thumbprint set to ADFS and service restarted"
    }catch{
        "Cert thumbprint was not set successfully"
        "Error: $($Error[0])"
    }
} else {
    "Cert thumbprint not found in the cert store... which is strange because it should be there."
}
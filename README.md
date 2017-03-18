
### RSAKeyBits

The key size to sign the certificate with. Default is 2048, Minimum is 1024.

### HostsPerPage

The number of hosts to display per page. Default is 50.

### CertificatePath

The path where certificates and request files are stored. 
Default is empty which resolves to `%appdata%\letsencrypt-win-simple\[BaseUri]`. 
All directories and subdirectories in the specified path are created unless they already exist.
The default path is used when the specified path is invalid.

### RenewalDays

The number of days to renew a certificate after.
The default is 60. Let's Encrypt certificates are currently valid for a max of 90 days so it is advised to not increase the days much.
If you increase the days, please note that you will have less than 30 days to fix any issues if the certificate doesn't renew correctly.

### CertificateStore

The certificate store to save the certificates in.

### CleanupFolders

If set to True, it will cleanup the folder structure and files it creates under the site for authorization.

# Settings

Some of the applications' settings can be updated in the app's settings or configuration file. the file is in the application root and is called letsencrypt.exe.config.

### FileDateFormat

The FileDateFormat is a string that is used to format the date of the pfx file's friendly name.
Default: ```yyyy/M/d/ h:m:s tt``` ex. 2016/1/21 2:58:12 PM
See https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx to create your own custom date format.

### PFXPassword

The password to sign all PFX files with. Default is empty.

### RSAKeyBits

The key size to sign the certificate with. Default is 2048, Minimum is 1024.

### HostsPerPage

The number of hosts to display per page. Default is 50.

### CertificatePath

The path where certificates and request files are stored. 
Default is empty which resolves to `%appdata%\letsencrypt-win-simple\[BaseUri]`. 
All directories and subdirectories in the specified path are created unless they already exist.
The default path is used when the specified path is invalid.

### RenewalDays

The number of days to renew a certificate after.
The default is 60. Let's Encrypt certificates are currently valid for a max of 90 days so it is advised to not increase the days much.
If you increase the days, please note that you will have less than 30 days to fix any issues if the certificate doesn't renew correctly.

### CertificateStore

The certificate store to save the certificates in.

### CleanupFolders

If set to True, it will cleanup the folder structure and files it creates under the site for authorization.

# Wiki

Please head to the [Wiki](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki) to learn more.

## Settings

See the [Application Settings](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/Application-Settings) page on the wiki for settings such as how to change the location where certificates are stored, how they're generated, and how often they are renewed among other settings.

# Support

If you run into trouble please open an issue at https://github.com/Lone-Coder/letsencrypt-win-simple/issues

Please check to see if your issue is covered in the [Wiki](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki) before you create a new issue.

If you ran the app and you got an error when it tried to Authorize your site take a look [here](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/web.config).

# Web.Config Pull Requests

If you submit a pull request that changes the included web.config file and it does not work on stock IIS 7.5 +, it will not be merged in. Instead add a section to the [WIki page](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/web.config) with your changes.
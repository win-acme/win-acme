---
sidebar: manual
---

# Apache
To get the certificate in the correct format for Apache (i.e. `.pem` files), you have to active 
the [PemFiles plugin](/win-acme/reference/plugins/store/pemfiles) for each of your renewals. 
For **new** renewals this can be done either from the command line with `--store pemfiles` or 
from the main menu with the `M` option, where it will be posed as a question ("How would you 
like to store this certificate?"). 

Existing renewals that are set up without the PemFiles plugin (which unfortunately includes 
those [imported](/win-acme/manual/upgrading/to-v2.0.0) from 1.9.x) cannot be modified with a 
command line switch or settings change. You will have to re-create them one by one, or manually 
modify the `.json` files on disk.

## Getting the certificate in .pem format

### Interactive
- Choose `M` in the main menu (create with full options)
- Choose "Manually input host names" as target
- Input the domain name(s)
- Choose or accept the friendly name
- Pick a validation method. Most common would be to save to a local path
- Pick your key type
- Now the critical part: at "How would you like to store this certificate?" pick `Write .pem files to folder (Apache, nginx, etc.)`
- And so on...

### Unattended 
`wacs.exe --target manual --host www.example.com --validation filesystem --webroot "C:\htdocs\www\example.com" --store pemfiles --pemfilespath C:\apache-certs`

### Pro tip
If you don't want to have to specify the path for the `.pem` files each time, you can 
edit `settings.json` in the program directory and set the `DefaultPemFilesPath` 
option.
     
## Configuring Apache
To use certificates obtained with the help of WACS with the Apache 2.4 server, you need 
to make settings in `Apache24\conf\extra\httpd-vhosts.conf` file; you could also make 
these changes in the `\Apache24\conf\extra\httpd-ssl.conf` file as well instead if 
you so wish:

```
Define CERTROOT "C:/apache-certs"
Define SITEROOT "C:/htdocs/www"
....
<VirtualHost *:443>
    ServerName www.example.com
    DocumentRoot "${SITEROOT}/example.com"
....
    SSLEngine on
    SSLCertificateFile "${CERTROOT}/example.com-chain.pem"
    SSLCertificateKeyFile "${CERTROOT}/example.com-key.pem"
</VirtualHost>
```

Obviously replace `example.com` with your actual domain name your siteroot to 
where you're hosting your files. 

### Enable SSL 
Do not forget to uncomment `LoadModule ssl_module modules/mod_ssl.so` in `Apache24\conf\httpd.conf` 
file if it's not already uncommented. You also need to add `Listen 443` or `Listen 80 443`. 

### Not for XAMPP uses
You don't need the `/example.com` at the end after `"${SITEROOT}"` so it 
should just read as: `DocumentRoot "${SITEROOT}"` for that one line or else 
(at least according to my case), would result in an object not found 404 error 
when you visit your domain page. 

Also, according to Apache standards, backslash means escaping characters so if you wanted to 
use backslash as a way for defining directories, then you're supposed to use another one 
so it looks like `C:\\XAMPP\\Apache\\somestuff` but apparently the developers have modded 
it so that it doesn't really matter if you double slash or not or use forward slash instead 
of a single back slash - they all work the same regardless, at least as of version 
3.2.2 of XAMPP.

## Addendum
If you want to use your own folder to store certificates, you can use this cmd script is 
for copying (for example, with name `installcert.cmd`):

```
@echo off
if "%~1" == "" exit
if not exist "%2" md "%2" >nul
set certlist=%3-chain.pem,%3-key.pem
echo Script running...
for %%a in (%certlist%) do copy /y "%ProgramData%\win-acme\%1\%%a" "%2\" >nul && echo. [INFO] Install %%a to Certificate Store in %2... OK || echo. [WARN] Install certificate %%a fieled!
echo. [INFO] Restarting service...
C:\Apache24\bin\httpd.exe -k restart
echo. [INFO] Service restarted.
echo. [INFO] Script finished.
```
This script is called with parameters:
`LEWSuriDirectory CertFolder DomainName`

For example:
`wacs.exe --target manual --host www.example.com --webroot "C:\htdocs\www\example.com" --validation filesystem --script "installcert.cmd" --scriptparameters "acme-v02.api.letsencrypt.org C:\cert www.example.com"`

Also you must specify a new path to the folder with certificates in your `httpd-vhosts.conf`.
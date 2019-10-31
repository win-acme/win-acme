---
sidebar: manual
---

# System requirements
- Officially Microsoft only supports Windows Server 2012 R2 SP1 and higher
for .NET Core 3, but the program has been tested on Windows Server 2008 
as well.
- You may need to install [Microsoft Visual C++ 2015 Redistributable Update 3](https://www.microsoft.com/download/details.aspx?id=52685)
- If you run into the error about `api-ms-win-crt-runtime-l1-1-0.dll` you may need [KB2999226](https://support.microsoft.com/help/2999226/update-for-universal-c-runtime-in-windows)
- If you run into the error about `hostfxr.dll` you may need [KB2533623](https://support.microsoft.com/help/2533623/microsoft-security-advisory-insecure-library-loading-could-allow-remot)

## Microsoft IIS
### Server Name Indication
Server Name Indication (SNI) is supported from IIS 8.0 (Windows Server 2012) and above. 
This feature allows you to have multiple HTTPS certificates on the same IP address. 
Without it, you can only configure a single certificate per IP address. 

#### Workarounds for IIS 7.x
If you want to have SSL for multiple sites with multiple domains with IIS 7.5 or 
lower all bound to the same IP address your choices are:
- Create a single certificate for all sites. That only works if there are less than 
100 domains in total (that's the maximum Let's Encrypt will currently support)
- If they are sub domains of the same root, a wildcard certificate can be an option.

#### Configuring the IP address
When win-acme creates the binding for a new certificate, it will bind the wildcard (*) 
IP address by default. In other words, incoming connections on all network interfaces
will handeled using the certificate. You can customize this with the `--sslipaddress` 
switch from the command line, or manually after win-acme created the binding. On renewal, 
the program will preserve whatever setting is configured in IIS.

### Wildcard bindings
Wildcard bindings are only supported on IIS 10 (Windows Server 2016+). Wildcard 
certificates can be created with older versions of IIS but their bindings will have 
to be configured manually.

### FTPS bindings
Updating FTPS binding is only supported for IIS 7.5 (Windows 2008 R2) and above.

## Powershell
If you use Powershell (`.ps1`) script for custom installation or DNS validation steps,
please make sure to use a recent version of the Powershell runtime. Windows Server 2008 
ships with Powershell 2.0 which seems to have issues with starting from win-acme.

## Microsoft Exchange
Please refer to [this page](https://docs.microsoft.com/en-us/exchange/plan-and-deploy/supportability-matrix?view=exchserver-2019) 
to check compatibility between different versions of Exchange and the .NET Framework.
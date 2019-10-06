---
sidebar: reference
---

# IIS Central Certificate Store (CSS)
Designed for the [Central Certificate Store](https://blogs.msdn.microsoft.com/kaushal/2012/10/11/central-certificate-store-ccs-with-iis-8-windows-server-2012/) 
introduced in Windows 2012. Creates a separate copy of the `.pfx` file for each hostname and places 
it in the path provided by the `--centralsslstore` parameter, or the `DefaultCentralSslStore` setting
in [settings.json](/win-acme/reference/settings). Using this store also triggers any created or 
updated IIS bindings to get the `CentralSSL` flag. 

## Unattended
`--store centralssl [--centralsslstore C:\CentralSSL\] [--pfxpassword *****]`
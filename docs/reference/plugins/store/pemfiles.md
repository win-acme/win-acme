---
sidebar: reference
---

# PemFiles
Designed for [Apache](/win-acme/manual/advanced-use/examples/apache), nginx and other web servers. 
Exports a `.pem` file for the certificate and private key and places them in 
the path provided by the `--pemfilespath` parameter, or the `DefaultPemFilesPath` 
setting in [settings.json](/win-acme/reference/settings). 

## Unattended
`--store pemfiles [--pemfilespath C:\Certificates\]`
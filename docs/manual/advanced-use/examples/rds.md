---
sidebar: manual
---

# Remote Desktop Services
How to generate a Certificate for Microsoft Remote Desktop Servers

## Running the client
Assuming you've a simple all in one Remote Desktop Server setup with the roles RD Gateway, RD Connection Broker 
and RD Web Access, you have to import the certificate into the IIS site and additionally configure it for the 
installed RD roles. For IIS the standard plugin is used, for the RD roles, the included `ImportRDSFull.ps1` is 
used.

## Configuration
In order for this script to work, the private key of the certificate needs to be marked as exportable. 
Set `PrivateKeyExportable` in `settings.json` to `true`.

The script accepts two parameters: CertThumbprint and RDCB. RDCB specifies the Remote Desktop Connection Broker 
(RD Connection Broker) server for a Remote Desktop deployment. If you don't specify a value, the script uses the local 
computer's fully qualified domain name (FQDN).

## Unattended
- When specific domain names are configured in the IIS bindings, we can use them automatically
`wacs.exe --target iis --siteid 1 --certificatestore My --installation iis,script --script "Scripts\ImportRDSFull.ps1" --scriptparameters "{CertThumbprint}"`

- When only blank/catch-all binding are configured in IIS, we have to be explicit about the domain name(s) that we want
`wacs.exe --target manual --hostname rds.example.com --certificatestore My --installation iis,script --installationsiteid 1 --script "Scripts\ImportRDSFull.ps1" --scriptparameters "{CertThumbprint}"`
---
sidebar: manual
---

# Remote Desktop Services
How to generate a Certificate for Microsoft Remote Desktop Servers

## Running the client
Assuming you've a simple all in one Remote Desktop Server setup you've to import 
the certificate into the IIS site and additionally configure it in the RD Listener 
and RD Gateway. For IIS the standard plugin is used, for RDListener/RDGateway 
the included `ImportRDS.ps1` is used.

## Unattended, without CCS (Tested on Windows Server 2008 R2)
`wacs.exe --target iissite --siteid 1 --certificatestore My --installation iis,script --script "Scripts\ImportRDS.ps1" --scriptparameters "{CertThumbprint}"`
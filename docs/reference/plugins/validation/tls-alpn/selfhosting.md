---
sidebar: reference
---

# Self-hosting
This plugin launches a temporary built-in TCP listener that stores the 
validation response in memory. There for  share port 80 with IIS and 
other (Microsoft) software so this doesn't interfere with regular traffic. 
Not all software supports this port sharing feature though. If you get errors 
telling you that the listener cannot be started, please look for another
validation method.

## Non-default port
Even though Let's Encrypt will always try to open the validation connection 
on port 443, you may internally NAT that to another port. Using the 
`--validationport` switch you can tell the plugin to listen to a specific port.

## Unattended 
`--validationmode tls-alpn-01 --validation selfhosting [--validationport 4330]`
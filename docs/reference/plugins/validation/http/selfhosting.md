---
sidebar: reference
---

# Self-hosting
This plugin launches a temporary built-in web listener that stores the 
validation response in memory. It can share port 80 with IIS and 
other (Microsoft) software so this doesn't interfere with regular traffic. 
Not all software supports this port sharing feature though. If you get errors 
telling you that the listener cannot be started, please look for another
validation method.

## Non-default port
Even though Let's Encrypt will always send validation requests to port 80, 
you may internally proxy, NAT or redirect that to another port. Using the 
`--validationport` switch you can tell the plugin to listen to a specific port.

## Unattended 
`[--validation selfhosting] [--validationport 8080]`